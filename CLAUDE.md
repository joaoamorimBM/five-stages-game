# CLAUDE.md — Five Stages

Contexto do projeto para o Claude Code. Leia inteiro antes de qualquer tarefa. Vale em qualquer computador (PC de casa ou notebook): a memória automática do Claude **não** passa de uma máquina pra outra, então tudo o que importa está aqui.

## Como trabalhar comigo (LEIA PRIMEIRO)

- **Modo rápido (desde 28/10/2026):** implemente direto, sem aula, sem perguntas de verificação e sem "escreva você primeiro". Explique em poucas linhas o que mudou e o que testar.
- **Português do Brasil**, tom informal e direto. Comentários no código em português (padrão do projeto).
- Quando entregar código, prefira **arquivos completos e corrigidos**, não trechos soltos.
- **Você não roda o Unity.** Depois de mexer no jogo, diga exatamente o que testar no Play e peça o **primeiro erro vermelho** do Console se algo falhar. Antes disso, rode a checagem de compilação (`tools/checar-csharp.ps1`, seção "Como rodar"). Ela pega erro de C#, mas não substitui o teste no Play.
- **Decisões que são do João** (visual, texto clínico, o que guardar de dado): pergunte com opções e uma recomendação, em vez de decidir sozinho.
- **Segurança:** nunca digite senha, token, código de login nem chave de API (nem a pública) por ele. Abra o comando no terminal dele e deixe ele digitar. Nunca coloque a service role / secret key em arquivo nenhum.
- **Commit e push só quando ele pedir.** Um commit por tarefa, mensagem em português. Em PowerShell, use `git commit -F arquivo.txt` (aspas dentro de `-m @'...'@` quebram no PowerShell 5.1).

## O projeto

**Five Stages** é um jogo narrativo em Unity sobre luto. Um casal, **Noah** e **Emily**, perde a filha **Claire**. É o TCC do João Pedro (UNIFOR, Ciência da Computação), em dupla com **Luís Eduardo Barrocas**. O repositório também tem commits de João Victor Lopes. O público da apresentação são psicólogos e psiquiatras.

**Ideia central:** o jogo funciona como ferramenta **complementar** de apoio terapêutico. O psicólogo gera um código no site, o paciente joga com esse código, e o psicólogo vê um relatório organizado da sessão: como o paciente respondeu nos diálogos, pra onde olhou, quanto tempo levou. Instrumentos como o PG-13 são questionários de autorrelato aplicados fora da experiência; o jogo traz dado comportamental, durante a vivência.

**Honestidade acadêmica:** o relatório é um objetivo em desenvolvimento, não uma função validada clinicamente. Em textos, comentários e interface, use "foi pensado para…", nunca afirme eficácia clínica. Os estágios das respostas são **tendências** inspiradas em Kübler-Ross, nunca diagnóstico. A taxa de 10 a 20% de luto prolongado vem de Delalibera, Coelho & Barbosa (2011), *Acta Médica Portuguesa*, 24(6), 935-942.

## Arquitetura

```
Jogo (Unity)  ──submit-report / check-code──►  Supabase (São Paulo)  ◄──login + RLS──  Site do psicólogo
Assets/                                         supabase/                                site/
```

| Pasta | O que é |
|---|---|
| `Assets/` | O jogo (Unity 6000.3.9f1). |
| `supabase/` | Banco (migração SQL), Edge Functions `submit-report` e `check-code`, testes em Node. Deploy: `supabase/README.md`. |
| `site/` | Site do psicólogo (Vite + React + TS). Deploy: `site/README.md`. |
| `tools/` | `checar-csharp.ps1`: compila os scripts do jogo fora do Unity pra achar erros. |
| `GUIA-NOTEBOOK.md` | Passo a passo pra montar tudo num computador novo e resolver problemas. |
| `ROADMAP.md` | Fases do projeto e o que falta. |

Fluxo completo (testado em 09/11/2026 e funcionando):
1. O psicólogo entra no site (link mágico por e-mail) e clica em **Novo paciente**. O site gera um código `XXXX-XXXX`.
2. O jogo abre numa **tela de código** antes do menu. O código é conferido offline (dígito verificador) e online (`check-code`).
3. Durante a partida, o `ReportLogger` grava o relatório (formato v4) e o `ReportUploader` envia pra `submit-report` a cada troca de cena.
4. A sessão aparece no site, embaixo do código do paciente.

## Estado atual (09/11/2026)

**Git:** tudo está na branch **`feature/site-psicologo`** (já no GitHub), com PR pra `develop`. Até o merge, use essa branch. Depois do merge, volte pra `develop`.

### Jogo (`Assets/`)
- **Tela de código** (`Assets/Scripts/CodeScreen.cs`): criada por código por cima do `Scene_MainMenu`, sem mexer na cena. Tem texto de consentimento, campo do código, **Começar** e **Jogar sem código** (não grava nada). Reaparece a cada volta ao menu, pra um computador de clínica não herdar o código do paciente anterior. Título e botões usam a Bebas Neue do menu (pega do objeto `Titulo` da cena); o texto usa Alegreya Sans.
- **Código do paciente** (`PatientCode.cs`): mesma regra de `supabase/functions/_shared/codigo.ts`. **Se mudar uma, muda a outra.**
- **Relatório** (`ReportLogger.cs`, formato v4): a sessão só grava com código aceito. No editor, dar Play direto numa cena (sem passar pelo menu) grava sem código, só local. Fechar o jogo no meio grava `partial: true`. Ganchos: `ThoughtInteractable` (`LogObserveStart/End`) e `DialogueManager` (`LogDialogue`, `LogChoice`).
- **Envio** (`ReportUploader.cs`): envia a cada save e ao abrir o jogo. 200 → `Relatorios/Enviados`; 4xx → `Relatorios/Recusados`; sem internet → fica e tenta depois. Relatório sem código (teste no editor) nunca é enviado.
- **Configuração** (`GameConfig.cs` lê `Assets/Resources/five_stages_config.json`): `functionsUrl` = `https://stnzcrwdzwctpjnwamdo.supabase.co/functions/v1`. Vazio = jogo 100% offline. Não tem chave nenhuma (as funções do jogo não pedem login).
- **Opções nos diálogos:** uma fala pode ter `choices` (`text`, `stage`, `response` opcional) e `choiceSpeaker`. As opções aparecem **dentro** da caixa de diálogo, embaixo da fala (fonte 14, teclas 1 a 4 ou clique, cursor liberado nessa hora). A fala sobe pra abrir espaço e a caixa só cresce pra cima se não couber. A opção escolhida vira fala de `choiceSpeaker`, depois vem a `response`, e a história segue igual (não ramifica). Diálogos com opções: `remedios`, `louca`, `Pensamento_Quadro_familia`, `Celular`, `TV`, `torradas`, `dialogoAntesFlashBack`, `dialogoPosFlashback`, `ConversaPadaria`, `DialogoGraceFinal`.
- **Caixa de diálogo, visual "memória íntima"** (`Beco_Assets/Scripts/DialogueSkin.cs`, montada por código em cima do prefab `DialogueCanvas`):
  - painel carvão com cantos arredondados e borda creme (sprite 9-slice gerado em `UiShapes.cs`);
  - retrato de busto de quem fala num quadro à esquerda (`PortraitCrop.cs` recorta os PNGs na hora, sem alterar os arquivos; **retrato novo precisa de uma linha lá**, senão aparece inteiro);
  - nome em versalete e fala em **Alegreya Sans** (`Assets/Resources/Fonts/`, licença OFL junto, carregada como fonte dinâmica em `GameFonts.cs`);
  - os retratos grandes e as etiquetas de nome antigas ficam escondidos. `useNewLook` no DialogueManager volta ao visual antigo.
  - O prefab `DialogueCanvas` foi corrigido: escalas em 1 (antes a caixa era achatada em 0,77), fala preenchendo a caixa com tamanho automático 15 a 22. Canvas de referência 800×600, match na largura.
- **Temas dos objetos da casa** (campo `theme` do `DialogueData`): "Memórias da Claire" (foto, carta, TV, coruja), "Autocuidado" (remédios, louça, torradeira, geladeira), "Relação com a Emily" (Emily dormindo), "Rotina" (celular).
- `NPCInteraction` (padaria) não é `ThoughtInteractable`: o diálogo dele entra no relatório, mas não conta na exploração.

### Back-end (`supabase/`)
- Projeto **Five Stages**, ref `stnzcrwdzwctpjnwamdo`, região **São Paulo** (sa-east-1), na conta do João.
- Migração aplicada e funções `submit-report` e `check-code` no ar, testadas com curl.
- Comandos (precisam de `npx supabase login` antes, feito pelo João no terminal dele):
  - `npx supabase link --project-ref stnzcrwdzwctpjnwamdo`
  - `npx supabase db push --remote --yes`
  - `npx supabase functions deploy submit-report check-code --remote --use-api --yes`
- Testes das regras (sem rede): `node supabase/functions/_shared/codigo.test.ts` e `node supabase/functions/_shared/relatorio.test.ts`.

### Site (`site/`)
- Vite + React + TS, `react-router-dom`, `supabase-js`. Visual "caderno de sessão" (papel creme, Alegreya / Alegreya Sans / Courier Prime). Mockup: https://claude.ai/artifact/N5bbCW2zPhDPTyBVyiDV7x
- Telas: Login (link mágico), Pacientes (gerar código, apelido **só no navegador**, sessões, apagar), Sessão (resumo por regras fixas, como respondeu com as opções não escolhidas, "hesitou" relativo à mediana do próprio paciente, para onde olhou por tema, linha do tempo) e Privacidade.
- Lê os formatos 3 e 4 (`src/lib/relatorio.ts`; tipo do v4 em `src/lib/tipos.ts`).
- Sem `site/.env.local`, roda em **modo demonstração** com dados de exemplo (bom pra mostrar à banca sem internet).
- **Ainda não está publicado na Vercel.** Roda local em http://localhost:5173.
- No Supabase (Authentication → URL Configuration): Site URL `http://localhost:5173` e Redirect URL `http://localhost:5173/**`. Quando publicar, adicionar a URL da Vercel nos dois.

## Como rodar

**Jogo:** abrir no Unity Hub com a versão **6000.3.9f1**. Play no `Scene_MainMenu`. Use a aba Game em **Full HD (1920x1080)** pra conferir a interface.

**Checar o C# sem abrir o Unity** (usa o compilador e as DLLs do próprio Unity; precisa ter aberto o projeto no Unity pelo menos uma vez, pra existir o `Assembly-CSharp.csproj` e a pasta `Library/`):
```powershell
powershell -ExecutionPolicy Bypass -File tools/checar-csharp.ps1
```
Saída `0` = sem erros. Scripts novos são incluídos sozinhos (o script lê todos os `.cs` de `Assets/`, fora pastas `Editor`).

**Site:**
```powershell
npm install --prefix site
npm run dev --prefix site
```
O Claude Code também tem o preview "site" em `.claude/launch.json`.

**Supabase:** ver `supabase/README.md`. O CLI roda via `npx supabase` (não precisa instalar). Docker não está instalado, então `db push` sempre com `--remote`.

## Arquivos locais (não vão pro git)

| Arquivo | Pra que serve | Como recriar |
|---|---|---|
| `site/.env.local` | URL e chave **pública** do Supabase pro site | Copiar `site/.env.example` e colar a **Publishable key** (Painel → Project Settings → API Keys). O João cola, não o Claude. |
| `Relatorios/` | Relatórios gerados no editor (dado sensível) | O jogo cria sozinho. Nunca commitar. |
| `supabase/.temp/` | Estado do `supabase link` | Rodar `npx supabase link` de novo. |
| Login do Supabase CLI | Token no computador | `npx supabase login` no terminal do João. |

## Formato do relatório (v4)

Já vem organizado como o site mostra. Tipo TypeScript equivalente: `site/src/lib/tipos.ts` (`RelatorioV4`).

```json
{
  "formatVersion": 4,
  "patientCode": "K7M2-9QXP",
  "sessionId": "uuid",
  "gameVersion": "0.1",
  "startedAt": "2026-11-09T18:00:00.0000000Z",
  "endedAt": "2026-11-09T18:14:02.0000000Z",
  "durationSeconds": 842.0,
  "partial": false,
  "scenes": [ { "scene": "Scene_House", "label": "Casa", "t": 20.0, "seconds": 170.0 } ],
  "observables": [ { "scene": "Scene_House", "id": "remedios", "label": "Remédios", "theme": "Autocuidado", "order": 2, "t": 38.0, "seconds": 9.0 } ],
  "choices": [ { "t": 41.0, "scene": "Scene_House", "dialogue": "Remédios", "speaker": "Noah", "prompt": "O médico receitou. Não estou tomando.",
                 "options": [ { "text": "Não adianta.", "stage": "Depressao" }, { "text": "Eu não preciso disso. Eu tô bem.", "stage": "Negacao" } ],
                 "chosen": 1, "seconds": 2.9 } ],
  "dialogues": [ { "t": 38.0, "scene": "Scene_House", "label": "Remédios", "seconds": 9.0, "lines": 2, "typingSkips": 1 } ]
}
```

- `observables`: um por **diálogo** de `ThoughtInteractable` (objetos com o mesmo diálogo contam como um), listados ao carregar a cena, inclusive inativos. `order` = ordem em que observou (0 = não observou); `seconds` = tempo no pensamento. `theme` vem do `DialogueData.theme` (vazio = "Outros").
- `choices`: guarda **todas as opções mostradas** e o índice `chosen`. `seconds` = tempo pra escolher.
- `t` = segundos desde o início da sessão. O `t` de `dialogues` é o **início** do diálogo.
- `stage` é o nome do enum `DialogueData.GriefStage` (`Negacao`, `Raiva`, `Barganha`, `Depressao`, `Aceitacao`).
- `sessionId` é **estável durante a sessão**: o envio é **upsert por `sessionId`**, nunca insert duplicado.
- Mudou o formato? Suba `formatVersion` e mantenha o site e a `submit-report` aceitando os antigos. Histórico: v1 só `events`; v2 `availableInteractables`; v3 eventos `choice` (ainda aceito); v4 listas organizadas.

## Modelo de dados (`supabase/migrations/`)

- `pacientes`: `id`, `psicologo_id` (→ `auth.users`, default `auth.uid()`), `codigo` (único, `XXXX-XXXX`), `criado_em`.
- `sessoes`: `id` (= `sessionId`), `paciente_id`, `iniciada_em`, `duracao_segundos`, `parcial`, `formato`, `relatorio` (jsonb inteiro), `resumo` (jsonb `{escolhas, estagios}`, calculado pela função), `recebida_em`, `atualizada_em`. View `sessoes_resumo` (sem o `relatorio`) pra lista do site.
- **RLS:** o psicólogo lê e apaga só o que é dele, e cria pacientes só pra si. Ninguém altera código nem sessão pelo site. Só a `submit-report` (service role) escreve em `sessoes`, depois de validar código, tamanho (512 KB) e `formatVersion` (3 ou 4). Uma versão parcial atrasada não sobrescreve uma sessão completa.

## Privacidade (LGPD)

Os dados são sobre saúde emocional: **dado pessoal sensível**. Sempre:
- código anônimo, **nunca** nome, e-mail ou identificador do paciente no relatório ou no banco (os dados são *pseudonimizados*: o psicólogo sabe quem é, então a LGPD continua valendo);
- o apelido do paciente fica só no navegador do psicólogo (localStorage);
- HTTPS em tudo; nenhuma service role no jogo nem no site;
- termo de consentimento antes de coletar (na tela de código);
- dados no Brasil (Supabase em São Paulo);
- nada de dado real de paciente em testes, commits ou logs; as funções nunca logam o conteúdo do relatório.

## Stack e código do jogo

- **Unity 6000.3.9f1**, URP 17.3, Input System 1.18 com `activeInputHandler: 2` (`PlayerInteraction` usa `Input.GetKeyDown`; `DialogueManager` e `CodeScreen` usam o Input System), TextMeshPro (ugui 2.x: use `textWrappingMode`, não `enableWordWrapping`), Cinemachine, ProBuilder.
- Cenas no build: `Menu/Scene_MainMenu`, `Scene_Intro`, `Scene_Room`, `Scene_House`, `Scene_Alley`, `padaria_scene`, `Acidente_Scene`, `Dedicatoria_Scene`.
- Scripts em `Assets/Scripts/` (`Fase_0`, `Fase_02`, `Fase_1`, `GameManager.cs`, `MainMenu.cs`, `DedicationScreen.cs`, e os do relatório) e `Assets/Beco_Assets/Scripts/` (`DialogueManager`, `DialogueData`, `DialogueSkin`, `DialogueTrigger`, `CharacterDataBase`, `BecoSequenceManager`...). Sem asmdef (tudo em `Assembly-CSharp`).
- **Diálogos:** `DialogueData` (ScriptableObject) tem falas (`speaker` + `sentence` + `choices`), `reportLabel` e `theme`. Assets em `Assets/sistemaDeDialogo/`, `Beco_Assets/Diálogos`, `Scripts/Fase_1`, `Scripts/Fase_02` e `Scenes/padaria_scene`. O `DialogueManager` é singleton (`DontDestroyOnLoad`) e expõe `StartDialogue`, `PlayDialogueAuto`, `isDialogueActive`, `isAwaitingChoice` e `OnDialogueEnded`. O `CharacterDatabase` liga cada personagem ao nome e ao retrato.
- Os enums `CharacterType` e `GriefStage` são salvos como número nos assets: **só adicione valores no final**.
- **Interação:** `IInteractable` (`Interact()`, `GetPromptText()`) em `ThoughtInteractable`, `DoorController`, `LightSwitch` e `SceneTransition`. `PlayerInteraction` (`PlayerInteraction1.cs`) faz o raycast e chama `Interact()` no E.
- `GameManager` e `ReportLogger` são singletons criados por `[RuntimeInitializeOnLoadMethod]`.
- **Fim de sessão:** voltar pro `Scene_MainMenu` encerra a sessão no logger.

## Git e LFS

- Repositório: `https://github.com/joaoamorimBM/five-stages-game`. Branch principal: **`develop`** (a `main` está desatualizada). Trabalho novo em `feature/<nome>`, partindo da `develop`.
- **Git LFS** guarda `.png`, `.jpg`, `.fbx`, `.wav`, `.mp3` e `.mp4`. Antes de clonar: `git lfs install`. Se um `.mp4`/`.fbx`/`.png` tiver ~130 bytes, é ponteiro (Unity dá "Error while reading movie" ou textura rosa). Conserto: `git lfs pull`.
- O GitHub CLI (`gh`) não estava instalado no PC de casa. Pra abrir PR, use o link que o `git push` mostra.
- Avisos "LF will be replaced by CRLF" são inofensivos.
- O Unity regrava arquivos sozinho ao abrir: `ProjectSettings/Packages/*/Settings.json` e fontes TMP como `Assets/Scenes/Menu/BebasNeue-Regular SDF.asset`. **Não misture com commits de funcionalidade.** `Assets/_Recovery/` (backup de cena de quando o Unity fecha mal) também fica fora.

## O que NÃO fazer

- **Não edite cenas (`.unity`) nem prefabs como texto.** Mudanças em cena são feitas no editor pelo João; descreva os passos. **Exceções liberadas pelo João:** o prefab `Beco_Assets/Prefabs/UI/DialogueCanvas.prefab`, os textos do `Scene_MainMenu` e os `.asset` de diálogo (inclusive os de `Assets/Scenes/padaria_scene`).
- Prefira resolver interface **por código** (como `CodeScreen` e `DialogueSkin`) em vez de editar cena.
- **Não apague assets.** Uma limpeza automática por GUID já quebrou o jogo: FBX procuram materiais e texturas **pelo nome**. Asset sem uso não entra no build. Limpeza adiada de propósito.
- Não toque em `Assets/Settings/`, `TextMesh Pro` nem pacotes de terceiros.
- Não commite `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Relatorios/`, `site/.env.local`, `supabase/.temp/`, nem chaves e segredos.

## Ferramentas úteis no Claude Code

- **Plugin oficial da Unity** (instalado no PC de casa; no notebook, instalar de novo): skills `unity:ui-ugui` (Canvas, prefabs de UI), `unity:optimize-text-mesh-pro`, `unity:unity-cli` (controla o editor aberto), `unity:craft-game-floors` (acabamento).
- Skills de design (`design:accessibility-review`, `design:ux-copy`) pro site e pros textos.
- `/code-review` e `/security-review` antes de PR grande.

## Próximos passos

Ver `ROADMAP.md`. Resumo do que falta:
1. Merge do PR `feature/site-psicologo` → `develop`.
2. Publicar o site na Vercel (Root Directory `site`, as duas variáveis do `.env.local`) e adicionar a URL no Supabase (Site URL e Redirect URLs).
3. Testar com **dois psicólogos** (um não pode ver os pacientes do outro) e o fluxo offline (jogar sem internet, reconectar).
4. Revisar com um psicólogo o texto de consentimento, os textos das opções e os temas.
5. Definir o prazo de retenção dos dados (hoje: o psicólogo apaga quando quiser) e atualizar a página de Privacidade.
6. Pro Luís testar o site: ele cria o próprio `site/.env.local`.
7. Roteiro de demonstração de 3 minutos pra banca e o texto do TCC (arquitetura, decisões, limitações).
