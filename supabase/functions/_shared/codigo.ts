// Código do paciente: 8 caracteres do alfabeto Crockford base32, mostrado como XXXX-XXXX.
// Os 7 primeiros são aleatórios e o último é um dígito verificador, pra o jogo
// pegar erro de digitação mesmo sem internet. A MESMA regra existe em
// Assets/Scripts/PatientCode.cs e em site/src/lib/codigo.ts. Mudou aqui, muda lá.
//
// Verificador: soma de (valor do caractere × posição, começando em 1) mod 31.
// Mod 31 (primo) pega qualquer troca de um caractere e qualquer inversão de
// dois vizinhos, exceto 0 ↔ Z.

export const ALFABETO = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"; // sem I, L, O, U

export function digitoVerificador(sete: string): string {
  let soma = 0;
  for (let i = 0; i < sete.length; i++) {
    soma += ALFABETO.indexOf(sete[i]) * (i + 1);
  }
  return ALFABETO[soma % 31];
}

// Aceita minúsculas, espaços, hífen e as confusões comuns (O→0, I/L→1).
// Devolve "XXXX-XXXX" ou null se o formato ou o verificador não baterem.
export function normalizarCodigo(entrada: string): string | null {
  if (typeof entrada !== "string") return null;
  const limpo = entrada
    .toUpperCase()
    .replace(/[\s-]/g, "")
    .replace(/O/g, "0")
    .replace(/[IL]/g, "1");

  if (limpo.length !== 8) return null;
  for (const c of limpo) {
    if (!ALFABETO.includes(c)) return null;
  }
  if (digitoVerificador(limpo.slice(0, 7)) !== limpo[7]) return null;

  return limpo.slice(0, 4) + "-" + limpo.slice(4);
}

export function gerarCodigo(): string {
  const bytes = new Uint8Array(7);
  crypto.getRandomValues(bytes);
  let sete = "";
  for (const b of bytes) sete += ALFABETO[b % 32]; // 256 é múltiplo de 32: sem viés
  const oito = sete + digitoVerificador(sete);
  return oito.slice(0, 4) + "-" + oito.slice(4);
}
