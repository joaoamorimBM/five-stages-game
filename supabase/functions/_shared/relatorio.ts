// Validação do relatório que o jogo envia. Sem dependências, pra rodar no Deno
// (Edge Function) e no Node (teste).
import { normalizarCodigo } from "./codigo.ts";

export const LIMITE_BYTES = 512 * 1024; // um relatório normal tem ~20 KB
export const FORMATOS_ACEITOS = [3, 4];
export const ESTAGIOS = ["Negacao", "Raiva", "Barganha", "Depressao", "Aceitacao"];

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const MAX_ITENS = 5000;

export interface Resumo {
  escolhas: number;
  estagios: Record<string, number>;
}

export interface DadosValidos {
  codigo: string;
  sessionId: string;
  iniciadaEm: string;
  duracaoSegundos: number;
  parcial: boolean;
  formato: number;
  resumo: Resumo;
}

export type Validacao = { ok: true; dados: DadosValidos } | { ok: false; erro: string };

// deno-lint-ignore no-explicit-any
type Json = any;

function lista(v: Json): v is Json[] {
  return Array.isArray(v) && v.length <= MAX_ITENS;
}

// Conta as escolhas por estágio. v3: eventos "choice"; v4: lista "choices".
export function resumir(r: Json): Resumo {
  const estagios: Record<string, number> = {};
  for (const e of ESTAGIOS) estagios[e] = 0;
  let escolhas = 0;

  const somar = (stage: unknown) => {
    if (typeof stage === "string" && stage in estagios) {
      estagios[stage]++;
      escolhas++;
    }
  };

  if (r.formatVersion === 3 && lista(r.events)) {
    for (const e of r.events) if (e && e.type === "choice") somar(e.stage);
  } else if (lista(r.choices)) {
    for (const c of r.choices) {
      const op = c && lista(c.options) ? c.options[c.chosen] : null;
      somar(op ? op.stage : undefined);
    }
  }

  return { escolhas, estagios };
}

export function validarRelatorio(r: Json): Validacao {
  if (!r || typeof r !== "object" || Array.isArray(r)) return { ok: false, erro: "corpo_invalido" };

  if (!FORMATOS_ACEITOS.includes(r.formatVersion)) return { ok: false, erro: "formato_nao_suportado" };

  const codigo = normalizarCodigo(r.patientCode);
  if (!codigo) return { ok: false, erro: "codigo_invalido" };

  if (typeof r.sessionId !== "string" || !UUID.test(r.sessionId)) return { ok: false, erro: "sessao_invalida" };

  const inicio = typeof r.startedAt === "string" ? Date.parse(r.startedAt) : NaN;
  if (Number.isNaN(inicio)) return { ok: false, erro: "data_invalida" };

  const duracao = r.durationSeconds;
  if (typeof duracao !== "number" || !Number.isFinite(duracao) || duracao < 0 || duracao > 24 * 3600) {
    return { ok: false, erro: "duracao_invalida" };
  }

  if (typeof r.partial !== "boolean") return { ok: false, erro: "parcial_invalido" };

  for (const campo of ["events", "scenes", "observables", "choices", "dialogues", "availableInteractables"]) {
    if (r[campo] !== undefined && !lista(r[campo])) return { ok: false, erro: "lista_invalida_" + campo };
  }

  return {
    ok: true,
    dados: {
      codigo,
      sessionId: r.sessionId.toLowerCase(),
      iniciadaEm: new Date(inicio).toISOString(),
      duracaoSegundos: duracao,
      parcial: r.partial,
      formato: r.formatVersion,
      resumo: resumir(r),
    },
  };
}
