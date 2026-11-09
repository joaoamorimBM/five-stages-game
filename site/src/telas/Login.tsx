import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { supabase } from "../lib/supabase";

// Login sem senha: o psicólogo recebe um link por e-mail
export default function Login() {
  const [email, setEmail] = useState("");
  const [estado, setEstado] = useState<"digitando" | "enviando" | "enviado">("digitando");
  const [erro, setErro] = useState("");

  async function enviar(e: FormEvent) {
    e.preventDefault();
    if (!supabase) return;
    setErro("");
    setEstado("enviando");
    const { error } = await supabase.auth.signInWithOtp({
      email: email.trim(),
      options: { emailRedirectTo: window.location.origin },
    });
    if (error) {
      setErro(error.message.includes("rate") ? "Muitas tentativas. Espere um minuto e tente de novo." : "Não deu pra enviar o link. Confira o e-mail e tente de novo.");
      setEstado("digitando");
    } else {
      setEstado("enviado");
    }
  }

  return (
    <main className="login">
      <div className="login-caixa">
        <div>
          <h1 className="titulo">Five Stages</h1>
          <p className="italico apagado" style={{ fontSize: 22 }}>caderno de sessão</p>
        </div>

        {estado === "enviado" ? (
          <div className="cartao" role="status">
            <p>Enviamos um link de acesso para <strong>{email}</strong>.</p>
            <p className="pequeno" style={{ marginTop: 8 }}>Abra o e-mail neste mesmo navegador. O link vale por uma hora.</p>
            <button type="button" className="botao-texto" onClick={() => setEstado("digitando")}>Usar outro e-mail</button>
          </div>
        ) : (
          <form onSubmit={enviar}>
            <label htmlFor="email">Seu e-mail profissional</label>
            <input
              id="email"
              className="campo"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
            <button className="botao" type="submit" disabled={estado === "enviando"}>
              {estado === "enviando" ? "Enviando…" : "Receber link de acesso"}
            </button>
            {erro && <p className="erro" role="alert">{erro}</p>}
          </form>
        )}

        <p className="nota">
          Sem senha: você entra pelo link que chega no e-mail. Os pacientes nunca entram aqui, eles só jogam com o código que você gera.
        </p>
        <Link to="/privacidade" className="sans" style={{ fontSize: 15 }}>O que guardamos e por quanto tempo</Link>
      </div>
    </main>
  );
}
