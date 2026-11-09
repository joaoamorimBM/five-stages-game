// Dados de EXEMPLO pro modo demonstração (sem Supabase configurado).
// Nada aqui é de paciente real: são sessões inventadas com os diálogos do jogo.
import type { Estagio, Paciente, RelatorioV4, Sessao } from "./tipos";

type Op = [string, Estagio];
interface Pergunta { cena: string; dialogo: string; quem: string; fala: string; opcoes: Op[] }

const P: Record<string, Pergunta> = {
  foto: { cena: "Scene_House", dialogo: "Foto da família", quem: "Noah", fala: "A gente tirou essa foto no aniversário dela... três meses atrás.",
    opcoes: [["Se eu tivesse saído mais cedo naquele dia... ela ainda estaria aqui.", "Barganha"], ["Parece que ela vai entrar por aquela porta a qualquer momento.", "Negacao"], ["Foi um dia lindo. Quero lembrar dela assim.", "Aceitacao"]] },
  remedios: { cena: "Scene_House", dialogo: "Remédios", quem: "Noah", fala: "O médico receitou. Não estou tomando.",
    opcoes: [["Não adianta.", "Depressao"], ["Eu não preciso disso. Eu tô bem.", "Negacao"], ["Talvez eu devesse tentar. Pela Emily.", "Aceitacao"]] },
  tv: { cena: "Scene_House", dialogo: "TV ligada", quem: "Noah", fala: "Esse é o desenho favorito dela.",
    opcoes: [["Ainda não tive coragem de desligar.", "Depressao"], ["Deixa ligada. Ela vai querer assistir quando voltar.", "Negacao"], ["Vou desligar. Mas não vou esquecer.", "Aceitacao"]] },
  celular: { cena: "Scene_House", dialogo: "Celular", quem: "Noah", fala: "\"Previsão do tempo: Chuva forte\"",
    opcoes: [["Ah, que ótimo. Mais um ponto negativo na minha vida de merda.", "Raiva"], ["Tanto faz. Nada muda mesmo.", "Depressao"], ["Melhor levar um guarda-chuva.", "Aceitacao"]] },
  louca: { cena: "Scene_House", dialogo: "Louça na pia", quem: "Noah", fala: "E a pilha de louça só aumenta.",
    opcoes: [["Amanhã eu lavo... se houver amanhã.", "Depressao"], ["Depois eu vejo isso. Não é nada demais.", "Negacao"], ["Vou lavar pelo menos uns pratos agora.", "Aceitacao"]] },
  grace1: { cena: "padaria_scene", dialogo: "Conversa com a Grace na padaria", quem: "Grace", fala: "Noah! Que bom te ver. Como você e a Emily estão?",
    opcoes: [["Bem. A gente tá bem.", "Negacao"], ["Sinceramente? Não sei, Grace. Tá difícil.", "Aceitacao"], ["Como você acha que a gente tá?", "Raiva"]] },
  grace2: { cena: "padaria_scene", dialogo: "Grace pergunta se Noah está bem", quem: "Grace", fala: "Noah... tá tudo bem?",
    opcoes: [["Tá. Tá tudo bem.", "Negacao"], ["Não... não tá.", "Aceitacao"], ["...", "Depressao"]] },
  emily1: { cena: "Scene_Alley", dialogo: "Emily encontra Noah no beco", quem: "Emily", fala: "Naquele dia... sinto que morri junto com ela",
    opcoes: [["Eu também. Às vezes acho que não vou aguentar.", "Depressao"], ["A culpa é minha. Eu devia ter protegido ela.", "Barganha"], ["Não fala isso. A gente ainda tem um ao outro.", "Aceitacao"]] },
  emily2: { cena: "Scene_Alley", dialogo: "Conversa com a Emily depois do flashback", quem: "Emily", fala: "Eu me sinto impotente.",
    opcoes: [["Eu sei, meu amor. Eu também.", "Aceitacao"], ["Se a gente tivesse chegado antes... talvez ela estivesse aqui.", "Barganha"], ["Não era pra ter sido assim! Não é justo!", "Raiva"]] },
};

const OBJETOS: [string, string][] = [
  ["Foto da família", "Memórias da Claire"], ["Carta", "Memórias da Claire"], ["TV ligada", "Memórias da Claire"],
  ["Remédios", "Autocuidado"], ["Louça na pia", "Autocuidado"], ["Torradeira", "Autocuidado"], ["Geladeira", "Autocuidado"],
  ["Emily dormindo", "Relação com a Emily"], ["Celular", "Rotina"], ["Coruja", "Rotina"],
];

const NOMES_CENA: Record<string, string> = {
  Scene_Intro: "Introdução", Scene_House: "Casa", padaria_scene: "Padaria", Acidente_Scene: "Acidente", Scene_Alley: "Beco", Dedicatoria_Scene: "Dedicatória",
};

interface Def {
  codigo: string;
  inicio: string;
  parcial: boolean;
  cenas: [string, number][]; // [cena, segundos]
  vistos: [string, number, number][]; // [objeto, t, segundos], na ordem
  escolhas: [keyof typeof P, number, number, number][]; // [pergunta, opção, t, segundos pra escolher]
  pulos: number;
}

function montar(d: Def): RelatorioV4 {
  let t = 0;
  const scenes = d.cenas.map(([scene, seconds]) => {
    const c = { scene, label: NOMES_CENA[scene] ?? scene, t, seconds };
    t += seconds;
    return c;
  });
  const visto = new Map(d.vistos.map(([nome, tt, s], i) => [nome, { order: i + 1, t: tt, seconds: s }]));
  const temCasa = d.cenas.some(([c]) => c === "Scene_House");
  const observables = temCasa
    ? OBJETOS.map(([label, theme]) => {
        const v = visto.get(label);
        return { scene: "Scene_House", id: label, label, theme, order: v?.order ?? 0, t: v?.t ?? 0, seconds: v?.seconds ?? 0 };
      })
    : [];
  const choices = d.escolhas.map(([k, chosen, tt, seconds]) => {
    const p = P[k];
    return { t: tt, scene: p.cena, dialogue: p.dialogo, speaker: p.quem, prompt: p.fala,
      options: p.opcoes.map(([text, stage]) => ({ text, stage })), chosen, seconds };
  });
  const dialogues = d.vistos.map(([label, tt, s]) => ({ t: tt, scene: "Scene_House", label, seconds: s, lines: 2, typingSkips: Math.min(2, d.pulos) }));
  const fim = new Date(Date.parse(d.inicio) + t * 1000).toISOString();

  return {
    formatVersion: 4, patientCode: d.codigo, sessionId: crypto.randomUUID(), gameVersion: "demo",
    startedAt: d.inicio, endedAt: fim, durationSeconds: t, partial: d.parcial,
    scenes, observables, choices, dialogues,
  };
}

const SESSOES: Def[] = [
  { codigo: "K7M2-9QXP", inicio: "2026-11-09T18:00:00Z", parcial: false, pulos: 1,
    cenas: [["Scene_Intro", 20], ["Scene_House", 170], ["padaria_scene", 290], ["Acidente_Scene", 100], ["Scene_Alley", 230], ["Dedicatoria_Scene", 32]],
    vistos: [["Foto da família", 22, 12], ["Remédios", 38, 9], ["Carta", 47, 18], ["TV ligada", 56, 6], ["Celular", 78, 5], ["Emily dormindo", 100, 7]],
    escolhas: [["foto", 1, 25, 6.8], ["remedios", 1, 41, 2.9], ["tv", 0, 58, 3.4], ["celular", 2, 80, 1.8], ["grace1", 0, 252, 2.2], ["grace2", 2, 400, 7.5], ["emily1", 1, 605, 5.1], ["emily2", 0, 750, 3.0]] },
  { codigo: "K7M2-9QXP", inicio: "2026-11-02T18:05:00Z", parcial: false, pulos: 2,
    cenas: [["Scene_Intro", 18], ["Scene_House", 120], ["padaria_scene", 260], ["Acidente_Scene", 95], ["Scene_Alley", 200], ["Dedicatoria_Scene", 27]],
    vistos: [["Foto da família", 25, 8], ["Remédios", 40, 5], ["Louça na pia", 52, 4], ["Celular", 70, 3]],
    escolhas: [["foto", 1, 28, 5.0], ["remedios", 1, 43, 3.1], ["louca", 0, 55, 2.6], ["celular", 0, 72, 2.0], ["grace1", 0, 200, 2.4], ["grace2", 0, 350, 4.0], ["emily1", 0, 520, 6.2], ["emily2", 1, 640, 3.5]] },
  { codigo: "K7M2-9QXP", inicio: "2026-10-26T18:10:00Z", parcial: true, pulos: 2,
    cenas: [["Scene_Intro", 22], ["Scene_House", 210], ["padaria_scene", 128]],
    vistos: [["Foto da família", 30, 10], ["TV ligada", 60, 7], ["Celular", 95, 4], ["Remédios", 130, 6], ["Coruja", 170, 3]],
    escolhas: [["foto", 1, 33, 4.2], ["tv", 1, 63, 3.0], ["celular", 0, 98, 1.5], ["remedios", 0, 133, 2.8]] },
  { codigo: "R4TN-8WK8", inicio: "2026-11-05T14:30:00Z", parcial: true, pulos: 2,
    cenas: [["Scene_Intro", 20], ["Scene_House", 220]],
    vistos: [["Celular", 40, 3], ["TV ligada", 70, 4]],
    escolhas: [["celular", 0, 42, 1.2], ["tv", 0, 72, 2.0]] },
];

function resumo(r: RelatorioV4) {
  const estagios: Partial<Record<Estagio, number>> = {};
  for (const c of r.choices) {
    const s = c.options[c.chosen]?.stage;
    if (s) estagios[s] = (estagios[s] ?? 0) + 1;
  }
  return { escolhas: r.choices.length, estagios };
}

export function dadosDemo(): { pacientes: Paciente[]; sessoes: Sessao[] } {
  const pacientes: Paciente[] = [
    { id: "p1", codigo: "K7M2-9QXP", criado_em: "2026-10-20T12:00:00Z" },
    { id: "p2", codigo: "R4TN-8WK8", criado_em: "2026-11-01T12:00:00Z" },
    { id: "p3", codigo: "B9HX-2MDE", criado_em: "2026-11-08T12:00:00Z" },
  ];
  const sessoes: Sessao[] = SESSOES.map((d) => {
    const r = montar(d);
    return {
      id: r.sessionId,
      paciente_id: pacientes.find((p) => p.codigo === d.codigo)!.id,
      iniciada_em: r.startedAt,
      duracao_segundos: r.durationSeconds,
      parcial: r.partial,
      formato: 4,
      resumo: resumo(r),
      relatorio: r,
    };
  });
  return { pacientes, sessoes };
}
