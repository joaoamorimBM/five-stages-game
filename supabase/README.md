# Back-end (Supabase)

```
Site do psicólogo ──(login, RLS)──► pacientes / sessoes ◄──(service role)── submit-report ◄── Jogo
                                                                             check-code    ◄── Jogo
```

- `migrations/`: tabelas `pacientes` e `sessoes`, view `sessoes_resumo` e as regras de acesso (RLS).
- `functions/submit-report`: recebe o relatório do jogo, valida e faz upsert por `sessionId`.
- `functions/check-code`: diz ao jogo se um código existe. Responde só sim ou não.
- `functions/_shared`: o código do paciente (com dígito verificador) e a validação do relatório. Os testes rodam com Node, sem instalar nada:

```bash
node supabase/functions/_shared/codigo.test.ts
```

```bash
node supabase/functions/_shared/relatorio.test.ts
```

## Privacidade

- Nenhuma tabela guarda dado do paciente: ele é só um código (`XXXX-XXXX`).
- O único dado pessoal é o e-mail do psicólogo, que fica no Supabase Auth.
- O jogo não carrega nenhuma chave. As funções validam tudo antes de escrever.
- As funções nunca logam o conteúdo do relatório.
- Apagar um paciente apaga as sessões dele. Apagar a conta do psicólogo apaga tudo dele.

## Subir pela primeira vez

1. Crie um projeto em [supabase.com](https://supabase.com) na região **South America (São Paulo)**, pra os dados ficarem no Brasil. Anote o *Project ref* (está na URL do painel).
2. Na raiz do repositório, faça login e ligue o projeto:

```bash
npx supabase login
```

```bash
npx supabase link --project-ref SEU_PROJECT_REF
```

3. Crie as tabelas:

```bash
npx supabase db push
```

4. Publique as funções:

```bash
npx supabase functions deploy submit-report check-code
```

5. No painel, em **Authentication → Sign In / Providers**, deixe o **Email** ligado (login por link mágico). Em **URL Configuration**, coloque a URL do site (quando ele existir) em *Site URL*.

## Testar as funções

Troque `SEU_PROJECT_REF` pelo seu. Um código com formato válido, mas que não existe no banco, tem que voltar `"existe": false`:

```bash
curl -s -X POST https://SEU_PROJECT_REF.supabase.co/functions/v1/check-code -H "content-type: application/json" -d "{\"codigo\":\"K7M2-9QXP\"}"
```

Pra testar o envio, crie um paciente pelo site (ou pelo SQL Editor, com o seu usuário) e envie um `.json` da pasta `Relatorios/` com o `patientCode` trocado pelo código dele:

```bash
curl -s -X POST https://SEU_PROJECT_REF.supabase.co/functions/v1/submit-report -H "content-type: application/json" --data-binary @Relatorios/sessao_exemplo.json
```

Respostas esperadas:

| Situação | Status | `erro` |
|---|---|---|
| Tudo certo | 200 | — |
| Código com formato ou verificador errado | 400 | `codigo_invalido` |
| Código que não existe | 404 | `codigo_desconhecido` |
| Mesmo `sessionId` de outro paciente | 409 | `sessao_de_outro_paciente` |
| Corpo acima de 512 KB | 413 | `grande_demais` |
| `formatVersion` fora de 3 e 4 | 400 | `formato_nao_suportado` |
