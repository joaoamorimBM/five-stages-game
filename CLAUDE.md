# CLAUDE.md — Five Stages

Contexto do projeto para o Claude Code. Leia inteiro antes de qualquer tarefa.

## Como trabalhar comigo (LEIA PRIMEIRO)

> **Modo rápido (desde 28/10/2026):** o João pediu para o Claude implementar direto neste projeto, sem o passo a passo didático abaixo. Os itens 1 a 3 estão suspensos; os itens 4 a 6 e o resto do arquivo continuam valendo.

O João Pedro quer **aprender**, não só receber código pronto. O objetivo dele é fortalecer a base de programação e depender menos de IA. Então:

1. **Antes de mexer em qualquer arquivo**, explique em linguagem simples o que vai fazer e por quê. Use analogias. Na dúvida, assuma que ele **não conhece** o conceito.
2. **Construa aos poucos.** Uma tarefa pequena por vez. Só avance quando ficar claro que ele entendeu: faça uma pergunta curta de verificação ("como você explicaria X?") ou peça para ele resumir antes do próximo passo.
3. **Deixe ele escrever as partes-chave.** Em tarefas didáticas, descreva o que a função deve fazer, deixe ele tentar, e então revise o código dele em vez de substituir.
4. Quando entregar código, prefira **arquivos completos e corrigidos**, não trechos soltos.
5. Idioma: **português do Brasil**, tom informal e direto. Comentários no código em português (é o padrão do projeto).
6. **Você não consegue rodar o Unity.** Depois de cada mudança, diga exatamente o que ele deve testar no Play e peça para colar o primeiro erro vermelho do Console se algo falhar. Nunca afirme que "compila" sem ele ter testado.

## O projeto

**Five Stages** é um jogo narrativo em Unity sobre luto. Um casal, **Noah** e **Emily**, perde a filha **Claire**. É o TCC do João Pedro (UNIFOR, Ciência da Computação), em dupla com **Luís Eduardo Barrocas**; o repositório também tem commits de João Victor Lopes. O público-alvo da apresentação são psicólogos e psiquiatras.

**Ideia central:** o jogo funciona como ferramenta **complementar** de apoio terapêutico. Ao final da sessão, ele gera um **relatório das ações do paciente** para o psicólogo. Instrumentos tradicionais como o PG-13 são questionários de autorrelato aplicados fora da experiência; o jogo traz dado comportamental, durante a vivência, para um público jovem e familiarizado com videogame.

**Honestidade acadêmica:** o relatório é um objetivo em desenvolvimento, não uma função validada clinicamente. Em textos, comentários e interface, use "foi pensado para…", nunca afirme eficácia clínica. A taxa de 10 a 20% de luto prolongado vem de Delalibera, Coelho & Barbosa (2011), *Acta Médica Portuguesa*, 24(6), 935-942.

## Meta atual: relatório web para o psicólogo

```
Jogo (Unity)  →  Supabase (banco + login + Edge Function)  ←  Site do psicólogo
```

- O jogo **grava o relatório localmente** e só depois tenta enviar. Ele precisa funcionar **sem internet**.
- O psicólogo cria o paciente no site, e o site gera um **código anônimo** `XXXX-XXXX` (Crockford base32, último caractere é dígito verificador; regra em `supabase/functions/_shared/codigo.ts`, que precisa ser igual no C# e no site). Nenhum nome ou dado pessoal. Um código por paciente; as sessões dele ficam agrupadas.
- O jogo abre numa **tela de código** antes do menu (com o texto de consentimento e a opção "Jogar sem código", que não grava nada). Ela aparece de novo a cada volta ao menu, pra um computador de clínica não herdar o código do paciente anterior. Offline, o dígito verificador pega erro de digitação; online, o jogo confirma pela função `check-code`.
- O apelido do paciente ("paciente das terças") fica **só no navegador do psicólogo** (localStorage), nunca no servidor.
- O envio passa pela Edge Function `submit-report`, que valida o código. O jogo **não carrega chave nenhuma** (as funções têm `verify_jwt = false` e se protegem validando o corpo).
- Visual do site: "caderno de sessão" (papel creme, Alegreya / Alegreya Sans / Courier Prime). Mockup: https://claude.ai/artifact/N5bbCW2zPhDPTyBVyiDV7x
- Cada psicólogo só pode ver os próprios pacientes (Row Level Security no banco).
- O site mostra: lista de pacientes, gerar código, lista de sessões e a página da sessão (linha do tempo, tempo por cena, **o que o paciente observou e o que deixou de observar**).

### Formato do relatório (v4, implementado no `ReportLogger`)

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
- `choices`: guarda **todas as opções mostradas** e o índice `chosen`, porque o que ele não escolheu também importa. `seconds` = tempo pra escolher.
- `t` = segundos desde o início da sessão. O `t` de `dialogues` é o **início** do diálogo.

Histórico:
- **v1:** só `events`. **v2:** `availableInteractables`. **v3:** eventos `choice` com `choice`/`stage` (formato antigo baseado em eventos; o site e a `submit-report` ainda aceitam).
- **v4:** troca a lista de eventos por listas organizadas (`scenes`, `observables`, `choices`, `dialogues`), com tema, rótulo legível e todas as opções.

Regras do formato:
- `stage` (nas opções) é o nome do enum `DialogueData.GriefStage` (`Negacao`, `Raiva`, `Barganha`, `Depressao`, `Aceitacao`). É **tendência de resposta** inspirada em Kübler-Ross, nunca diagnóstico.
- `sessionId` é **estável durante a sessão**. O logger salva várias vezes (a cada troca de cena e ao fechar), com `partial: true` até a sessão terminar. Portanto o envio deve ser **upsert por `sessionId`**, nunca insert duplicado.
- Mudanças no formato exigem subir `formatVersion` e manter o site compatível com versões antigas.

### Modelo de dados (implementado em `supabase/migrations/`)

- `pacientes`: `id`, `psicologo_id` (→ `auth.users`, default `auth.uid()`), `codigo` (único, `XXXX-XXXX`), `criado_em`.
- `sessoes`: `id` (= `sessionId` do jogo), `paciente_id`, `iniciada_em`, `duracao_segundos`, `parcial`, `formato`, `relatorio` (jsonb, o JSON do jogo inteiro), `resumo` (jsonb `{escolhas, estagios}`, calculado pela função), `recebida_em`, `atualizada_em`. View `sessoes_resumo` (sem o `relatorio`) pra lista do site.
- RLS: o psicólogo lê e apaga só o que é dele, e cria pacientes só pra si. Ninguém altera código nem sessão pelo site. A escrita em `sessoes` vem **somente** da `submit-report` (service role), depois de validar o código, o tamanho (512 KB) e o `formatVersion` (3 ou 4). Uma versão parcial atrasada não sobrescreve uma sessão completa.
- Projeto do Supabase na região São Paulo. Passo a passo de deploy: `supabase/README.md`.

## Privacidade (LGPD)

Os dados são sobre saúde emocional, ou seja, **dado pessoal sensível**. Sempre:
- código anônimo, **nunca** nome, e-mail ou qualquer identificador do paciente no relatório;
- HTTPS em tudo; nenhuma chave secreta (service role) no jogo nem no front-end;
- o psicólogo só acessa os próprios pacientes;
- termo de consentimento antes de coletar;
- nada de dados reais de paciente em testes, commits ou logs.

## Stack e estrutura do código

- **Unity 6000.3.9f1**, URP 17.3, Input System 1.18 com `activeInputHandler: 2` (usa os dois: `PlayerInteraction` usa `Input.GetKeyDown`, o `DialogueManager` usa o Input System), TextMeshPro, Cinemachine, ProBuilder.
- Cenas no build: `Menu/Scene_MainMenu`, `Scene_Intro`, `Scene_Room`, `Scene_House`, `Scene_Alley`, `padaria_scene`, `Acidente_Scene`, `Dedicatoria_Scene`.
- Scripts em `Assets/Scripts/` (`Fase_0`, `Fase_02`, `Fase_1`, `GameManager.cs`, `MainMenu.cs`, `DedicationScreen.cs`) e `Assets/Beco_Assets/Scripts/` (`DialogueManager`, `DialogueData`, `DialogueTrigger`, `CharacterDataBase`, `BecoSequenceManager`...). Sem asmdef (tudo em `Assembly-CSharp`).
- **Diálogos:** `DialogueData` é um `ScriptableObject` com lista de falas (`speaker` + `sentence`) e um `reportLabel` opcional. Os assets ficam em `Assets/sistemaDeDialogo/` (e alguns em `Beco_Assets/Diálogos`, `Scripts/Fase_1`, `Scripts/Fase_02` e `Scenes/padaria_scene`). O `DialogueManager` é singleton (`DontDestroyOnLoad`) e expõe `StartDialogue`, `PlayDialogueAuto`, `isDialogueActive`, `isAwaitingChoice` e o evento `OnDialogueEnded`.
- **Opções de resposta:** uma fala pode ter `choices` (`text`, `stage`, `response` opcional) e `choiceSpeaker`. Depois que a fala aparece, as opções surgem DENTRO da caixa de diálogo, embaixo da fala e alinhadas com ela (linhas discretas, fonte 14); a fala sobe pra abrir espaço e a caixa só cresce pra cima se não couber (teclas 1 a 4 ou clique, com o cursor liberado nesse momento). A opção escolhida vira uma fala de `choiceSpeaker`, depois vem a `response` (dita por quem falou a linha), e a história segue igual. Os botões são criados por código; dá pra trocar por `choicesContainer`/`choiceButtonPrefab` no Inspector. Os enums `CharacterType` e `GriefStage` são salvos como número: **só adicione valores no final**.
- **Interação:** a interface `IInteractable` (`Interact()`, `GetPromptText()`) é implementada por `ThoughtInteractable`, `DoorController`, `LightSwitch` e `SceneTransition`. `PlayerInteraction` (arquivo `PlayerInteraction1.cs`) faz o raycast e chama `Interact()` ao apertar E.
- **`GameManager`:** singleton criado por `[RuntimeInitializeOnLoadMethod]`; guarda estado entre cenas. O `ReportLogger` segue o mesmo padrão.
- As escolhas **não ramificam a história**: servem só pro relatório. Além delas, o jogador decide **o que observar, em que ordem e por quanto tempo**, e o relatório mede isso também (em números, sem nome de asset no `.txt`).
- **Fim de sessão:** voltar para `Scene_MainMenu` (por exemplo, depois da `Dedicatoria_Scene`) encerra a sessão no logger.

## Git e LFS

- Repositório: `https://github.com/joaoamorimBM/five-stages-game`. Branch padrão e mais atual: **`develop`** (a `main` está desatualizada).
- Trabalho novo em `feature/<nome>`, partindo da `develop`. Um commit por tarefa, mensagem clara em português.
- **Git LFS** guarda `.png`, `.jpg`, `.fbx`, `.wav`, `.mp3` e `.mp4`. Antes de clonar: `git lfs install`. Se um `.mp4` ou `.fbx` tiver ~130 bytes, é ponteiro, e o Unity dá "Error while reading movie". Conserto: `git lfs pull`.
- O `.gitattributes` tem linhas residuais (`git`, `lfs`, `install` com `filter=lfs`), inofensivas; podem ser limpas.
- Avisos "LF will be replaced by CRLF" no Windows são inofensivos.
- O Unity regrava arquivos sozinho ao abrir (`ProjectSettings/*`, assets de fonte TMP). Não misture esses ajustes com commits de funcionalidade.

## O que NÃO fazer

- **Não edite cenas (`.unity`) nem prefabs como texto.** Mudanças em cena são feitas no editor do Unity pelo João. Se uma tarefa exigir isso, descreva os passos para ele. **Exceções liberadas pelo João:** o prefab `Beco_Assets/Prefabs/UI/DialogueCanvas.prefab` e os textos do `Scene_MainMenu`.
- **Não apague assets.** Uma limpeza automática por GUID já foi tentada e **quebrou o jogo**: FBX procuram materiais e texturas **pelo nome**, sem GUID, então o script marcou como lixo coisas em uso (pinheiros rosa, café sem textura). Asset sem uso **não entra no build**, então isso não afeta o jogo final. Limpeza ficou **adiada** de propósito.
- Não toque em `Assets/Scenes/` (exceto os `.asset` de diálogo, como os de `padaria_scene`, que o João liberou), `Assets/Settings/`, `TextMesh Pro` nem nos pacotes de terceiros.
- Não commite `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Relatorios/`, nem chaves e segredos.

## Estado atual

- **Jogo (Fase 4):** `ReportLogger.cs` (formato v4, ganchos em `ThoughtInteractable` `LogObserveStart/End` e `DialogueManager` `LogDialogue/LogChoice`), `CodeScreen.cs` (tela de código por código, sem mexer na cena; `ArmSession`/`DisarmSession`), `PatientCode.cs` (mesma regra do `codigo.ts`), `ReportUploader.cs` (envia a cada save e ao abrir o jogo; 200 → `Relatorios/Enviados`, 4xx → `Relatorios/Recusados`, sem internet → fica) e `GameConfig.cs` (lê `Assets/Resources/five_stages_config.json`: `functionsUrl` vazio = offline). A sessão só grava com código aceito; no editor, dar Play direto numa cena (sem passar pelo menu) grava sem código, só local. Fechar o jogo no meio grava `partial: true`, enviado na próxima abertura.
- Diálogos com opções: `remedios`, `louca`, `Pensamento_Quadro_familia`, `Celular`, `TV`, `torradas`, `dialogoAntesFlashBack`, `dialogoPosFlashback`, `ConversaPadaria`, `DialogoGraceFinal`.
- `NPCInteraction` (padaria) não é `ThoughtInteractable`: o diálogo dele entra no relatório, mas não conta na exploração.
- Saída do logger (`ReportLogger.ReportsFolder`): no editor, `Relatorios/` na raiz do projeto (no `.gitignore`, nunca commitar); no build, `Application.persistentDataPath/Relatorios/`. Arquivos `sessao_<data>_<id>.json` e `.txt`.
- **Back-end (Fase 3):** `supabase/` publicado no projeto **Five Stages** (ref `stnzcrwdzwctpjnwamdo`, região São Paulo): migração aplicada e funções `submit-report` e `check-code` no ar (testadas com curl). Comandos: `npx supabase db push --remote` e `npx supabase functions deploy submit-report check-code --remote --use-api`.
- **Caixa de diálogo (`DialogueCanvas`):** sem escalas tortas (tudo em escala 1), caixa de 640 de largura, fala preenchendo a caixa com respiro (40 nas laterais, 46 em cima por causa da etiqueta do nome) e tamanho automático de 15 a 22. O Canvas usa referência 800×600 com match na largura. A tela de código usa a fonte do título do menu (Bebas Neue).
- Temas usados nos diálogos da casa: "Memórias da Claire" (foto, carta, TV, coruja), "Autocuidado" (remédios, louça, torradeira, geladeira), "Relação com a Emily" (Emily dormindo), "Rotina" (celular).
- **Site (Fase 5):** `site/` (Vite + React + TS, `react-router-dom`, `supabase-js`). Login por link mágico, Pacientes (gerar código, apelido local, sessões, apagar) e Sessão (resumo por regras, como respondeu com as opções não escolhidas e "hesitou" relativo à mediana dele, para onde olhou por tema, linha do tempo). Lê os formatos 3 e 4 (`src/lib/relatorio.ts`; o tipo do v4 está em `src/lib/tipos.ts`). Sem `.env.local`, roda em **modo demonstração** com dados de exemplo. Roda com `npm run dev --prefix site` (ou o preview "site" em `.claude/launch.json`). Passo a passo de deploy na Vercel: `site/README.md`.
- Próximos passos: ver `ROADMAP.md`.
