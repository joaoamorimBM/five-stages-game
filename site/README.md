# Site do psicólogo

Vite + React + TypeScript, no estilo "caderno de sessão". Conversa com o Supabase só pela chave pública: quem protege os dados é o RLS do banco (`supabase/migrations`).

## Rodar no computador

```bash
npm install --prefix site
```

```bash
npm run dev --prefix site
```

Abra http://localhost:5173. **Sem configurar nada, o site abre em modo demonstração**, com pacientes e sessões de exemplo, e nada sai do navegador. Serve pra mostrar pra banca ou pra um psicólogo antes do back-end existir.

## Ligar no Supabase de verdade

1. Suba o back-end (veja `supabase/README.md`).
2. Copie `site/.env.example` para `site/.env.local` e preencha `VITE_SUPABASE_URL` e `VITE_SUPABASE_ANON_KEY` (Painel → Project Settings → API). Use **só a chave pública**: a service role nunca entra no site.
3. Rode `npm run dev --prefix site` de novo e entre com o seu e-mail (chega um link de acesso).

## Publicar na Vercel

1. Na Vercel, importe o repositório e defina **Root Directory = `site`** (framework: Vite).
2. Em *Environment Variables*, coloque as mesmas duas variáveis do `.env.local`.
3. Depois do primeiro deploy, coloque a URL do site no Supabase em **Authentication → URL Configuration → Site URL**, senão o link do e-mail volta pro lugar errado.

O `vercel.json` já cuida das rotas (`/sessao/...`) e de alguns cabeçalhos de segurança.

## Onde está cada coisa

- `src/lib/relatorio.ts`: transforma o JSON do jogo (formatos 3 e 4) na leitura organizada, e monta as frases do resumo com regras fixas.
- `src/lib/dados.ts`: o acesso ao banco (ou aos dados de exemplo, em `demo.ts`).
- `src/lib/apelidos.ts`: os apelidos, que ficam só no `localStorage`.
- O código do paciente vem de `supabase/functions/_shared/codigo.ts`, a mesma regra usada pelas Edge Functions.
- `src/telas/`: Login, Pacientes, Sessão e Privacidade.
