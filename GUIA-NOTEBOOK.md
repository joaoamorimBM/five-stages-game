# Guia: Five Stages num computador novo (notebook)

Passo a passo pra deixar o jogo, o site e o Claude Code funcionando, e o que fazer quando algo dá errado.

**Recomendação: clone de novo, numa pasta nova.** É o caminho mais seguro. Um clone antigo pode estar na branch errada, ter arquivos do LFS faltando (texturas rosa, vídeo que não abre) ou mudanças locais esquecidas. Não apague a pasta antiga até a nova funcionar: renomeie ela, por exemplo, pra `five-stages-game-antigo`.

---

## 1. Programas (instalar uma vez)

| Programa | Pra quê | Como conferir |
|---|---|---|
| **Git** | baixar o projeto | `git --version` |
| **Git LFS** | imagens, modelos 3D, áudio e vídeo | `git lfs version` |
| **Unity Hub** + editor **6000.3.9f1** | o jogo (a versão tem que ser essa) | no Unity Hub, aba *Installs* |
| **Node.js 20 ou mais novo** | site e Supabase CLI | `node --version` |
| **VS Code** | editar código | — |
| **Claude Code** | o assistente | `claude --version` (ou o app) |
| GitHub CLI (opcional) | abrir PR pelo terminal | `gh --version` |

No Unity Hub, ao instalar o 6000.3.9f1, pode marcar só *Windows Build Support*. O resto não é necessário.

## 2. Baixar o projeto

No PowerShell, dentro da pasta onde quer o projeto (ex.: `D:\Unity\fiveStages`):

```powershell
git lfs install
git clone --branch feature/site-psicologo https://github.com/joaoamorimBM/five-stages-game.git five-stages-game
cd five-stages-game
git lfs pull
```

> Se o PR já foi aceito, troque `feature/site-psicologo` por `develop`.

**Conferir se o LFS baixou tudo:**

```powershell
(Get-Item Assets\Scenes\Menu\videoMenu.mp4).Length
```

Tem que dar `1375962`. Se der algo perto de 130, os arquivos grandes não vieram: rode `git lfs pull` de novo.

```powershell
git lfs ls-files | Select-String "^[0-9a-f]+ - "
```

Tem que voltar **vazio**. Se aparecer alguma linha, são arquivos que ficaram como "ponteiro": rode `git lfs pull`.

Configure seu nome no git, se ainda não fez nesse computador:

```powershell
git config --global user.name "João Pedro Amorim"
git config --global user.email "seu-email@exemplo.com"
```

## 3. Abrir o jogo

1. Unity Hub → **Add** → **Add project from disk** → escolha a pasta `five-stages-game`.
2. Abra com a versão **6000.3.9f1**. A primeira vez demora (ele monta a pasta `Library`, uns 10 a 30 minutos).
3. Em **Edit → Preferences → External Tools**, escolha **Visual Studio Code** e clique em **Regenerate project files**.
4. Abra `Assets/Scenes/Menu/Scene_MainMenu` e dê **Play**, com a aba Game em **Full HD (1920x1080)**.

O que deve acontecer:
- Aparece a tela **"ANTES DE COMEÇAR"**, com o campo do código.
- Com um código gerado no site, aparece "Código reconhecido" e o jogo segue pro menu.
- Na casa, ao observar a foto da família, a caixa de diálogo nova aparece (retrato à esquerda, nome em cima, opções dentro da caixa).

## 4. Rodar o site

```powershell
npm install --prefix site
npm run dev --prefix site
```

Abra http://localhost:5173.

- **Sem configurar nada**, o site abre em **modo demonstração** (aparece "Dados de exemplo" no topo). Serve pra mostrar sem internet.
- **Pra usar de verdade:** copie `site\.env.example` pra `site\.env.local` e cole a **Publishable key** do Supabase (Painel → Project Settings → API Keys, começa com `sb_publishable_`). A URL já está no exemplo: `https://stnzcrwdzwctpjnwamdo.supabase.co`. **Nunca use a secret key.**
- O login é por link no e-mail. Abra o link **no mesmo navegador** em que pediu.

O `.env.local` não vai pro git de propósito: tem que ser criado em cada computador.

## 5. Claude Code no notebook

1. Abra o Claude Code **na pasta do projeto**. Ele lê o `CLAUDE.md` sozinho, e lá está todo o contexto: o modo rápido, o que já foi feito, as regras e os próximos passos.
2. Instale de novo o **plugin oficial da Unity** (os plugins não passam de um computador pro outro).
3. Pra testar se ele entendeu, pergunte: *"Leia o CLAUDE.md e me diga em 5 linhas o estado do projeto e o que falta."*
4. **Supabase CLI** (só se for mexer no banco ou nas funções): rode `npx supabase login` **você mesmo** no terminal e depois `npx supabase link --project-ref stnzcrwdzwctpjnwamdo`.

## 6. Se der errado

| Sintoma | Causa provável | Como resolver |
|---|---|---|
| "Error while reading movie", texturas **rosa**, modelos sem textura | Arquivos do LFS não baixaram (ficaram como ponteiro de ~130 bytes) | `git lfs install` e depois `git lfs pull`. Confira de novo com os comandos da seção 2. |
| Unity pede outra versão ou abre em "Safe Mode" | Versão errada do editor | Instale a **6000.3.9f1** exata no Unity Hub. Não aceite "upgrade". |
| Erros vermelhos de script no Console | Compilação | Mande o **primeiro** erro vermelho pro Claude. Ele também pode rodar `powershell -ExecutionPolicy Bypass -File tools/checar-csharp.ps1`. |
| "Recovering Scene Backups" ao abrir | O Unity fechou mal antes | Clique em **Sim** (só copia backups pra `Assets/_Recovery`, sem mexer nas cenas). Não commite essa pasta. |
| A tela de código não aparece | Play não começou pelo menu | Dê Play no `Scene_MainMenu`. Começando direto em outra cena, o jogo grava sem código, só local (modo teste). |
| "Esse código não existe" | O código não está no banco, ou foi apagado | Gere outro no site. Confira se o site está no modo real, e não no de demonstração. |
| A sessão não aparece no site | Sem internet, ou o relatório foi recusado | Veja a pasta `Relatorios/` na raiz: `Enviados` = chegou, `Recusados` = o servidor recusou (veja o Console), solto na pasta = ainda não enviou (tenta de novo ao trocar de cena ou abrir o jogo). |
| Site mostra "Dados de exemplo" | Falta o `site/.env.local` ou a chave está vazia | Crie o arquivo como na seção 4 e reinicie o `npm run dev`. |
| O link do e-mail abre uma página que não carrega | Endereço de retorno não liberado no Supabase | Painel → Authentication → URL Configuration: Site URL `http://localhost:5173` e Redirect URL `http://localhost:5173/**`. |
| "Muitas tentativas" no login | Limite de e-mails do plano grátis (poucos por hora) | Espere um pouco antes de pedir outro link. |
| `npm install` falha | Node antigo | Instale o Node 20 ou mais novo e rode de novo. |
| `npx supabase ...` dá "Cannot find project ref" | Projeto não ligado nesse computador | `npx supabase login` e `npx supabase link --project-ref stnzcrwdzwctpjnwamdo`. |
| Arquivos modificados que você não mexeu (`ProjectSettings/...`, `BebasNeue-Regular SDF.asset`) | O Unity regrava sozinho ao abrir | Normal. Não misture nos seus commits. |
| Avisos "LF will be replaced by CRLF" | Diferença de quebra de linha Windows/Linux | Inofensivo. Ignore. |

### Último recurso: clone limpo

Se nada resolver, comece do zero sem perder nada:

```powershell
cd D:\Unity\fiveStages
Rename-Item five-stages-game five-stages-game-antigo
```

Depois repita as seções 2 a 4. Quando a pasta nova estiver funcionando, a antiga pode ser apagada. Antes, confira se não ficou nada importante só nela: rode `git status` dentro dela e veja se tem commits que não foram enviados.

## 7. O que nunca vai pro git

- `site/.env.local` (chave do site)
- `Relatorios/` (relatórios gerados no editor: dado sensível)
- `supabase/.temp/` (estado local do CLI)
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` (o Unity recria)
- Qualquer senha, token ou *secret key*
