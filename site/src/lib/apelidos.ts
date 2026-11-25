// Apelidos dos pacientes ("paciente das terças"). Ficam SÓ neste navegador,
// nunca vão pro servidor: mesmo que o psicólogo escreva um nome, nós não guardamos.
import { useCallback, useEffect, useState } from "react";

const chaveDaConta = (conta: string) => "fivestages.apelidos." + conta;

function ler(chave: string): Record<string, string> {
  try {
    const bruto = localStorage.getItem(chave);
    const valor: unknown = bruto ? JSON.parse(bruto) : {};
    if (!valor || typeof valor !== "object" || Array.isArray(valor)) return {};
    return Object.fromEntries(Object.entries(valor).filter(([, apelido]) => typeof apelido === "string"));
  } catch {
    return {};
  }
}

function gravar(chave: string, mapa: Record<string, string>) {
  try {
    localStorage.setItem(chave, JSON.stringify(mapa));
  } catch {
    // navegador sem armazenamento (aba anônima bloqueada): o apelido some ao fechar
  }
}

export function useApelidos(conta: string) {
  const chave = chaveDaConta(conta);
  const [mapa, setMapa] = useState<Record<string, string>>(() => ler(chave));

  // Mantém várias abas em sincronia
  useEffect(() => {
    const aoMudar = (e: StorageEvent) => {
      if (e.key === chave || e.key === null) setMapa(ler(chave));
    };
    window.addEventListener("storage", aoMudar);
    return () => window.removeEventListener("storage", aoMudar);
  }, [chave]);

  const definir = useCallback((codigo: string, apelido: string) => {
    setMapa((atual) => {
      const novo = { ...atual };
      const limpo = apelido.trim().slice(0, 60);
      if (limpo) novo[codigo] = limpo;
      else delete novo[codigo];
      gravar(chave, novo);
      return novo;
    });
  }, [chave]);

  const importarLegados = useCallback((codigos: string[]) => {
    if (conta === "demo") return;
    const antigos = ler("fivestages.apelidos");
    setMapa((atual) => {
      const novo = { ...atual };
      for (const codigo of codigos) {
        if (!novo[codigo] && typeof antigos[codigo] === "string")
          novo[codigo] = antigos[codigo].trim().slice(0, 60);
      }
      gravar(chave, novo);
      return novo;
    });
  }, [chave, conta]);

  return { apelido: (codigo: string) => mapa[codigo] ?? "", definir, importarLegados };
}
