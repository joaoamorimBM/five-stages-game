import { Link } from "react-router-dom";
import { modoDemo, supabase } from "../lib/supabase";

export default function Topo({ email }: { email: string }) {
  return (
    <header className="topo">
      <div className="topo-dentro">
        <Link to="/" className="marca">
          <span className="marca-nome">Five Stages</span>
          <span className="marca-sub">caderno de sessão</span>
        </Link>
        <div className="conta">
          {modoDemo && <span className="selo-demo">Dados de exemplo</span>}
          {!modoDemo && email && <span>{email}</span>}
          {supabase && email && (
            <button type="button" className="botao-texto" onClick={() => supabase!.auth.signOut()}>
              Sair
            </button>
          )}
        </div>
      </div>
    </header>
  );
}
