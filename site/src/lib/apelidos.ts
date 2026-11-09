// Apelidos dos pacientes ("paciente das terças"). Ficam SÓ neste navegador,
// nunca vão pro servidor: mesmo que o psicólogo escreva um nome, nós não guardamos.
import { useCallback, useEffect, useState } from "react";

const CHAVE = "fivestages.apelidos";

function ler(): Record<string, string> {
  try {
    const bruto = localStorage.getItem(CHAVE);
    return bruto ? (JSON.parse(bruto) as Record<string, string>) : {};
  } catch {
    return {};
  }
}

function gravar(mapa: Record<string, string>) {
  try {
    localStorage.setItem(CHAVE, JSON.stringify(mapa));
  } catch {
    // navegador sem armazenamento (aba anônima bloqueada): o apelido some ao fechar
  }
}

export function useApelidos() {
  const [mapa, setMapa] = useState<Record<string, string>>(ler);

  // Mantém várias abas em sincronia
  useEffect(() => {
    const aoMudar = (e: StorageEvent) => {
      if (e.key === CHAVE) setMapa(ler());
    };
    window.addEventListener("storage", aoMudar);
    return () => window.removeEventListener("storage", aoMudar);
  }, []);

  const definir = useCallback((codigo: string, apelido: string) => {
    setMapa((atual) => {
      const novo = { ...atual };
      const limpo = apelido.trim().slice(0, 60);
      if (limpo) novo[codigo] = limpo;
      else delete novo[codigo];
      gravar(novo);
      return novo;
    });
  }, []);

  return { apelido: (codigo: string) => mapa[codigo] ?? "", definir };
}
