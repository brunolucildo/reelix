# Conventional Comments e a regra de veredito

Formato dos comentários da revisão (https://conventionalcomments.org), com um vocabulário **fechado** que o `scripts/review-lint.mjs` valida.

```text
<label> (<decorações>): <assunto em uma linha>

<discussão opcional: por quê, evidência, como corrigir>
```

- `label` e decorações em **inglês e minúsculas** (são palavras-chave); o texto do comentário segue o idioma da revisão (`pt-BR` por padrão).
- O assunto cabe em **uma linha** e diz o problema, não o sintoma vago ("falta `await`; a falha vira _unhandled rejection_", não "ver async").
- Decorações são opcionais, entre parênteses, separadas por vírgula: `issue (security): ...`, `suggestion (blocking, tests): ...`.

## Labels

| Label | Use para | Bloqueia? |
| --- | --- | --- |
| `praise` | Algo bem feito e que vale repetir. Seja específico; elogio genérico é ruído. | Nunca |
| `nitpick` | Preferência trivial que o lint/formatador não cobre. Use pouco. | Nunca |
| `suggestion` | Melhoria concreta que o autor pode aceitar ou não. Inclua a alternativa. | Só com `(blocking)` |
| `issue` | Problema **real e verificado**: bug, falha de segurança, quebra de regra do projeto, teste faltando para comportamento novo. | **Sempre** |
| `todo` | Mudança pequena e necessária, mas sem gravidade de bug. | Só com `(blocking)` |
| `question` | Dúvida genuína sobre intenção ou comportamento. | Só com `(blocking)` |
| `thought` | Ideia ou reflexão que pode virar trabalho futuro. Não pede mudança agora. | Nunca |
| `chore` | Tarefa de processo (atualizar doc, registrar ADR, rodar um comando). | Só com `(blocking)` |
| `note` | Informação sem pedido de ação (contexto, decisão já tomada, algo que você verificou). | Nunca |

## Decorações

**Comportamento:**

| Decoração | Efeito |
| --- | --- |
| `blocking` | O PR não deve ser mesclado antes de isto ser resolvido. |
| `non-blocking` | Não impede o merge. É o padrão de todo label diferente de `issue`; só escreva quando quiser enfatizar. |
| `if-minor` | O autor decide se o custo compensa. |

**Dimensão** (escolha a principal; uma por comentário): `standards`, `performance`, `bug`, `readability`, `a11y`, `i18n`, `tests`, `o11y`, `security`, `architecture`, `data`, `api`, `docs`, `scope`, `deps`. Elas espelham as dimensões de [`review-checklist.md`](review-checklist.md) e permitem ao autor filtrar a revisão.

Decoração fora desta lista **falha no lint**: um erro de digitação em `blocking` mudaria o veredito em silêncio.

## Regra de veredito (não negociável)

> Se existir **pelo menos um** comentário `issue` **ou** com a decoração `blocking`, a revisão é **REQUEST_CHANGES**. Caso contrário, é **APPROVE**. Não existe revisão neutra (só comentários).

- A regra vale para comentários inline **e** para os comentários gerais do corpo.
- `issue` é sempre bloqueante: `issue (non-blocking)` é erro de lint. Se não bloqueia, não é `issue`: use `suggestion`, `nitpick` ou `thought`.
- `praise`, `nitpick`, `thought` e `note` com `(blocking)` são erro de lint.
- O **script calcula o veredito**; você não o escreve. O corpo traz o marcador `{{VERDICT}}`, que o script troca pelo veredito real. Assim o texto nunca contradiz os comentários.
- Aprovação pode vir com comentários não bloqueantes (`suggestion`, `nitpick`, `praise`, `thought`, `note`).

### Como escolher entre `issue` e `suggestion`

Pergunte: _"Se o autor insistisse em mesclar assim, eu aceitaria?"_

- **Não** → `issue` (ou `suggestion`/`question`/`todo` com `(blocking)` quando o problema for de natureza diferente de defeito). Exige **evidência**: o que você leu, onde, e o cenário que falha.
- **Sim, mas ficaria melhor** → `suggestion` ou `nitpick`.
- **Não tenho certeza se é defeito** → verifique no código; se continuar incerto, `question`. Só vire `(blocking)` se a resposta mudar a correção do PR.

Nunca use `issue` por gosto, por estilo coberto pelo lint/formatador ou por dúvida não verificada.

## Como escrever bem

1. **Evidência primeiro.** Cite o trecho, o caminho e o cenário: "se `x` for `null` (ex.: conta sem sobrenome), a linha 42 lança `TypeError` e o endpoint responde 500".
2. **Diga o que fazer.** Aponte a correção ou a alternativa; quando for uma troca de código, use um bloco `suggestion` do GitHub (o autor aplica com um clique):

   ````text
   suggestion (readability): extraia o limite para uma constante

   ```suggestion
   const MAX_ATTEMPTS = 5;
   ```
   ````

3. **Explique o porquê** em uma frase, com a regra do projeto quando existir ("`DEVELOPMENT.md`: controller não acessa o `DbContext`"). Cite o documento e o tema; só cite número de seção se você abriu o arquivo e confirmou.
4. **Tom:** direto, respeitoso, sobre o código e não sobre a pessoa. Prefira "isto lança" a "você esqueceu". Pergunte quando for dúvida. Elogie o que é bom.
5. **Um problema por comentário.** Padrão repetido em vários lugares: comente no primeiro e liste os outros caminhos na discussão, em vez de repetir.
6. **Sem ruído:** não comente o que o lint do mobile (`npm run lint`), o compilador/analisadores do .NET e o formatador já pegam, nem código que o PR não tocou (a menos que o PR o torne errado).

## Exemplos

```text
issue (bug): `movie` pode ser `null` aqui e a linha 42 lança `NullReferenceException`

`FindByTmdbIdAsync` devolve `null` para título inexistente (ver `MovieRepository.cs:31`). O endpoint
passa a responder 500 em vez de 404 em `ProblemDetails`. Trate o `null` e devolva o erro de domínio.
```

```text
issue (a11y): o botão só com ícone não tem `accessibilityLabel`

O `Pressable` de "favoritar" renderiza só o ícone. Sem `accessibilityRole="button"` e
`accessibilityLabel`, TalkBack e VoiceOver não anunciam a ação. Garanta também a área de toque de 44px.
```

```text
issue (o11y, security): o e-mail vai para o log

`_logger.LogInformation("Login de {Email}", email)` grava PII em texto claro (`DEVELOPMENT.md`: não logar
token, e-mail, identificador de usuário ou corpo de requisição). Remova o parâmetro; o `traceId` basta para correlacionar.
```

```text
suggestion (performance): consultas independentes em sequência

As duas chamadas não dependem uma da outra; `await Task.WhenAll(...)` reduz a latência para a maior das duas.
```

```text
question (blocking, data): a migration é reversível?

Ela remove `users.legacy_id`. O `Down` recria a coluna, mas os dados não voltam. Existe backup
ou a coluna já está sem uso em produção?
```

```text
nitpick: `data` é genérico; `session` diz mais
```

```text
praise (tests): ótimo cobrir o 429 com `FakeTimeProvider`; o teste não depende de `Thread.Sleep`
```

```text
note: o fallback para dados em cache quando o provedor falha é decisão já registrada em ADR; nada a mudar
```

## Comentários gerais (no corpo)

Só para o que **não dá para ancorar em uma linha do diff** (ex.: "não há teste de integração para o endpoint novo", "falta ADR"). Escreva cada um **em uma linha, no mesmo formato**, dentro da seção "Comentários gerais" do corpo, sem negrito nem prefixo:

```text
- issue (tests): o endpoint `POST /api/...` não tem teste de integração
```

Linhas nesse formato contam para o veredito. Qualquer outra linha do corpo (tabelas, listas de resumo) **não** pode começar com um label seguido de `:` por acidente.
