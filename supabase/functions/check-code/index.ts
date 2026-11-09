// O jogo pergunta se um código existe, pra mostrar "código reconhecido" na tela inicial.
// Responde só sim/não: nada sobre o paciente ou o psicólogo.
import { createClient } from "jsr:@supabase/supabase-js@2";
import { normalizarCodigo } from "../_shared/codigo.ts";
import { lerJson, preflight, responder } from "../_shared/http.ts";

const db = createClient(Deno.env.get("SUPABASE_URL")!, Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!, {
  auth: { persistSession: false },
});

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return preflight();
  if (req.method !== "POST") return responder(405, { erro: "use_post" });

  const { json, erro } = await lerJson(req, 1024);
  if (erro) return erro;

  const codigo = normalizarCodigo((json as { codigo?: string })?.codigo ?? "");
  if (!codigo) return responder(400, { erro: "codigo_invalido" });

  const { data, error } = await db.from("pacientes").select("id").eq("codigo", codigo).maybeSingle();
  if (error) {
    console.error("check-code falhou: " + (error.code ?? "sem_codigo"));
    return responder(500, { erro: "falha_no_servidor" });
  }

  return responder(200, { existe: data !== null, codigo });
});
