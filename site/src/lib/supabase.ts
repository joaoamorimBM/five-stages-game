import { createClient, type SupabaseClient } from "@supabase/supabase-js";

// Só a chave PÚBLICA entra no site. Quem protege os dados é o RLS do banco.
const url = import.meta.env.VITE_SUPABASE_URL as string | undefined;
const chave = (import.meta.env.VITE_SUPABASE_ANON_KEY as string | undefined)?.trim();

// Sem as variáveis, o site roda em modo demonstração (dados de exemplo, nada sai do navegador)
export const supabase: SupabaseClient | null =
  url && chave ? createClient(url, chave, { auth: { persistSession: true, detectSessionInUrl: true } }) : null;

export const modoDemo = supabase === null;
