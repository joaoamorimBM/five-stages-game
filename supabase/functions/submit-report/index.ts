// Recebe o relatório do jogo e grava na tabela sessoes.
//
// - Público (sem login): o jogo não carrega nenhuma chave secreta.
// - Valida formato, tamanho e o código do paciente antes de escrever.
// - Upsert por sessionId: o jogo envia a mesma sessão várias vezes (parcial → completa).
// - Nunca loga o conteúdo do relatório (LGPD).
import { createClient } from "jsr:@supabase/supabase-js@2";
import { LIMITE_BYTES, validarRelatorio } from "../_shared/relatorio.ts";
import { lerJson, preflight, responder } from "../_shared/http.ts";

const db = createClient(Deno.env.get("SUPABASE_URL")!, Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!, {
  auth: { persistSession: false },
});

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return preflight();
  if (req.method !== "POST") return responder(405, { erro: "use_post" });

  const { json, erro } = await lerJson(req, LIMITE_BYTES);
  if (erro) return erro;

  const v = validarRelatorio(json);
  if (!v.ok) return responder(400, { erro: v.erro });
  const d = v.dados;

  const { data: paciente, error: e1 } = await db
    .from("pacientes").select("id").eq("codigo", d.codigo).maybeSingle();
  if (e1) return falha("busca_paciente", e1);
  // 404 faz o jogo separar o arquivo em vez de tentar de novo pra sempre
  if (!paciente) return responder(404, { erro: "codigo_desconhecido" });

  const { data: existente, error: e2 } = await db
    .from("sessoes").select("paciente_id, parcial").eq("id", d.sessionId).maybeSingle();
  if (e2) return falha("busca_sessao", e2);

  if (existente && existente.paciente_id !== paciente.id) {
    return responder(409, { erro: "sessao_de_outro_paciente" });
  }
  // Uma versão parcial atrasada não sobrescreve a sessão já completa
  if (existente && !existente.parcial && d.parcial) {
    return responder(200, { ok: true, ignorado: "sessao_ja_completa" });
  }

  const { error: e3 } = await db.from("sessoes").upsert({
    id: d.sessionId,
    paciente_id: paciente.id,
    iniciada_em: d.iniciadaEm,
    duracao_segundos: d.duracaoSegundos,
    parcial: d.parcial,
    formato: d.formato,
    relatorio: json,
    resumo: d.resumo,
    atualizada_em: new Date().toISOString(),
  });
  if (e3) return falha("gravar_sessao", e3);

  return responder(200, { ok: true });
});

function falha(etapa: string, e: { code?: string }) {
  console.error("submit-report falhou em " + etapa + ": " + (e.code ?? "sem_codigo"));
  return responder(500, { erro: "falha_no_servidor" });
}
