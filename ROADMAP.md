# ROADMAP — Relatório web do Five Stages

Marque `[x]` conforme for concluindo. Cada fase tem: **objetivo**, **passos**, **prompt** pra colar no Claude Code, **pronto quando** (como saber que acabou) e **você deve saber explicar** (pra fixar o que aprendeu antes de seguir).

```
Jogo (Unity)  →  Supabase  ←  Site do psicólogo
 Fases 1, 2, 4     Fase 3        Fase 5
```

## Regras de ouro

- **Uma fase por vez.** Só passa pra próxima quando conseguir explicar o checklist "você deve saber explicar" com suas palavras.
- Comece cada tarefa no **modo de plano** do Claude Code (`Shift+Tab` até aparecer "plan mode"), leia o plano e só aprove se entender.
- **Um commit por tarefa**, na branch `feature/...`. Antes de pedir a próxima coisa, `git status` limpo.
- O Claude Code **não roda o Unity**. Quem testa é você: dá Play, e se der erro, cola o **primeiro erro vermelho** do Console.
- Tenta escrever você mesmo as partes marcadas com ✍️ antes de pedir ajuda. Peça ao Claude Code pra **revisar**, não pra substituir.
- Não mexe em cenas nem apaga assets (veja o `CLAUDE.md`).

---

## Fase 0 — Preparar o ambiente

**Objetivo:** projeto limpo, funcionando, com o Claude Code enxergando o contexto.

- [x] Clone novo com LFS: `git lfs install`, depois `git clone --branch develop https://github.com/joaoamorimBM/five-stages-game.git five-stages-novo2`
- [x] Conferir: `(Get-Item Assets\Scenes\Menu\videoMenu.mp4).Length` deve dar `1375962` e `git lfs ls-files | Select-String "^[0-9a-f]+ - "` deve voltar vazio
- [x] Abrir no Unity Hub (versão **6000.3.9f1**), esperar importar, testar menu, casa, beco e padaria
- [x] `git config --global user.name` e `user.email` configurados
- [x] Copiar `CLAUDE.md` e `ROADMAP.md` pra **raiz** do projeto e commitar: `git add CLAUDE.md ROADMAP.md`
- [x] Criar a branch: `git checkout -b feature/relatorio`
- [x] Unity: *Edit → Preferences → External Tools → External Script Editor* = Visual Studio Code, depois *Regenerate project files*
- [x] Abrir o Claude Code na raiz (`claude`) e testar com: *"Leia o CLAUDE.md e me diga, com suas palavras, qual é a meta atual e o que você não deve fazer."*

**Pronto quando:** o jogo roda inteiro e o Claude Code resume o contexto corretamente.

---

## Fase 1 — Logger local (Etapa 1)

**Objetivo:** o jogo gravar um relatório `.json` e `.txt` no disco.

**Passos**
- [x] Colocar `ReportLogger.cs` em `Assets/Scripts/` e **sobrescrever** o conteúdo de `PlayerInteraction1.cs` e `DialogueManager.cs` (sem apagar os arquivos, pra manter a ligação com as cenas)
- [ ] Dar Play na `Scene_Room`/`Scene_House`, observar alguns objetos, abrir um diálogo, trocar de cena
- [ ] Abrir `C:\Users\<você>\AppData\LocalLow\DefaultCompany\My project\Relatorios` e ler o `.txt`
- [ ] Commit: `feat: ReportLogger registra interações e diálogos`

**Prompt pro Claude Code (se der erro ou pra entender o código)**
```
Estou na Fase 1 do ROADMAP. Antes de mexer em qualquer coisa, leia
Assets/Scripts/ReportLogger.cs e me explique, passo a passo e com analogias,
como ele funciona: como se cria sozinho, o que acontece quando troco de cena,
e quando o arquivo é gravado. Depois me faça 3 perguntas pra eu verificar que entendi.
```

**Pronto quando:** o `.txt` lista cenas, objetos observados e diálogos na ordem em que você fez.

**Você deve saber explicar**
- Por que o logger não precisa estar em nenhuma cena (`RuntimeInitializeOnLoadMethod`).
- Por que ele salva com `partial: true` várias vezes antes de a sessão terminar.
- Por que a interação só é registrada se o prompt não está vazio.

---

## Fase 2 — Relatório mais rico: "observou" x "deixou de observar"

**Objetivo:** o relatório dizer **o que existia pra observar** em cada cena, pra o site mostrar o que o paciente ignorou.

**Passos**
- [x] ✍️ Ao carregar cada cena, o logger lista todos os objetos com `IInteractable` e guarda no relatório (campo novo, `availableInteractables`, por cena)
- [x] Subir `formatVersion` para **2** e documentar a mudança no `CLAUDE.md`
- [ ] Testar: o `.json` deve ter, pra cada cena visitada, a lista de interativos disponíveis
- [ ] Commit: `feat: relatório lista interativos disponíveis por cena (formato v2)`

**Prompt**
```
Fase 2 do ROADMAP. Quero que o ReportLogger registre, ao carregar cada cena, a
lista de objetos que implementam IInteractable (use o nome do GameObject).
Primeiro entre em modo de plano e me explique a abordagem e as alternativas
(por exemplo, como achar esses objetos na cena e o custo disso). Eu vou escrever
a primeira versão do método; você revisa. Atualize formatVersion para 2.
```

**Pronto quando:** com um objeto observado e vários ignorados, o JSON mostra os dois lados.

**Você deve saber explicar**
- Como o Unity acha objetos de um tipo/interface na cena e por que isso é feito uma vez por cena, e não a cada frame.
- Por que mudar o formato exige subir a versão.

---

## Fase 3 — Supabase: banco, regras de acesso e função de envio

**Objetivo:** um backend que recebe o relatório com segurança.

**Passos**
- [ ] Criar o projeto no Supabase (anotar URL e chaves; **a service role nunca vai pro jogo nem pro site**)
- [ ] Criar as tabelas `pacientes` e `sessoes` (modelo no `CLAUDE.md`), via SQL em `supabase/migrations/`
- [ ] Ligar **Row Level Security** nas duas e escrever as políticas (psicólogo só vê os próprios pacientes e suas sessões)
- [ ] Criar a Edge Function `submit-report`: valida o JSON, o `formatVersion`, o tamanho, confere o código do paciente e faz **upsert por `sessionId`**
- [ ] Testar com `curl` (código válido, código inválido, corpo gigante, mesmo `sessionId` duas vezes)
- [ ] Commit: `feat: backend Supabase (tabelas, RLS, submit-report)`

**Prompt**
```
Fase 3 do ROADMAP. Vou usar Supabase. Em modo de plano, me explique o que é
Row Level Security com uma analogia simples e proponha o SQL das tabelas
pacientes e sessoes conforme o CLAUDE.md. Não escreva a Edge Function ainda.
Depois que eu entender as políticas, vamos para a função submit-report.
Lembre: código anônimo, upsert por sessionId, nada de service role fora da função.
```

**Pronto quando:** um `curl` com código válido grava a sessão, com código inválido é recusado, e um segundo usuário **não** consegue ler as sessões do primeiro.

**Você deve saber explicar**
- O que é RLS e por que ele protege mesmo se alguém descobrir a URL da API.
- Por que o jogo envia pela Edge Function e não direto pro banco.
- Por que a escrita é upsert (e não insert).

---

## Fase 4 — Unity: código do paciente e envio

**Objetivo:** o jogo pedir o código, e enviar o relatório sem travar quem joga offline.

**Passos**
- [ ] No menu, um campo de texto pro **código do paciente** (feito por você no editor do Unity: Claude Code descreve os passos). `MainMenu.Jogar()` chama `ReportLogger.SetPatientCode(...)`; o código fica salvo em `PlayerPrefs`
- [ ] Criar `ReportUploader`: manda os relatórios da pasta `Relatorios` pra Edge Function com `UnityWebRequest` (POST JSON)
- [ ] Depois de enviar com sucesso, mover o arquivo para `Relatorios/Enviados`
- [ ] Se falhar (sem internet), o arquivo **fica** e é tentado de novo na próxima abertura do jogo e ao voltar ao menu
- [ ] URL da função e chave pública em um `ScriptableObject` de configuração (não espalhada no código)
- [ ] Commit: `feat: código do paciente no menu e envio do relatório`

**Prompt**
```
Fase 4 do ROADMAP. Quero o envio do relatório pro Supabase. Entre em modo de
plano. Me explique: o que é UnityWebRequest e por que usamos coroutine pra ele;
como a fila de reenvio deve funcionar (pasta Relatorios x Enviados); e o que
fazer se o jogador fecha o jogo no meio do envio. Os passos no editor do Unity
(campo de texto do código no menu) me descreva por escrito, que eu faço.
```

**Pronto quando:** jogando com internet, a sessão aparece no Supabase; jogando sem internet, o arquivo fica guardado e é enviado na próxima vez.

**Você deve saber explicar**
- Por que o jogo funciona offline e o que acontece com o relatório nesse caso.
- Por que o envio não pode travar a tela (coroutine).
- Por que a chave que vai no jogo é pública e por que isso é aceitável aqui.

---

## Fase 5 — Site do psicólogo

**Objetivo:** uma interface web funcional pra ver os relatórios.

**Decisão (você escolhe):** Vite + React (mais comum no mercado) ou HTML/JS puro com `supabase-js` (menos peças pra aprender). Hospedagem gratuita na Vercel ou Netlify.

**Passos**
- [ ] Login do psicólogo (Supabase Auth)
- [ ] Lista de pacientes e botão **gerar código** (8+ caracteres, único)
- [ ] Lista de sessões por paciente (data, duração, completa/incompleta)
- [ ] Página da sessão: **linha do tempo**, **tempo por cena**, tempo em diálogos e **observado x não observado** (usa o campo da Fase 2)
- [ ] Publicar e testar com dois psicólogos de teste (um não pode ver o outro)
- [ ] Commit: `feat: site do psicólogo`

**Prompt**
```
Fase 5 do ROADMAP. Vou fazer o site do psicólogo com <Vite+React | HTML puro>.
Em modo de plano, proponha a estrutura de pastas e as telas. Quero construir
uma tela por vez: primeiro só login + logout, e só depois a lista de pacientes.
A cada tela, me explique como o front-end conversa com o Supabase.
```

**Pronto quando:** você joga uma sessão, loga no site como psicólogo e vê o relatório, e um segundo usuário não vê nada.

**Você deve saber explicar**
- Como o front-end se autentica e por que ele usa a chave pública (e o RLS faz o resto).
- De onde vem cada número que aparece na tela da sessão.

---

## Fase 6 — Privacidade, testes de ponta a ponta e demonstração

- [ ] Texto do **termo de consentimento** (mostrado no jogo antes do código, ou entregue pelo psicólogo)
- [ ] Revisão LGPD: só código anônimo, HTTPS, sem dado real em testes/logs, política de retenção
- [ ] Teste completo: jogo limpo → código novo → jogar → relatório no site
- [ ] Teste offline: jogar sem internet, depois reconectar
- [ ] Teste de falha: fechar o jogo no meio (relatório `partial: true` aparece como "incompleta")
- [ ] Roteiro de demonstração de 3 minutos pra banca
- [ ] Texto do TCC: arquitetura, decisões, limitações e trabalhos futuros

**Prompt**
```
Fase 6 do ROADMAP. Revise o projeto inteiro procurando problemas de privacidade
(LGPD): dados pessoais no relatório, chaves expostas, logs com informação sensível,
políticas de acesso fracas. Liste os achados por gravidade, sem corrigir nada
ainda. Depois vamos corrigir um por um e eu explico cada correção.
```

---

## Depois (opcional)

- [x] **Escolhas nos diálogos:** opções de resposta classificadas por estágio do luto, registradas no relatório (formato v3). Não ramificam a história.
- **Otimização:** ajustar *Max Size* e compressão das texturas e usar o *Build Report* num build funcionando. Asset sem uso não entra no build, então isso rende mais do que apagar.
- **Limpeza de assets:** só com uma ferramenta que entenda a busca de materiais **por nome** dos FBX. Foi tentada e quebrou o jogo, então fica por último.
- Limpar as linhas residuais do `.gitattributes`.
- Criar um `CLAUDE.md` mais curto dentro de `supabase/` e do site, se o projeto crescer.
