-- Serializa os envios da mesma sessão, inclusive quando chegam simultaneamente.
create or replace function public.proteger_atualizacao_sessao()
returns trigger
language plpgsql
set search_path = public, pg_temp
as $$
declare
  anterior public.sessoes%rowtype;
begin
  perform pg_advisory_xact_lock(hashtextextended(new.id::text, 0));
  select * into anterior from public.sessoes where id = new.id for update;
  if found then
    if anterior.paciente_id <> new.paciente_id then
      raise exception 'sessao_de_outro_paciente' using errcode = '23514';
    end if;
    if (not anterior.parcial and new.parcial)
       or (anterior.parcial = new.parcial and anterior.duracao_segundos > new.duracao_segundos) then
      return null;
    end if;
  end if;
  return new;
end;
$$;

create or replace trigger proteger_atualizacao_sessao
before insert or update on public.sessoes
for each row execute function public.proteger_atualizacao_sessao();
