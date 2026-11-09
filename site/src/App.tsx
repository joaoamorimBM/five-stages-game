import { useEffect, useState } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import type { Session } from "@supabase/supabase-js";
import { modoDemo, supabase } from "./lib/supabase";
import Topo from "./componentes/Topo";
import Login from "./telas/Login";
import Pacientes from "./telas/Pacientes";
import SessaoTela from "./telas/Sessao";
import Privacidade from "./telas/Privacidade";

export default function App() {
  const [sessao, setSessao] = useState<Session | null>(null);
  const [pronto, setPronto] = useState(modoDemo);

  useEffect(() => {
    if (!supabase) return;
    supabase.auth.getSession().then(({ data }) => {
      setSessao(data.session);
      setPronto(true);
    });
    const { data } = supabase.auth.onAuthStateChange((_evento, s) => setSessao(s));
    return () => data.subscription.unsubscribe();
  }, []);

  if (!pronto) return <p className="carregando" style={{ textAlign: "center" }}>Abrindo o caderno…</p>;

  const logado = modoDemo || sessao !== null;
  const email = modoDemo ? "modo demonstração" : sessao?.user.email ?? "";

  return (
    <Routes>
      <Route path="/privacidade" element={<><Topo email={logado ? email : ""} /><Privacidade /></>} />
      {logado ? (
        <>
          <Route path="/" element={<><Topo email={email} /><Pacientes /></>} />
          <Route path="/sessao/:id" element={<><Topo email={email} /><SessaoTela /></>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </>
      ) : (
        <Route path="*" element={<Login />} />
      )}
    </Routes>
  );
}
