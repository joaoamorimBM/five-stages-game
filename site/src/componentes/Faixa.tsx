import { ESTAGIOS, type ResumoSessao } from "../lib/tipos";

// Faixa fina com a proporção de cada estágio nas escolhas de uma sessão
export default function Faixa({ resumo }: { resumo: ResumoSessao }) {
  const partes = ESTAGIOS.map((e) => ({ ...e, n: resumo.estagios?.[e.id] ?? 0 })).filter((e) => e.n > 0);
  const descricao = partes.length
    ? partes.map((p) => `${p.nome}: ${p.n}`).join(", ")
    : "nenhuma escolha registrada";

  return (
    <span className="faixa" role="img" aria-label={"Escolhas por estágio. " + descricao}>
      {partes.map((p) => (
        <span key={p.id} style={{ flex: `${p.n} 1 0`, background: p.cor }} />
      ))}
    </span>
  );
}
