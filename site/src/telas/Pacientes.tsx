import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import Faixa from "../componentes/Faixa";
import { IconeCopiar, IconeMais } from "../componentes/Icones";
import { useApelidos } from "../lib/apelidos";
import { api } from "../lib/dados";
import { fmtDataCurta, fmtMinutos } from "../lib/formato";
import { ESTAGIOS, type Paciente, type SessaoResumo } from "../lib/tipos";

// "Da 1ª para a 3ª sessão, as respostas de aceitação passaram de 0 para 2."
function tendencia(sessoes: SessaoResumo[]): string {
  if (sessoes.length < 2) return "";
  const antiga = sessoes[sessoes.length - 1].resumo.estagios ?? {};
  const nova = sessoes[0].resumo.estagios ?? {};
  let melhor = { nome: "", de: 0, para: 0, dif: 0 };
  for (const e of ESTAGIOS) {
    const de = antiga[e.id] ?? 0;
    const para = nova[e.id] ?? 0;
    if (Math.abs(para - de) > Math.abs(melhor.dif)) melhor = { nome: e.nome.toLowerCase(), de, para, dif: para - de };
  }
  if (melhor.dif === 0) return `Da 1ª para a ${sessoes.length}ª sessão, a distribuição das respostas ficou parecida.`;
  return `Da 1ª para a ${sessoes.length}ª sessão, as respostas de ${melhor.nome} passaram de ${melhor.de} para ${melhor.para}.`;
}

export default function Pacientes() {
  const [pacientes, setPacientes] = useState<Paciente[] | null>(null);
  const [sessoes, setSessoes] = useState<SessaoResumo[]>([]);
  const [erro, setErro] = useState("");
  const [novo, setNovo] = useState<Paciente | null>(null);
  const [criando, setCriando] = useState(false);
  const [copiado, setCopiado] = useState(false);
  const [abertos, setAbertos] = useState<Set<string>>(new Set());
  const [editando, setEditando] = useState<string | null>(null);
  const [rascunho, setRascunho] = useState("");
  const [apagando, setApagando] = useState<string | null>(null);
  const { apelido, definir } = useApelidos();

  async function carregar() {
    try {
      const [p, s] = await Promise.all([api.listarPacientes(), api.listarSessoes()]);
      setPacientes(p);
      setSessoes(s);
      // Abre o primeiro paciente com sessões, pra tela não começar vazia
      setAbertos((atual) => (atual.size ? atual : new Set(p.filter((x) => s.some((y) => y.paciente_id === x.id)).slice(0, 1).map((x) => x.id))));
    } catch {
      setErro("Não foi possível carregar seus pacientes. Confira a conexão e recarregue a página.");
    }
  }

  useEffect(() => {
    carregar();
  }, []);

  const porPaciente = useMemo(() => {
    const mapa = new Map<string, SessaoResumo[]>();
    for (const s of sessoes) mapa.set(s.paciente_id, [...(mapa.get(s.paciente_id) ?? []), s]);
    return mapa;
  }, [sessoes]);

  async function criar() {
    setCriando(true);
    setCopiado(false);
    try {
      const p = await api.criarPaciente();
      setNovo(p);
      setPacientes((atual) => [p, ...(atual ?? [])]);
    } catch {
      setErro("Não foi possível criar o paciente. Tente de novo.");
    } finally {
      setCriando(false);
    }
  }

  async function copiar(codigo: string) {
    try {
      await navigator.clipboard.writeText(codigo);
      setCopiado(true);
    } catch {
      setCopiado(false);
    }
  }

  function alternar(id: string) {
    setAbertos((atual) => {
      const novoSet = new Set(atual);
      if (novoSet.has(id)) novoSet.delete(id);
      else novoSet.add(id);
      return novoSet;
    });
  }

  function salvarApelido(e: FormEvent, codigo: string) {
    e.preventDefault();
    definir(codigo, rascunho);
    setEditando(null);
  }

  async function apagar(p: Paciente) {
    try {
      await api.apagarPaciente(p.id);
      definir(p.codigo, "");
      setPacientes((atual) => (atual ?? []).filter((x) => x.id !== p.id));
      setSessoes((atual) => atual.filter((s) => s.paciente_id !== p.id));
      if (novo?.id === p.id) setNovo(null);
    } catch {
      setErro("Não foi possível apagar o paciente. Tente de novo.");
    } finally {
      setApagando(null);
    }
  }

  return (
    <main className="pagina">
      <div className="pagina-dentro">
        <div className="linha">
          <section className="principal">
            <div className="cabeca-secao" style={{ alignItems: "flex-end" }}>
              <div>
                <h1 className="titulo">Seus pacientes</h1>
                <p className="italico apagado" style={{ marginTop: 10 }}>Cada paciente é um código. Nenhum nome passa por aqui.</p>
              </div>
              <button type="button" className="botao" onClick={criar} disabled={criando}>
                <IconeMais />
                {criando ? "Gerando…" : "Novo paciente"}
              </button>
            </div>

            {erro && <p className="erro" role="alert">{erro}</p>}

            {novo && (
              <div className="cartao novo-codigo" role="status">
                <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                  <span className="rotulo">Código gerado</span>
                  <span className="codigo">{novo.codigo}</span>
                  <span className="apagado" style={{ fontSize: 16 }}>Digite este código na tela inicial do jogo, antes do paciente começar.</span>
                </div>
                <button type="button" className="botao-linha" onClick={() => copiar(novo.codigo)}>
                  <IconeCopiar />
                  {copiado ? "Copiado" : "Copiar"}
                </button>
              </div>
            )}

            {pacientes === null ? (
              <p className="carregando">Carregando…</p>
            ) : pacientes.length === 0 ? (
              <p className="vazio">Você ainda não tem pacientes. Use “Novo paciente” para gerar o primeiro código.</p>
            ) : (
              <div className="lista">
                {pacientes.map((p) => {
                  const lista = porPaciente.get(p.id) ?? [];
                  const aberto = abertos.has(p.id);
                  const nome = apelido(p.codigo);
                  const n = lista.length;
                  const resumoLinha = n === 0 ? "nenhuma sessão ainda" : `${n === 1 ? "1 sessão" : n + " sessões"} · última em ${fmtDataCurta(lista[0].iniciada_em)}`;
                  const nota = tendencia(lista);

                  return (
                    <article key={p.id} className="paciente">
                      <div className="paciente-linha">
                        <span className="codigo">{p.codigo}</span>

                        {editando === p.codigo ? (
                          <form className="apelido-form" onSubmit={(e) => salvarApelido(e, p.codigo)}>
                            <label htmlFor={"apelido-" + p.id} className="sans" style={{ position: "absolute", left: -9999 }}>Apelido de {p.codigo}</label>
                            <input
                              id={"apelido-" + p.id}
                              className="campo"
                              value={rascunho}
                              maxLength={60}
                              placeholder="ex.: paciente das terças"
                              autoFocus
                              onChange={(e) => setRascunho(e.target.value)}
                            />
                            <button type="submit" className="botao-linha">Salvar</button>
                            <button type="button" className="botao-texto" onClick={() => setEditando(null)}>Cancelar</button>
                          </form>
                        ) : (
                          <div className="apelido">
                            {nome ? <span className="apelido-texto">{nome}</span> : <span className="italico apagado">sem apelido</span>}
                            <button
                              type="button"
                              className="botao-texto"
                              onClick={() => {
                                setRascunho(nome);
                                setEditando(p.codigo);
                              }}
                            >
                              {nome ? "editar" : "dar apelido"}
                            </button>
                          </div>
                        )}

                        <span className="pequeno" style={{ minWidth: 170 }}>{resumoLinha}</span>
                        <button type="button" className="botao-linha" aria-expanded={aberto} onClick={() => alternar(p.id)}>
                          {aberto ? "Recolher" : "Ver sessões"}
                        </button>
                      </div>

                      {aberto && (
                        <div className="sessoes-do-paciente">
                          <div className="sessoes-lista">
                            {lista.map((s) => (
                              <Link key={s.id} to={"/sessao/" + s.id} className="sessao-item">
                                <span className="data">{fmtDataCurta(s.iniciada_em)}</span>
                                <span className="pequeno" style={{ minWidth: 72 }}>{fmtMinutos(s.duracao_segundos)}</span>
                                <span className={"situacao " + (s.parcial ? "situacao-parcial" : "situacao-ok")}>{s.parcial ? "incompleta" : "completa"}</span>
                                <Faixa resumo={s.resumo} />
                                <span className="abrir">Abrir</span>
                              </Link>
                            ))}
                            {n === 0 && (
                              <p className="vazio">Aguardando a primeira sessão. Ela aparece aqui quando o jogo enviar o relatório.</p>
                            )}

                            {apagando === p.id ? (
                              <div className="confirmar" role="alert" style={{ marginTop: 12 }}>
                                <span>
                                  Apagar o paciente {p.codigo}
                                  {n > 0 ? ` e ${n === 1 ? "a sessão dele" : "as " + n + " sessões dele"}` : ""}? Não dá pra desfazer.
                                </span>
                                <button type="button" className="botao-linha botao-perigo" onClick={() => apagar(p)}>Apagar</button>
                                <button type="button" className="botao-texto" onClick={() => setApagando(null)}>Cancelar</button>
                              </div>
                            ) : (
                              <button type="button" className="botao-texto" style={{ alignSelf: "flex-start", marginTop: 8, color: "var(--alerta)" }} onClick={() => setApagando(p.id)}>
                                Apagar paciente
                              </button>
                            )}
                          </div>
                          {nota && (
                            <aside className="nota" style={{ flex: "1 1 220px" }}>
                              <p>{nota}</p>
                            </aside>
                          )}
                        </div>
                      )}
                    </article>
                  );
                })}
              </div>
            )}
          </section>

          <aside className="margem" style={{ paddingTop: 8 }}>
            <div className="nota nota-reta">
              <h2>Como funciona</h2>
              <ol style={{ margin: 0, paddingLeft: 20, display: "flex", flexDirection: "column", gap: 8, fontSize: 16 }}>
                <li>Gere um código para o paciente.</li>
                <li>Digite o código na tela inicial do jogo.</li>
                <li>Ao fim da sessão, o relatório aparece aqui. Sem internet, o jogo envia depois.</li>
              </ol>
            </div>
            <div className="nota nota-reta">
              <h2>O que guardamos</h2>
              <p style={{ fontSize: 16 }}>O código, as sessões jogadas e o seu e-mail de acesso. Os apelidos ficam só neste navegador e nunca vão para o servidor.</p>
              <Link to="/privacidade" className="sans" style={{ fontSize: 15, fontWeight: 500 }}>Política de retenção e exclusão</Link>
            </div>
            <p className="pequeno">Material complementar, pensado para apoiar a conversa clínica. Não é instrumento diagnóstico.</p>
          </aside>
        </div>
      </div>
    </main>
  );
}
