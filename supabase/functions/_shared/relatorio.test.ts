// Roda com: node supabase/functions/_shared/relatorio.test.ts
import { gerarCodigo } from "./codigo.ts";
import { validarRelatorio } from "./relatorio.ts";

function ok(cond: boolean, msg: string) {
  if (!cond) throw new Error("FALHOU: " + msg);
}

const codigo = gerarCodigo();
const v3 = {
  formatVersion: 3,
  patientCode: codigo.toLowerCase(),
  sessionId: "49c96ad6-9cd5-4589-a9ab-c593b521be6a",
  startedAt: "2026-10-28T14:01:10.3848245Z",
  endedAt: "2026-10-28T14:02:22.1868477Z",
  durationSeconds: 71.8,
  partial: true,
  events: [
    { t: 0, scene: "Scene_Intro", type: "sceneChange", target: "Scene_Intro" },
    { t: 25, scene: "Scene_House", type: "choice", target: "Remédios", stage: "Negacao" },
    { t: 30, scene: "Scene_House", type: "choice", target: "TV ligada", stage: "Depressao" },
    { t: 31, scene: "Scene_House", type: "choice", target: "X", stage: "Inventado" },
  ],
  availableInteractables: [],
};

const r = validarRelatorio(v3);
ok(r.ok, "v3 válido");
if (r.ok) {
  ok(r.dados.codigo === codigo, "normaliza código");
  ok(r.dados.resumo.escolhas === 2, "ignora estágio inventado");
  ok(r.dados.resumo.estagios.Negacao === 1 && r.dados.resumo.estagios.Depressao === 1, "conta estágios");
  ok(r.dados.iniciadaEm === "2026-10-28T14:01:10.384Z", "data em ISO");
}

const v4 = {
  formatVersion: 4, patientCode: codigo, sessionId: crypto.randomUUID(),
  startedAt: new Date().toISOString(), durationSeconds: 10, partial: false,
  choices: [
    { options: [{ text: "a", stage: "Raiva" }, { text: "b", stage: "Aceitacao" }], chosen: 1 },
    { options: [{ text: "a", stage: "Raiva" }], chosen: 5 },
  ],
};
const r4 = validarRelatorio(v4);
ok(r4.ok && r4.dados.resumo.escolhas === 1 && r4.dados.resumo.estagios.Aceitacao === 1, "v4 conta a opção escolhida");

const erro = (mudanca: object) => {
  const x = validarRelatorio({ ...v3, ...mudanca });
  return x.ok ? "ok" : x.erro;
};
ok(erro({ formatVersion: 2 }) === "formato_nao_suportado", "formato antigo");
ok(erro({ patientCode: "" }) === "codigo_invalido", "sem código");
ok(erro({ patientCode: "AAAA-AAAA" }) === "codigo_invalido", "verificador errado");
ok(erro({ sessionId: "x" }) === "sessao_invalida", "uuid");
ok(erro({ startedAt: "ontem" }) === "data_invalida", "data");
ok(erro({ durationSeconds: -1 }) === "duracao_invalida", "duração negativa");
ok(erro({ partial: "sim" }) === "parcial_invalido", "parcial");
ok(erro({ events: new Array(6000).fill({}) }) === "lista_invalida_events", "lista gigante");
ok(validarRelatorio(null).ok === false && validarRelatorio([]).ok === false, "corpo estranho");

console.log("ok: validação do relatório");
