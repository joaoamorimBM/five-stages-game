// Transforma o JSON cru do jogo numa leitura organizada pro psicólogo.
// Tudo aqui é regra fixa e explicável: nenhuma IA interpreta a sessão.
import { fmtDuracao, juntar } from "./formato";
import { ESTAGIOS, infoEstagio, type Cena, type Dialogo, type Escolha, type Estagio, type Leitura, type Observavel, type RelatorioV4 } from "./tipos";

const NOMES_CENA: Record<string, string> = {
  Scene_Intro: "Introdução",
  Scene_Room: "Quarto",
  Scene_House: "Casa",
  Scene_Alley: "Beco",
  padaria_scene: "Padaria",
  Acidente_Scene: "Acidente",
  Dedicatoria_Scene: "Dedicatória",
};
const NA_CENA: Record<string, string> = {
  Casa: "Na casa", Quarto: "No quarto", Beco: "No beco", Padaria: "Na padaria", Introdução: "Na introdução", Acidente: "No acidente",
};

export const nomeCena = (id: string) => NOMES_CENA[id] ?? id;

// Hesitou = levou mais de 1,5× o tempo típico DELE nesta sessão (e pelo menos 1,5 s a mais)
export const FATOR_HESITACAO = 1.5;

function mediana(valores: number[]): number {
  if (valores.length === 0) return 0;
  const v = [...valores].sort((a, b) => a - b);
  const m = Math.floor(v.length / 2);
  return v.length % 2 ? v[m] : (v[m - 1] + v[m]) / 2;
}

// deno-lint-ignore no-explicit-any
type Json = any;
const num = (v: unknown, padrao = 0) => (typeof v === "number" && Number.isFinite(v) ? v : padrao);
const str = (v: unknown, padrao = "") => (typeof v === "string" ? v : padrao);
const arr = (v: unknown): Json[] => (Array.isArray(v) ? v : []);

// ---------- Formato 4 ----------

function lerV4(r: RelatorioV4): Omit<Leitura, "tempoTipico"> {
  return {
    formato: 4,
    parcial: !!r.partial,
    duracao: num(r.durationSeconds),
    iniciadaEm: str(r.startedAt),
    cenas: arr(r.scenes).map((c): Cena => ({ id: str(c.scene), nome: str(c.label) || nomeCena(str(c.scene)), inicio: num(c.t), duracao: num(c.seconds) })),
    escolhas: arr(r.choices).map((c): Escolha => ({
      t: num(c.t),
      cena: nomeCena(str(c.scene)),
      dialogo: str(c.dialogue),
      quem: str(c.speaker),
      fala: str(c.prompt).replace(/^"(.*)"$/, "$1"), // a tela já põe aspas
      opcoes: arr(c.options).map((o) => ({ texto: str(o.text), estagio: infoEstagio(o.stage) })),
      escolhida: num(c.chosen, -1),
      segundos: num(c.seconds),
      hesitou: false,
    })),
    observaveis: arr(r.observables).map((o): Observavel => ({
      cena: nomeCena(str(o.scene)), nome: str(o.label) || str(o.id), tema: str(o.theme) || "Outros", ordem: num(o.order), t: num(o.t), segundos: num(o.seconds),
    })),
    dialogos: arr(r.dialogues).map((d): Dialogo => ({
      t: num(d.t), cena: nomeCena(str(d.scene)), nome: str(d.label), segundos: num(d.seconds), falas: num(d.lines), pulos: num(d.typingSkips),
    })),
    avisos: [],
  };
}

// ---------- Formato 3 (versão atual do jogo) ----------

function lerV3(r: Json): Omit<Leitura, "tempoTipico"> {
  const eventos = arr(r.events);
  const duracao = num(r.durationSeconds);

  const trocas = eventos.filter((e) => e.type === "sceneChange");
  const cenas: Cena[] = trocas.map((e, i) => ({
    id: str(e.target),
    nome: nomeCena(str(e.target)),
    inicio: num(e.t),
    duracao: Math.max(0, (i + 1 < trocas.length ? num(trocas[i + 1].t) : duracao) - num(e.t)),
  }));

  const escolhas: Escolha[] = eventos
    .filter((e) => e.type === "choice")
    .map((e) => ({
      t: num(e.t), cena: nomeCena(str(e.scene)), dialogo: str(e.target), quem: "", fala: "",
      opcoes: [{ texto: str(e.choice), estagio: infoEstagio(e.stage) }], escolhida: 0, segundos: num(e.seconds), hesitou: false,
    }));

  const dialogos: Dialogo[] = eventos
    .filter((e) => e.type === "dialogue")
    .map((e) => ({ t: num(e.t), cena: nomeCena(str(e.scene)), nome: str(e.target), segundos: num(e.seconds), falas: num(e.lines), pulos: num(e.typingSkips) }));

  const observaveis: Observavel[] = [];
  for (const s of arr(r.availableInteractables)) {
    const interacoes = eventos.filter((e) => e.type === "interact" && e.scene === s.scene);
    for (const nome of arr(s.objects).map((o) => str(o))) {
      const i = interacoes.findIndex((e) => e.target === nome);
      const ev = i >= 0 ? interacoes[i] : null;
      const dialogo = ev ? dialogos.find((d) => d.t >= num(ev.t) && d.t - num(ev.t) < 1) : undefined;
      observaveis.push({ cena: nomeCena(str(s.scene)), nome, tema: "Objetos", ordem: i + 1, t: ev ? num(ev.t) : 0, segundos: dialogo?.segundos ?? 0 });
    }
  }

  return {
    formato: 3, parcial: !!r.partial, duracao, iniciadaEm: str(r.startedAt), cenas, escolhas, observaveis, dialogos,
    avisos: ["Esta sessão veio de uma versão anterior do jogo (formato 3): as opções não escolhidas e os temas dos objetos não foram registrados."],
  };
}

export function lerRelatorio(json: unknown): Leitura {
  const r = (json ?? {}) as Json;
  const base = r.formatVersion === 4 ? lerV4(r as RelatorioV4) : lerV3(r);

  const tempoTipico = mediana(base.escolhas.map((e) => e.segundos));
  if (base.escolhas.length >= 3) {
    for (const e of base.escolhas) {
      e.hesitou = e.segundos > tempoTipico * FATOR_HESITACAO && e.segundos - tempoTipico >= 1.5;
    }
  }
  return { ...base, tempoTipico };
}

// ---------- Contas ----------

export function contarEstagios(l: Leitura): Record<Estagio, number> {
  const c = { Negacao: 0, Raiva: 0, Barganha: 0, Depressao: 0, Aceitacao: 0 } as Record<Estagio, number>;
  for (const e of l.escolhas) {
    const est = e.opcoes[e.escolhida]?.estagio;
    if (est) c[est.id]++;
  }
  return c;
}

export interface Tema { nome: string; total: number; vistos: number; itens: Observavel[] }
export interface Exploracao { cena: string; total: number; vistos: number; nivel: string; temas: Tema[] }

export function nivelExploracao(fracao: number): string {
  return fracao >= 0.7 ? "explorou bastante" : fracao >= 0.35 ? "explorou parte do cenário" : "explorou pouco";
}

export function explorar(l: Leitura): Exploracao[] {
  const porCena = new Map<string, Observavel[]>();
  for (const o of l.observaveis) porCena.set(o.cena, [...(porCena.get(o.cena) ?? []), o]);

  return [...porCena.entries()].map(([cena, itens]) => {
    const porTema = new Map<string, Observavel[]>();
    for (const o of itens) porTema.set(o.tema, [...(porTema.get(o.tema) ?? []), o]);
    const temas = [...porTema.entries()].map(([nome, lista]) => ({
      nome,
      total: lista.length,
      vistos: lista.filter((o) => o.ordem > 0).length,
      // observados primeiro, na ordem; depois os que ficaram de fora
      itens: [...lista].sort((a, b) => (a.ordem || 999) - (b.ordem || 999)),
    }));
    const vistos = itens.filter((o) => o.ordem > 0).length;
    return { cena, total: itens.length, vistos, nivel: nivelExploracao(vistos / itens.length), temas };
  });
}

export function ritmoLeitura(l: Leitura) {
  const falas = l.dialogos.reduce((s, d) => s + d.falas, 0);
  const pulos = l.dialogos.reduce((s, d) => s + d.pulos, 0);
  const fracao = falas > 0 ? pulos / falas : 0;
  return { dialogos: l.dialogos.length, falas, fracao };
}

// ---------- Resumo em frases (regras fixas) ----------

const nome = (id: Estagio) => ESTAGIOS.find((e) => e.id === id)!.nome.toLowerCase();

export function frasesResumo(l: Leitura): string[] {
  const frases: string[] = [];
  const total = l.escolhas.length;

  if (total === 0) {
    frases.push("Nenhuma escolha foi registrada nesta sessão.");
  } else {
    const c = contarEstagios(l);
    const ordem = ESTAGIOS.map((e) => e.id).filter((id) => c[id] > 0).sort((a, b) => c[b] - c[a]);
    const max = c[ordem[0]];
    const topo = ordem.filter((id) => c[id] === max);
    const resto = ordem.filter((id) => c[id] < max);

    let f = topo.length === 1
      ? `Nas escolhas, as respostas tenderam à ${nome(topo[0])} (${max} de ${total})`
      : `Nas escolhas, as respostas se dividiram entre ${juntar(topo.map(nome))} (${max} cada, de ${total})`;

    if (resto.length > 0) {
      const segundo = c[resto[0]];
      const empatados = resto.filter((id) => c[id] === segundo);
      f += empatados.length === 1
        ? `, seguidas de ${nome(empatados[0])} (${segundo})`
        : `, seguidas de ${juntar(empatados.map(nome))} (${segundo} cada)`;
    }
    f += ".";

    const zerados = ESTAGIOS.map((e) => e.id).filter((id) => c[id] === 0);
    if (zerados.length > 0 && zerados.length <= 2) {
      f += ` Nenhuma resposta de ${juntar(zerados.map(nome))}.`;
    }
    frases.push(f);
  }

  for (const ex of explorar(l)) {
    const prefixo = NA_CENA[ex.cena] ?? `Em ${ex.cena}`;
    let f = `${prefixo}, observou ${ex.vistos} de ${ex.total} objetos`;
    const cheios = ex.temas.filter((t) => t.total >= 2 && t.vistos === t.total).map((t) => `“${t.nome}”`);
    const vazios = ex.temas.filter((t) => t.total >= 3 && t.vistos / t.total <= 0.34);
    const partes: string[] = [];
    if (cheios.length) partes.push(`todos os do tema ${juntar(cheios)}`);
    for (const t of vazios) partes.push(`só ${t.vistos} de ${t.total} do tema “${t.nome}”`);
    if (partes.length) f += `: ${partes.join(cheios.length && vazios.length ? ", mas " : "; ")}`;
    frases.push(f + ".");
  }

  const hes = l.escolhas.filter((e) => e.hesitou);
  if (hes.length > 0) {
    frases.push(
      `Demorou mais para responder em ${hes.length === 1 ? "1 momento" : hes.length + " momentos"}: ${juntar(hes.map((e) => e.dialogo))}.`,
    );
  }

  if (l.parcial) frases.push(`A sessão foi interrompida antes do fim, depois de ${fmtDuracao(l.duracao)}.`);

  return frases;
}
