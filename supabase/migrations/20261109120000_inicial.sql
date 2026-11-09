-- Five Stages: banco do relatório para o psicólogo.
--
-- Regra de privacidade (LGPD): nenhuma tabela guarda dado do paciente.
-- O paciente existe só como um código anônimo, gerado pelo site.
-- O único dado pessoal é o e-mail do psicólogo, que fica em auth.users (Supabase Auth).

-- ---------------------------------------------------------------------------
-- Pacientes: só o código e de qual psicólogo ele é
-- ---------------------------------------------------------------------------
create table public.pacientes (
  id            uuid primary key default gen_random_uuid(),
  psicologo_id  uuid not null default auth.uid() references auth.users (id) on delete cascade,
  -- Formato XXXX-XXXX (Crockford base32). O dígito verificador é conferido no site e nas Edge Functions.
  codigo        text not null unique check (codigo ~ '^[0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]{4}$'),
  criado_em     timestamptz not null default now()
);

create index pacientes_psicologo_idx on public.pacientes (psicologo_id);

-- ---------------------------------------------------------------------------
-- Sessões: uma por partida, id = sessionId do jogo (upsert a cada envio)
-- ---------------------------------------------------------------------------
create table public.sessoes (
  id                uuid primary key,
  paciente_id       uuid not null references public.pacientes (id) on delete cascade,
  iniciada_em       timestamptz not null,
  duracao_segundos  real not null default 0 check (duracao_segundos >= 0),
  parcial           boolean not null default true,
  formato           smallint not null,
  relatorio         jsonb not null,
  -- Calculado pela Edge Function ao receber: { "escolhas": 8, "estagios": { "Negacao": 3, ... } }
  resumo            jsonb not null default '{}'::jsonb,
  recebida_em       timestamptz not null default now(),
  atualizada_em     timestamptz not null default now()
);

create index sessoes_paciente_idx on public.sessoes (paciente_id, iniciada_em desc);

-- ---------------------------------------------------------------------------
-- Row Level Security: cada psicólogo só enxerga o que é dele.
-- Mesmo com a URL e a chave pública em mãos, ninguém lê dados de outro.
-- ---------------------------------------------------------------------------
alter table public.pacientes enable row level security;
alter table public.sessoes   enable row level security;

create policy "psicologo le os proprios pacientes"
  on public.pacientes for select to authenticated
  using (psicologo_id = (select auth.uid()));

create policy "psicologo cria paciente para si"
  on public.pacientes for insert to authenticated
  with check (psicologo_id = (select auth.uid()));

create policy "psicologo apaga os proprios pacientes"
  on public.pacientes for delete to authenticated
  using (psicologo_id = (select auth.uid()));

create policy "psicologo le as sessoes dos seus pacientes"
  on public.sessoes for select to authenticated
  using (exists (
    select 1 from public.pacientes p
    where p.id = sessoes.paciente_id and p.psicologo_id = (select auth.uid())
  ));

create policy "psicologo apaga as sessoes dos seus pacientes"
  on public.sessoes for delete to authenticated
  using (exists (
    select 1 from public.pacientes p
    where p.id = sessoes.paciente_id and p.psicologo_id = (select auth.uid())
  ));

-- Sem policy de insert/update em sessoes: só a Edge Function submit-report
-- (service role, que ignora RLS) escreve sessões, depois de validar o código.

-- Ninguém de fora (anon) acessa as tabelas direto
revoke all on public.pacientes from anon;
revoke all on public.sessoes   from anon;

-- O psicólogo não altera código nem sessão: só cria, lê e apaga
revoke update on public.pacientes from authenticated;
revoke insert, update on public.sessoes from authenticated;

-- ---------------------------------------------------------------------------
-- Resumo das sessões para a lista do site (sem baixar o relatório inteiro)
-- security_invoker: a view respeita o RLS de quem consulta
-- ---------------------------------------------------------------------------
create view public.sessoes_resumo
with (security_invoker = true) as
select
  s.id,
  s.paciente_id,
  s.iniciada_em,
  s.duracao_segundos,
  s.parcial,
  s.formato,
  s.recebida_em,
  s.atualizada_em,
  s.resumo
from public.sessoes s;

revoke all on public.sessoes_resumo from anon;
