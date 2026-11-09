// Formatação em português (datas, durações, segundos).

const dataLonga = new Intl.DateTimeFormat("pt-BR", { day: "numeric", month: "long" });
const dataCurta = new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "short", year: "numeric" });
const dataHora = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" });

export const fmtDataLonga = (iso: string) => dataLonga.format(new Date(iso));
export const fmtDataCurta = (iso: string) => dataCurta.format(new Date(iso)).replace(/\./g, "").replace(/ de /g, " ");
export const fmtDataHora = (iso: string) => dataHora.format(new Date(iso));

// 842 → "14 min 02 s"
export function fmtDuracao(segundos: number): string {
  const total = Math.max(0, Math.round(segundos));
  const min = Math.floor(total / 60);
  const s = total % 60;
  if (min === 0) return s + " s";
  return min + " min " + String(s).padStart(2, "0") + " s";
}

// 842 → "14 min"
export function fmtMinutos(segundos: number): string {
  const min = Math.round(segundos / 60);
  return min < 1 ? "menos de 1 min" : min + " min";
}

// 25 → "00:25"
export function fmtRelogio(segundos: number): string {
  const total = Math.max(0, Math.floor(segundos));
  return String(Math.floor(total / 60)).padStart(2, "0") + ":" + String(total % 60).padStart(2, "0");
}

// 6.8 → "6,8 s"
export const fmtSegundos = (s: number) => s.toLocaleString("pt-BR", { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + " s";

// Junta uma lista em português: "a, b e c"
export function juntar(itens: string[]): string {
  if (itens.length <= 1) return itens.join("");
  return itens.slice(0, -1).join(", ") + " e " + itens[itens.length - 1];
}
