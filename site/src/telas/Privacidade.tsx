import { Link } from "react-router-dom";

export default function Privacidade() {
  return (
    <main className="pagina">
      <div className="pagina-dentro" style={{ maxWidth: 720 }}>
        <Link to="/" className="voltar">← Voltar</Link>
        <h1 className="titulo">O que guardamos</h1>

        <section style={{ display: "flex", flexDirection: "column", gap: 14 }}>
          <h2 className="secao-titulo" style={{ fontSize: 26 }}>Do paciente: nada que o identifique</h2>
          <p>O paciente existe aqui só como um código, por exemplo <span className="codigo">K7M2-9QXP</span>. Não pedimos nem guardamos nome, e-mail, idade ou qualquer outro dado dele.</p>
          <p>Ainda assim, o psicólogo sabe a quem cada código pertence, então os relatórios são dados <em>pseudonimizados</em> e continuam protegidos pela LGPD. Por isso o jogo mostra um termo de consentimento antes de começar.</p>
        </section>

        <section style={{ display: "flex", flexDirection: "column", gap: 14 }}>
          <h2 className="secao-titulo" style={{ fontSize: 26 }}>O que fica no servidor</h2>
          <ul style={{ margin: 0, paddingLeft: 22, display: "flex", flexDirection: "column", gap: 8 }}>
            <li>O seu e-mail, para o login.</li>
            <li>Os códigos que você gerou.</li>
            <li>As sessões jogadas: data, duração, escolhas feitas nos diálogos, objetos observados e tempos.</li>
          </ul>
          <p>Os dados ficam num servidor no Brasil (São Paulo). Cada psicólogo só consegue ver os próprios pacientes: isso é garantido pelo banco de dados, não só pelo site.</p>
        </section>

        <section style={{ display: "flex", flexDirection: "column", gap: 14 }}>
          <h2 className="secao-titulo" style={{ fontSize: 26 }}>O que fica só no seu navegador</h2>
          <p>Os apelidos que você dá aos pacientes. Eles nunca saem deste computador. Em outro navegador, os pacientes aparecem só com o código.</p>
        </section>

        <section style={{ display: "flex", flexDirection: "column", gap: 14 }}>
          <h2 className="secao-titulo" style={{ fontSize: 26 }}>Exclusão</h2>
          <p>Você pode apagar uma sessão ou um paciente inteiro a qualquer momento. Apagar o paciente apaga todas as sessões dele, e isso não pode ser desfeito.</p>
          <p className="pequeno">[Prazo de retenção a definir com os psicólogos parceiros do projeto.]</p>
        </section>

        <p className="nota">O Five Stages é um TCC de Ciência da Computação (UNIFOR). O relatório foi pensado como apoio complementar à conversa clínica e não é instrumento diagnóstico.</p>
      </div>
    </main>
  );
}
