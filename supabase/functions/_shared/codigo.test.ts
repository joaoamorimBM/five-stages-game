// Roda com: node supabase/functions/_shared/codigo.test.ts
// (ou deno test). Sem dependências.
import { ALFABETO, digitoVerificador, gerarCodigo, normalizarCodigo } from "./codigo.ts";

function ok(cond: boolean, msg: string) {
  if (!cond) throw new Error("FALHOU: " + msg);
}

// Códigos gerados sempre passam na validação
for (let i = 0; i < 2000; i++) {
  const c = gerarCodigo();
  ok(normalizarCodigo(c) === c, "gerado válido " + c);
}

const base = gerarCodigo();
const crus = base.replace("-", "");

// Normaliza minúscula, espaço e falta de hífen
ok(normalizarCodigo(" " + base.toLowerCase().replace("-", " ") + " ") === base, "normaliza entrada");

// Qualquer troca de um caractere é pega (exceto 0 <-> Z)
let pegos = 0, total = 0;
for (let pos = 0; pos < 8; pos++) {
  for (const c of ALFABETO) {
    if (c === crus[pos]) continue;
    const errado = crus.slice(0, pos) + c + crus.slice(pos + 1);
    const par = (crus[pos] === "0" && c === "Z") || (crus[pos] === "Z" && c === "0");
    total++;
    if (normalizarCodigo(errado) === null) pegos++;
    else ok(par, "troca não detectada em " + errado);
  }
}

// Inversão de vizinhos é pega
for (let pos = 0; pos < 7; pos++) {
  if (crus[pos] === crus[pos + 1]) continue;
  const inv = crus.slice(0, pos) + crus[pos + 1] + crus[pos] + crus.slice(pos + 2);
  const par = (crus[pos] === "0" && crus[pos + 1] === "Z") || (crus[pos] === "Z" && crus[pos + 1] === "0");
  ok(normalizarCodigo(inv) === null || par, "inversão não detectada em " + inv);
}

// Formatos inválidos
ok(normalizarCodigo("") === null, "vazio");
ok(normalizarCodigo("ABC") === null, "curto");
ok(normalizarCodigo("ABCD-EFG!") === null, "caractere estranho");
ok(normalizarCodigo("UUUU-UUUU") === null, "U não existe no alfabeto");

// Vetor fixo: o C# e o site têm que dar o mesmo resultado
ok(digitoVerificador("K7M29QX") === "P", "vetor fixo K7M29QX -> " + digitoVerificador("K7M29QX"));

console.log("ok: " + pegos + "/" + total + " trocas detectadas, exemplo " + base);
