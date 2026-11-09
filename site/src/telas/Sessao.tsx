import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { IconeVoltar } from "../componentes/Icones";
import { useApelidos } from "../lib/apelidos";
import { api } from "../lib/dados";
import { fmtDataHora, fmtDataLonga, fmtDuracao, fmtRelogio, fmtSegundos } from "../lib/formato";
import { contarEstagios, explorar, FATOR_HESITACAO, frasesResumo, lerRelatorio, ritmoLeitura } from "../lib/relatorio";
import { ESTAGIOS, type Paciente, type Sessao } from "../lib/tipos";

export default function SessaoTela() {
  const { id = "" } = useParams();
  const navegar = useNavigate();
  const { apelido } = useApelidos();
  const [sessao, setSessao] = useState<Sessao | null | undefined>(undefined);
  const [paciente, setPaciente] = useState<Paciente | null>(null);
  const [numero, setNumero] = useState(0);
  const [apagando, setApagando] = useState(false);
  const [erro, setErro] = useState("");

  useEffect(() => {
    let vivo = true;
    (async () => {
      try {
        const [s, pacientes, todas] = await Promise.all([api.obterSessao(id), api.listarPacientes(), api.listarSessoes()]);
        if (!vivo) return;
        setSessao(s);
        if (s) {
          setPaciente(pacientes.find((p) => p.id === s.paciente_id) ?? null);
          const doPaciente = todas.filter((x) => x.paciente_id === s.paciente_id).sort((a, b) => a.iniciada_em.localeCompare(b.iniciada_em));
          setNumero(doPaciente.findIndex((x) => x.id === s.id) + 1);
        }
      } catch {
        if (vivo) setErro("Não foi possível abrir a sessão. Confira a conexão e recarregue a página.");
      }
    })();
    return () => {
      vivo = false;
    };
  }, [id]);

  const leitura = useMemo(() => (sessao ? lerRelatorio(sessao.relatorio) : null), [sessao]);

  if (erro) return <main className="pagina"><div className="pagina-dentro"><p className="erro" role="alert">{erro}</p></div></main>;
  if (sessao === undefined) return <main className="pagina"><div className="pagina-dentro"><p className="carregando">Abrindo a sessão…</p></div></main>;
  if (sessao === null || !leitura) {
    return (
      <main className="pagina">
        <div className="pagina-dentro">
          <Link to="/" className="voltar"><IconeVoltar /> Pacientes</Link>
          <p>Essa sessão não existe ou não é de um paciente seu.</p>
        </div>
      </main>
    );
  }

  const contagem = contarEstagios(leitura);
  const total = leitura.escolhas.length;
  const exploracoes = explorar(leitura);
  const ritmo = ritmoLeitura(leitura);
  const observados = leitura.observaveis.filter((o) => o.ordem > 0).length;
  const duracao = leitura.duracao || sessao.duracao_segundos;
  const nome = paciente ? apelido(paciente.codigo) : "";

  const registro = [
    ...leitura.cenas.map((c) => ({ t: c.inicio, texto: "Entrou em " + c.nome })),
    ...leitura.observaveis.filter((o) => o.ordem > 0).map((o) => ({ t: o.t, texto: `Observou ${o.nome} (${fmtSegundos(o.segundos)})` })),
    ...leitura.escolhas.map((e) => ({ t: e.t, texto: `Escolheu “${e.opcoes[e.escolhida]?.texto ?? "?"}” · ${e.opcoes[e.escolhida]?.estagio?.nome ?? "sem estágio"}` })),
  ].sort((a, b) => a.t - b.t);

  async function apagar() {
    try {
      await api.apagarSessao(sessao!.id);
      navegar("/");
    } catch {
      setErro("Não foi possível apagar a sessão. Tente de novo.");
    }
  }

  return (
    <main className="pagina">
      <div className="pagina-dentro">
        {/* Cabeçalho */}
        <div style={{ display: "flex", flexDirection: "column", gap: 18 }}>
          <Link to="/" className="voltar"><IconeVoltar /> Pacientes</Link>
          <div style={{ display: "flex", flexWrap: "wrap", alignItems: "baseline", gap: "8px 16px" }}>
            {paciente && <span className="codigo" style={{ fontSize: 17 }}>{paciente.codigo}</span>}
            {nome && <span className="italico" style={{ fontSize: 19 }}>{nome}</span>}
            {numero > 0 && <span className="pequeno">{numero}ª sessão</span>}
          </div>
          <h1 className="titulo">Sessão de {fmtDataLonga(sessao.iniciada_em)}</h1>
          <div className="meta">
            <span><strong>{fmtDuracao(duracao)}</strong> de jogo</span>
            {leitura.parcial
              ? <span><strong style={{ color: "var(--alerta)" }}>Incompleta</strong>, o jogo foi fechado antes do fim</span>
              : <span><strong style={{ color: "var(--ok)" }}>Completa</strong>, do início à dedicatória</span>}
            <span>Jogada em {fmtDataHora(sessao.iniciada_em)}</span>
            <span>{total} {total === 1 ? "escolha" : "escolhas"} · {observados} {observados === 1 ? "objeto observado" : "objetos observados"}</span>
          </div>
        </div>

        {leitura.avisos.map((a) => <p key={a} className="aviso">{a}</p>)}

        {/* 1. Resumo */}
        <section className="linha">
          <div className="principal resumo">
            <h2 className="rotulo">Resumo</h2>
            {frasesResumo(leitura).map((f) => <p key={f}>{f}</p>)}
          </div>
          <aside className="margem">
            <p className="nota">Frases montadas por regras fixas a partir dos dados abaixo. Nenhuma IA interpreta a sessão.</p>
          </aside>
        </section>

        {/* 2. Como respondeu */}
        <section style={{ display: "flex", flexDirection: "column", gap: 28 }}>
          <div className="cabeca-secao">
            <h2 className="secao-titulo">Como respondeu</h2>
            {total > 0 && <span className="pequeno">{total} {total === 1 ? "escolha" : "escolhas"} · tempo típico para decidir: {fmtSegundos(leitura.tempoTipico)}</span>}
          </div>

          <div className="estagios">
            {ESTAGIOS.map((e) => (
              <div key={e.id} className="estagio">
                <span className="estagio-n" style={{ color: contagem[e.id] ? e.tinta : "#8c8273" }}>{contagem[e.id]}</span>
                <span className="estagio-nome">{e.nome}</span>
                <span className="trilho" aria-hidden="true">
                  <span style={{ width: total ? (contagem[e.id] / total) * 100 + "%" : 0, background: e.cor }} />
                </span>
              </div>
            ))}
          </div>

          <div className="linha">
            {total === 0 ? (
              <p className="vazio principal">Nenhuma escolha registrada nesta sessão.</p>
            ) : (
              <ol className="escolhas principal" style={{ gap: 0 }}>
                {leitura.escolhas.map((c, i) => (
                  <li key={i} className="escolha">
                    <div className="escolha-topo">
                      <span className="rotulo">{fmtRelogio(c.t)} · {c.cena} · {c.dialogo}</span>
                      <span className="escolha-tempo">
                        pensou {fmtSegundos(c.segundos)}
                        {c.hesitou && <span className="hesitou">hesitou</span>}
                      </span>
                    </div>
                    {c.fala && (
                      <p className="fala">
                        {c.quem && <span className="quem">{c.quem}</span>}
                        <span className="italico">“{c.fala}”</span>
                      </p>
                    )}
                    <ul className="opcoes">
                      {c.opcoes.map((o, j) =>
                        j === c.escolhida ? (
                          <li key={j} className="opcao opcao-escolhida">
                            <svg width="14" height="14" viewBox="0 0 14 14" role="img" aria-label="escolhida"><circle cx="7" cy="7" r="6" fill="#1E1A16" /></svg>
                            <span className="texto">{o.texto}</span>
                            {o.estagio && <span className="etiqueta" style={{ background: o.estagio.cor }}>{o.estagio.nome}</span>}
                          </li>
                        ) : (
                          <li key={j} className="opcao opcao-outra">
                            <svg width="14" height="14" viewBox="0 0 14 14" role="img" aria-label="não escolhida"><circle cx="7" cy="7" r="5.5" fill="none" stroke="#A89F90" strokeWidth="1.2" /></svg>
                            <span className="texto">{o.texto}</span>
                            {o.estagio && <span className="etiqueta-fraca">{o.estagio.nome}</span>}
                          </li>
                        ),
                      )}
                    </ul>
                  </li>
                ))}
              </ol>
            )}
            <aside className="margem">
              <p className="nota">As opções em cinza são as que apareceram e não foram escolhidas.</p>
              <p className="nota">
                “Hesitou” marca as escolhas que levaram mais de {String(FATOR_HESITACAO).replace(".", ",")} vez o tempo típico dele nesta sessão, e não um tempo fixo igual para todos.
              </p>
              <p className="nota">Os estágios são tendências de resposta inspiradas em Kübler-Ross. Não indicam em que fase o paciente está.</p>
            </aside>
          </div>
        </section>

        {/* 3. Para onde olhou */}
        <section style={{ display: "flex", flexDirection: "column", gap: 28 }}>
          <h2 className="secao-titulo">Para onde olhou</h2>
          {exploracoes.length === 0 && <p className="vazio">Esta sessão não passou por cenas com objetos para observar.</p>}
          {exploracoes.map((ex) => {
            const foraAutocuidado = ex.temas.find((t) => t.nome === "Autocuidado");
            return (
              <div key={ex.cena} style={{ display: "flex", flexDirection: "column", gap: 20 }}>
                <div className="cabeca-secao">
                  <h3 style={{ fontSize: 24, fontWeight: 600 }}>{ex.cena}</h3>
                  <span className="pequeno">observou {ex.vistos} de {ex.total} objetos · {ex.nivel}</span>
                </div>
                <div className="barra" aria-hidden="true"><div style={{ width: (ex.vistos / ex.total) * 100 + "%" }} /></div>
                <div className="linha">
                  <div className="principal temas" style={{ display: "grid" }}>
                    {ex.temas.map((t) => (
                      <div key={t.nome} className="cartao tema">
                        <div className="tema-topo">
                          <h3>{t.nome}</h3>
                          <span className="pequeno" style={{ whiteSpace: "nowrap" }}>{t.vistos} de {t.total}</span>
                        </div>
                        <ul className="objetos">
                          {t.itens.map((o) =>
                            o.ordem > 0 ? (
                              <li key={o.nome} className="objeto">
                                <span className="ordem">{o.ordem}º</span>
                                <span className="nome">{o.nome}</span>
                                <span className="pequeno" style={{ fontSize: 14 }}>{o.segundos ? fmtSegundos(o.segundos) : ""}</span>
                              </li>
                            ) : (
                              <li key={o.nome} className="objeto objeto-fora">
                                <span className="ordem" aria-hidden="true">—</span>
                                <span className="nome">{o.nome}</span>
                                <span className="pequeno" style={{ fontSize: 14 }}>não observou</span>
                              </li>
                            ),
                          )}
                        </ul>
                      </div>
                    ))}
                  </div>
                  <aside className="margem">
                    {foraAutocuidado && foraAutocuidado.total - foraAutocuidado.vistos > 0 && (
                      <p className="nota">
                        Não se aproximou de {foraAutocuidado.total - foraAutocuidado.vistos} dos {foraAutocuidado.total} objetos ligados ao autocuidado.
                      </p>
                    )}
                    <p className="nota">O número antes do objeto é a ordem em que ele foi observado. O tempo é quanto durou o pensamento do Noah.</p>
                  </aside>
                </div>
              </div>
            );
          })}
        </section>

        {/* 4. Linha do tempo */}
        <section style={{ display: "flex", flexDirection: "column", gap: 20 }}>
          <h2 className="secao-titulo">Linha do tempo</h2>
          {duracao > 0 && leitura.cenas.length > 0 && (
            <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
              <div className="tempo-faixa">
                <div className="tempo-cenas">
                  {leitura.cenas.map((c, i) => {
                    const largura = (c.duracao / duracao) * 100;
                    return (
                      <div key={i} className="tempo-cena" style={{ width: largura + "%" }} title={`${c.nome}: ${fmtDuracao(c.duracao)}`}>
                        {largura > 7 ? c.nome : ""}
                      </div>
                    );
                  })}
                </div>
                {leitura.escolhas.map((c, i) => {
                  const cor = c.opcoes[c.escolhida]?.estagio?.cor ?? "#5a5248";
                  return (
                    <span
                      key={i}
                      className="ponto"
                      aria-hidden="true"
                      style={{ left: Math.min(100, (c.t / duracao) * 100) + "%", background: cor, boxShadow: c.hesitou ? `0 0 0 3px #F5EFE3, 0 0 0 4.5px ${cor}` : "none" }}
                    />
                  );
                })}
              </div>
              <div className="tempo-marcas">
                <span>00:00</span>
                <span>{fmtRelogio(duracao / 2)}</span>
                <span>{fmtRelogio(duracao)}</span>
              </div>
              <p className="pequeno">Cada ponto é uma escolha, na cor do estágio. Os pontos com anel são as hesitações.</p>
            </div>
          )}

          <details className="registro">
            <summary>Ver registro completo</summary>
            <div className="registro-linhas">
              {ritmo.dialogos > 0 && (
                <p className="apagado" style={{ marginBottom: 8 }}>
                  Leitura: {ritmo.dialogos} diálogos, {ritmo.falas} falas. Pulou a digitação em {Math.round(ritmo.fracao * 100)}% das falas.
                </p>
              )}
              {registro.map((l, i) => (
                <div key={i}><span className="t">{fmtRelogio(l.t)}</span> {l.texto}</div>
              ))}
            </div>
          </details>
        </section>

        <footer className="rodape">
          <p className="italico apagado" style={{ fontSize: 16, maxWidth: 640 }}>
            Material complementar, pensado para apoiar a conversa clínica. Não é instrumento diagnóstico nem foi validado clinicamente.
          </p>
          {apagando ? (
            <div className="confirmar" role="alert">
              <span>Apagar esta sessão? Não dá pra desfazer.</span>
              <button type="button" className="botao-linha botao-perigo" onClick={apagar}>Apagar</button>
              <button type="button" className="botao-texto" onClick={() => setApagando(false)}>Cancelar</button>
            </div>
          ) : (
            <button type="button" className="botao-linha botao-perigo" onClick={() => setApagando(true)}>Excluir esta sessão</button>
          )}
        </footer>
      </div>
    </main>
  );
}
