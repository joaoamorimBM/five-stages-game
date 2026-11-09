// Tipos compartilhados do site.

export type Estagio = "Negacao" | "Raiva" | "Barganha" | "Depressao" | "Aceitacao";

export interface InfoEstagio {
  id: Estagio;
  nome: string;
  cor: string;   // preenchimento (faixas, pontos)
  tinta: string; // texto sobre o papel (contraste ≥ 4.5:1)
}

// Mesma ordem do enum DialogueData.GriefStage no jogo
export const ESTAGIOS: InfoEstagio[] = [
  { id: "Negacao", nome: "Negação", cor: "#3E5C7E", tinta: "#3E5C7E" },
  { id: "Raiva", nome: "Raiva", cor: "#A2452D", tinta: "#8A3A22" },
  { id: "Barganha", nome: "Barganha", cor: "#9A6A12", tinta: "#7A5410" },
  { id: "Depressao", nome: "Depressão", cor: "#514A60", tinta: "#514A60" },
  { id: "Aceitacao", nome: "Aceitação", cor: "#4F6B42", tinta: "#3F5634" },
];

export function infoEstagio(id: string | null | undefined): InfoEstagio | null {
  return ESTAGIOS.find((e) => e.id === id) ?? null;
}

// ---------- Banco ----------

export interface Paciente {
  id: string;
  codigo: string;
  criado_em: string;
}

export interface ResumoSessao {
  escolhas: number;
  estagios: Partial<Record<Estagio, number>>;
}

export interface SessaoResumo {
  id: string;
  paciente_id: string;
  iniciada_em: string;
  duracao_segundos: number;
  parcial: boolean;
  formato: number;
  resumo: ResumoSessao;
}

export interface Sessao extends SessaoResumo {
  relatorio: unknown;
}

// ---------- Relatório do jogo, formato 4 ----------
// O formato 3 (atual no jogo) é convertido em lib/relatorio.ts.

export interface RelatorioV4 {
  formatVersion: 4;
  patientCode: string;
  sessionId: string;
  gameVersion?: string;
  startedAt: string;
  endedAt: string;
  durationSeconds: number;
  partial: boolean;
  // t = segundos desde o início da sessão
  scenes: { scene: string; label: string; t: number; seconds: number }[];
  // order = ordem em que foi observado (1, 2, 3...); 0 = não observou
  observables: { scene: string; id: string; label: string; theme: string; order: number; t: number; seconds: number }[];
  choices: {
    t: number;
    scene: string;
    dialogue: string;
    speaker: string;
    prompt: string;
    options: { text: string; stage: Estagio }[];
    chosen: number;
    seconds: number;
  }[];
  dialogues: { t: number; scene: string; label: string; seconds: number; lines: number; typingSkips: number }[];
}

// ---------- Leitura organizada, igual pra todos os formatos ----------

export interface Cena {
  id: string;
  nome: string;
  inicio: number;
  duracao: number;
}

export interface Escolha {
  t: number;
  cena: string;
  dialogo: string;
  quem: string;
  fala: string;
  opcoes: { texto: string; estagio: InfoEstagio | null }[];
  escolhida: number;
  segundos: number;
  hesitou: boolean;
}

export interface Observavel {
  cena: string;
  nome: string;
  tema: string;
  ordem: number;
  t: number;
  segundos: number;
}

export interface Dialogo {
  t: number;
  cena: string;
  nome: string;
  segundos: number;
  falas: number;
  pulos: number;
}

export interface Leitura {
  formato: number;
  parcial: boolean;
  duracao: number;
  iniciadaEm: string;
  cenas: Cena[];
  escolhas: Escolha[];
  observaveis: Observavel[];
  dialogos: Dialogo[];
  tempoTipico: number; // mediana do tempo para escolher
  avisos: string[];
}
