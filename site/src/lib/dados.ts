// Acesso aos dados. Com Supabase configurado, fala com o banco (o RLS garante que
// cada psicólogo só vê o que é dele). Sem, usa os dados de exemplo em memória.
import { gerarCodigo } from "../../../supabase/functions/_shared/codigo.ts";
import { dadosDemo } from "./demo";
import { supabase } from "./supabase";
import type { Paciente, Sessao, SessaoResumo } from "./tipos";

export interface Api {
  listarPacientes(): Promise<Paciente[]>;
  criarPaciente(): Promise<Paciente>;
  apagarPaciente(id: string): Promise<void>;
  listarSessoes(): Promise<SessaoResumo[]>;
  obterSessao(id: string): Promise<Sessao | null>;
  apagarSessao(id: string): Promise<void>;
}

function apiSupabase(): Api {
  const db = supabase!;
  const falhou = (e: { message: string } | null) => {
    if (e) throw new Error(e.message);
  };

  return {
    async listarPacientes() {
      const { data, error } = await db.from("pacientes").select("id, codigo, criado_em").order("criado_em", { ascending: false });
      falhou(error);
      return data ?? [];
    },
    async criarPaciente() {
      // Código repetido é quase impossível (34 bits), mas se acontecer tenta de novo
      for (let tentativa = 0; tentativa < 5; tentativa++) {
        const { data, error } = await db.from("pacientes").insert({ codigo: gerarCodigo() }).select("id, codigo, criado_em").single();
        if (!error) return data as Paciente;
        if (error.code !== "23505") throw new Error(error.message);
      }
      throw new Error("Não foi possível gerar um código novo. Tente de novo.");
    },
    async apagarPaciente(id) {
      const { error } = await db.from("pacientes").delete().eq("id", id);
      falhou(error);
    },
    async listarSessoes() {
      const { data, error } = await db
        .from("sessoes_resumo")
        .select("id, paciente_id, iniciada_em, duracao_segundos, parcial, formato, resumo")
        .order("iniciada_em", { ascending: false });
      falhou(error);
      return (data ?? []) as SessaoResumo[];
    },
    async obterSessao(id) {
      const { data, error } = await db
        .from("sessoes")
        .select("id, paciente_id, iniciada_em, duracao_segundos, parcial, formato, resumo, relatorio")
        .eq("id", id)
        .maybeSingle();
      falhou(error);
      return (data as Sessao | null) ?? null;
    },
    async apagarSessao(id) {
      const { error } = await db.from("sessoes").delete().eq("id", id);
      falhou(error);
    },
  };
}

function apiDemo(): Api {
  const { pacientes, sessoes } = dadosDemo();
  const espera = () => new Promise((r) => setTimeout(r, 150));

  return {
    async listarPacientes() {
      await espera();
      return [...pacientes];
    },
    async criarPaciente() {
      await espera();
      const p = { id: crypto.randomUUID(), codigo: gerarCodigo(), criado_em: new Date().toISOString() };
      pacientes.unshift(p);
      return p;
    },
    async apagarPaciente(id) {
      await espera();
      pacientes.splice(pacientes.findIndex((p) => p.id === id), 1);
      for (let i = sessoes.length - 1; i >= 0; i--) if (sessoes[i].paciente_id === id) sessoes.splice(i, 1);
    },
    async listarSessoes() {
      await espera();
      return sessoes.map(({ relatorio: _r, ...resto }) => resto).sort((a, b) => b.iniciada_em.localeCompare(a.iniciada_em));
    },
    async obterSessao(id) {
      await espera();
      return sessoes.find((s) => s.id === id) ?? null;
    },
    async apagarSessao(id) {
      await espera();
      const i = sessoes.findIndex((s) => s.id === id);
      if (i >= 0) sessoes.splice(i, 1);
    },
  };
}

export const api: Api = supabase ? apiSupabase() : apiDemo();
