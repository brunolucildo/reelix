# Checklist de revisão

Oito dimensões prioritárias (as que o time mais valoriza), depois as dimensões extras. A numeração é a ordem de prioridade quando o tempo é curto. A decoração entre crases é a que o comentário usa (ver [`conventional-comments.md`](conventional-comments.md)).

Quando um item cita um documento (`DEVELOPMENT.md`, `ARCHITECTURE.md`, `QA.md`, `TESTING.md`, `DESIGN_SYSTEM.md`, `RULES.md`), ele aponta para `.docs/` do repositório: abra a seção do tema antes de citá-la em um comentário e **não invente número de seção**. **O código vence os docs**: se divergirem, vale o que roda e você registra a divergência como `note`/`chore (docs)`.

Aplique só o que o diff toca. Não percorra o checklist inteiro por obrigação: ele serve para você **não esquecer**, não para gerar comentário.

Mapa do repositório: backend .NET 10 em `backend/Reelix.{Api,Application,Domain,Infrastructure,UnitTests}` (solução `Reelix.sln`); mobile Expo/React Native/TypeScript em `mobile/` (npm); migrations em `backend/Reelix.Infrastructure/Persistence/Migrations`.

## Como ler o PR

1. Intenção: título, descrição, spec/plano/ADR citados (`.specs/`, `.plans/`, `.adrs/`). O diff faz o que o PR diz? Falta algo que a spec marca como `Must`?
2. Risco: onde um erro custa mais (autenticação e autorização por recurso, dados, contrato de API, UI de fluxo crítico, migration). Revise essas áreas primeiro e com mais cuidado.
3. Testes antes do código: o que o PR promete provar? Os testes provam isso?
4. Depois leia o código de produção, arquivo a arquivo, com contexto (arquivo inteiro, chamadores, testes existentes), não só as linhas verdes.

---

## 1. Padrões — `standards`

O PR segue as convenções do projeto e do stack?

- **Camadas e regra de dependência (`ARCHITECTURE.md`, `DEVELOPMENT.md`):** `Api → Application → Domain`; `Infrastructure` implementa as portas definidas em `Application`/`Domain`; o `Domain` não referencia framework, EF, HTTP nem SDK de provedor. Controller/endpoint fino (DTO → caso de uso → resposta), sem acesso a `DbContext` e sem regra de negócio. Um fluxo, um caso de uso; sem "service" genérico que agrega fluxos. Projeto novo ou `ProjectReference` novo que inverte a dependência é `issue`.
- **SOLID e design:** uma responsabilidade por classe; dependência por abstração injetada (construtor), sem `new` de colaborador nem localizador estático; interface só com 2+ usos ou fronteira real (KISS); herança só quando substituível (LSP); interface pequena e coesa (ISP).
- **C# (`DEVELOPMENT.md`):** `nullable` respeitado (sem `!` sem invariante comentada), `async`/`await` de ponta a ponta (sem `.Result`/`.Wait()`), `CancellationToken` propagado do endpoint até o EF/HTTP, `record`/`init` para DTOs e value objects, exceções de domínio tipadas no lugar de `Exception` genérica, sem `#pragma warning disable`/`[SuppressMessage]` sem justificativa.
- **TypeScript (`mobile/`):** sem `any`, `@ts-ignore`, `eslint-disable` sem justificativa; `unknown` + estreitamento; `import type`; `as`/`!` só com invariante comentada; uniões discriminadas no lugar de flags; dado externo (resposta da API, `AsyncStorage`, deep link) validado/estreitado na fronteira.
- **API (`DEVELOPMENT.md`):** DTO de entrada validado na borda (campo obrigatório, tamanho, faixa), JSON em `camelCase`, datas ISO 8601 UTC, erro como `ProblemDetails` com `title`/`detail` em **pt-BR** (e `status` coerente), sem stack nem detalhe interno no corpo. Endpoint novo é fechado por padrão (`[Authorize]`/política); `[AllowAnonymous]` exige justificativa. Rate limit (política ASP.NET) em rota sensível ou cara de atacar. Nada de SDK/HTTP de provedor (TMDB, OMDb, OpenAI, Expo Push) fora de `Infrastructure`.
- **Mobile (`DEVELOPMENT.md`, `DESIGN_SYSTEM.md`):** dado remoto com React Query (não `useEffect` + `useState` para carga); chaves de query estáveis e hierárquicas; mutation invalida/atualiza as queries afetadas; HTTP só pelo cliente de API da app (tela não chama `fetch` direto); navegação pelas rotas/estrutura do `mobile/`; componentes só apresentam; segredo nunca no cliente; só `EXPO_PUBLIC_*` (públicas) e a publishable key no bundle, nunca chave `service_role`/secret nem token de provedor.
- **Design system (`DESIGN_SYSTEM.md`):** cor, espaçamento, raio e tipografia vêm de **tokens de tema**, nunca de hex/valor solto; componente base aceita `style`/variantes explícitas e documenta os estados; sem copiar componente para "mudar só uma coisinha"; feedback segue a regra do projeto: **toast** para resultado de ação transitória, **erro inline** (texto junto ao campo/tela) para validação e falha que o usuário precisa corrigir.
- **Dados (`DEVELOPMENT.md`):** ver dimensão 12.
- **Idioma:** ver dimensão 6.
- **Anti-padrões:** `console.log`/`Console.WriteLine`, código morto, template demo, regra de negócio em componente/controller, `catch` vazio.
- **Processo:** commits em Conventional Commits (descrição em pt-BR, `type`/`scope` em inglês); PR sem segredo, `.env`, `mobile/.env` nem artefato gerado (`bin/`, `obj/`, `node_modules/`, `.expo/`, `dist/`, `coverage/`, `TestResults/`).

`issue` quando viola regra "não negociável" dos docs. `suggestion`/`nitpick` quando é preferência.

## 2. Performance — `performance`

- **Banco (EF Core/Npgsql):** N+1 (loop com consulta ou navegação preguiçosa), falta de `Include`/projeção (`Select`) onde bastam algumas colunas, `AsNoTracking` ausente em leitura, filtro/ordenação sem índice, ausência de paginação/limite, `ToList()` antes do `Where` (filtro em memória), transação longa ou lock amplo, índice novo sem justificativa. Consulte a skill `supabase-postgres-best-practices` para migration e SQL.
- **Async:** `await` em loop de chamadas independentes (`Task.WhenAll`), trabalho CPU-bound ou bloqueante (`.Result`, `Thread.Sleep`) em request, fila/recurso sem limite.
- **Memória:** crescimento sem teto (cache, `static` mutável, `Dictionary` em processo), `IDisposable` não descartado, `HttpClient` criado por chamada (use `IHttpClientFactory`/cliente tipado), vazamento de timer/listener/subscription no mobile.
- **Rede/integrações:** chamada externa (TMDB, OMDb, OpenAI, Expo Push) sem timeout, retry ilimitado, chamada em loop, payload grande demais, ausência de cache onde o dado é estável (catálogo).
- **Mobile:** re-render desnecessário, listas grandes sem `FlatList`/`FlashList` (e `keyExtractor`/`getItemLayout` quando couber), imagem sem tamanho/cache, import pesado no bundle inicial, cascata de requests (_waterfall_), `staleTime`/invalidação do React Query mal configurados, `useEffect` que dispara em loop, `useMemo`/`useCallback` espalhados sem medida.
- **Medida antes de otimizar:** não peça microotimização sem evidência de caminho quente. `performance` vira `issue` só com custo concreto (ex.: O(n²) em lista sem limite, query sem índice em tabela crescente, N+1 em endpoint de lista); senão, `suggestion`.

## 3. Bug e funcionalidade — `bug`

O código faz o que deveria, inclusive quando algo dá errado?

- **Lógica:** off-by-one, condição invertida, `null`, `NaN`, comparação de datas/fuso (UTC no servidor), arredondamento, `DateTime.Now` onde cabe `UtcNow`/`TimeProvider`.
- **Concorrência:** _check-then-act_ (consulta e depois insere), corrida entre requests, dupla submissão (botão sem desabilitar no mobile); concorrência se resolve no banco (índice único + tratamento de `DbUpdateException`/`DbUpdateConcurrencyException`). Idempotência de operações repetidas.
- **Cancelamento:** `CancellationToken` ignorado em operação longa; `OperationCanceledException` tratada como erro 500.
- **Erros:** `catch` que engole, `async void`, `Task` sem `await`, exceção de domínio mapeada para o status errado, falha de integração derrubando a demanda inteira (isolar e informar a lacuna), 503 que vira 500 ou erro genérico sem `ProblemDetails`.
- **Estados (`DEVELOPMENT.md`, `QA.md`):** vazio, carregando, erro, cancelamento e _retry_ existem e funcionam; botão desabilita durante envio; sucesso parcial; teclado/safe area/orientação no mobile.
- **Contrato:** o comportamento bate com a spec e os critérios de aceite; compatibilidade com versões do app já instaladas (o app em campo não atualiza junto com a API); respostas iguais para casos que não podem revelar existência (ex.: recuperação de conta).
- **Regressão:** o PR muda algo usado por outro fluxo? Procure os chamadores (`git grep` no head do PR).
- Prove o bug com um cenário concreto (entrada → saída errada). Sem cenário, é `question`, não `issue`.

## 4. Legibilidade — `readability`

- Nomes revelam intenção; sem abreviação obscura; booleanos com `is/has/can`; convenções de nomes do C# (as do `.editorconfig` e do código existente) e do TS (camelCase, componentes em PascalCase).
- Métodos/funções curtos, uma responsabilidade; aninhamento raso (retorno antecipado); complexidade que pede extração.
- Sem comentário óbvio; **comentário que explica o porquê** onde a regra não é evidente; sem código comentado ou morto; sem número mágico (constante nomeada); sem `TODO` sem dono/issue.
- **DRY de conhecimento, WET de estrutura (`DEVELOPMENT.md`):** a regra de negócio vive em um lugar; não peça abstração para duas ocorrências ainda incertas, mas aponte a mesma regra copiada em três lugares.
- Tipos expressivos (`record`, uniões discriminadas) em vez de flags e `string` solta; `using`/`import` organizados; arquivo no lugar certo da arquitetura.
- Mantenha como `suggestion`/`nitpick` quase sempre; `issue` só quando a falta de clareza esconde um defeito ou viola regra dos docs.

## 5. Componentes acessíveis — `a11y`

WCAG 2.2 AA é o piso (`DEVELOPMENT.md`, `QA.md`, `DESIGN_SYSTEM.md`). Só se o PR toca UI (React Native e Expo web).

- **Papel, nome e estado:** `accessibilityRole` correto (`button`, `link`, `header`, `image`, `tab`...); todo controle interativo com `accessibilityLabel` (botão só com ícone **precisa** de label); `accessibilityState` (`selected`, `disabled`, `busy`, `expanded`) e `accessibilityHint` quando o efeito não é óbvio; nunca `View`/`Pressable` clicável sem papel e nome.
- **Alvo de toque:** mínimo de **44×44** (use `minHeight`/`minWidth` ou `hitSlop`); espaçamento entre alvos que evite toque errado.
- **Formulários:** todo campo com rótulo visível e `accessibilityLabel`; erro em **texto inline** junto ao campo (não só cor, não só toast), associado ao campo e anunciado (`accessibilityLiveRegion`/`announceForAccessibility`); não desloque o botão de envio ao mostrar erro; `keyboardType`, `textContentType`/`autoComplete` corretos; teclado não cobre o campo.
- **Toast vs inline:** toast some sozinho e some do leitor de tela; erro que o usuário precisa corrigir nunca pode ficar só em toast.
- **Foco e leitor de tela:** ordem de leitura lógica (TalkBack/VoiceOver); foco movido ao abrir modal/folha e devolvido ao fechar; modal marca o conteúdo de fundo como inacessível; sem armadilha de foco. No Expo web: elementos nativos/`role` corretos, foco visível, navegação por teclado (Tab, Enter, Espaço, Esc).
- **Visual:** contraste AA (texto 4,5:1; texto grande e ícones/bordas de UI 3:1) usando os tokens aprovados; texto escala com o tamanho de fonte do sistema (`allowFontScaling`, sem altura fixa que corte); `accessibilityLabel` em imagem relevante e imagem decorativa escondida do leitor; `reduceMotion` respeitado em animação; layout em tela pequena, grande e paisagem sem corte.
- **Componentes base (`DESIGN_SYSTEM.md`):** aceitam `style`, variantes explícitas e todos os estados (pressed, focused, disabled, loading, error).
- Falha de acessibilidade que **impede** o uso por leitor de tela, teclado ou toque é `issue (a11y)`; refinamento é `suggestion`. Skill `web-design-guidelines` ajuda.

## 6. I18n — `i18n`

O projeto **não tem biblioteca de i18n**; adotar uma exige decisão e ADR. Até lá, a regra é de **idioma e preparo**:

- **pt-BR** em: texto de interface (copy do app), `title`/`detail` de `ProblemDetails`, mensagens de validação exibidas ao usuário, notificações push, docs, specs, planos, ADRs e commits. **Inglês** em: identificadores, nomes de classe/arquivo/rota técnica, comentários e mensagens de log/exceção interna no código. Copy de interface em inglês no código novo, ou mensagem de API ao usuário em inglês, é `issue (i18n)`; identificador em português também (pedir migração em código antigo é `suggestion`).
- **Texto de produto hardcoded em componente base** do design system é proibido: texto entra por props/`children`.
- **Concatenação de frases** ("Olá, " + nome + "!") dificulta tradução futura: prefira um único template com placeholders. Plural e gênero não podem ser `n + " item(s)"` espalhado; trate singular/plural corretamente em pt-BR.
- **Formatação:** data, número, duração e lista via `Intl`/`toLocaleString` com locale explícito (`pt-BR`) no app, não template manual; a API trafega ISO 8601 UTC e números crus, não texto formatado. Cuidado com fuso ao exibir data de lançamento.
- **Layout:** não assuma largura de texto: sem largura fixa em botão/rótulo; sem texto dentro de imagem.
- **Centralização:** mensagens do mesmo fluxo num único módulo (ex.: `describe-sign-in-failure.ts`), não espalhadas, para uma futura extração.
- Não peça lib de i18n em PR que não a introduz; no máximo `thought (i18n)`.

## 7. QA (testes) — `tests`

Comportamento novo ou bug corrigido precisa de teste (`TESTING.md`, `QA.md`). A pergunta central: **este teste falharia se o código estivesse errado?**

- **Nível certo:** regra de domínio/aplicação pura → unitário xUnit (`backend/Reelix.UnitTests`); contrato HTTP, EF/Npgsql, constraints, autorização por recurso → integração com `WebApplicationFactory` e Postgres via Testcontainers (exige Docker); no mobile, regra pura/hook/componente → Jest + React Native Testing Library e fluxo crítico de usuário → Playwright sobre Expo web (**ambos gates futuros: cobre-os quando existirem**; até lá, falta de teste de UI é `note` ou `suggestion (tests)` e o QA manual descrito no PR precisa estar claro). Teste de arquitetura (dependência entre camadas) idem, quando existir.
- **Cobertura do que importa:** caminho feliz **e** caminhos de erro, limites, vazio, permissão negada, **acesso cruzado entre usuários (IDOR)**, 400/401/403/404/409/429/503, idempotência, falha de provedor externo. Bug corrigido ganha teste de regressão.
- **Qualidade:** testa comportamento observável, não implementação; nome descreve cenário e resultado; assert forte (nada de `Assert.True(x != null)` onde cabe valor exato); sem `Thread.Sleep`/timer real (use `TimeProvider`/relógio falso); sem dependência de rede externa (TMDB/OMDb/OpenAI fakeados), dado real ou ordem de execução; dados únicos por teste e limpeza; usar os helpers/fixtures existentes (fábrica da API, builders, fixture do Testcontainers) em vez de reinventar.
- **Sinais de alerta:** teste editado para passar (asserção relaxada, `[Fact(Skip = ...)]`, `.only`/`xit`, `expect`/`Assert` removido), mock que reimplementa o código testado, teste que passaria sem a mudança, snapshot gigante sem leitura, flakiness por concorrência.
- **CI/gates:** consulte `gh pr checks`. Falha causada pelo PR é `issue (tests)`. Se **não há CI** ou nada foi executado, registre em "Não verificado": você **não roda** os testes (não faz checkout do PR). Os gates reais estão em `QA.md` (`dotnet build/test` no backend; `npm run lint`, `npx tsc --noEmit`, `npm audit` no mobile).
- Comportamento novo **sem nenhum teste** onde já existe runner é `issue (tests)`. Teste existente mas fraco é `suggestion (tests)`.

## 8. Observabilidade — `o11y`

Não há stack de observabilidade implantada (métricas, traces, dashboards, alertas: item REELIX-016, pendente). **Não peça** métrica, dashboard ou alerta em PR comum; no máximo `thought (o11y)`. A dimensão hoje é sobre **logs e sinal de diagnóstico**. Só se o PR toca backend, jobs, integrações ou fluxo operacional.

- **Logs estruturados:** `ILogger<T>` com _message template_ e parâmetros nomeados (`"Falha ao consultar {Provider}"`, nunca interpolação `$"..."`); nunca `Console.WriteLine`/`console.log` em código de produto; nível adequado (`Warning` para falha esperada, `Error` para inesperada, `Information` para marcos de negócio); correlação por `TraceIdentifier`/`traceId` do `ProblemDetails`.
- **Sem PII e sem segredo:** nenhum token, senha, JWT, e-mail, nome, `userId`, header `Authorization`, corpo de requisição ou connection string em log ou mensagem de exceção exposta. Atenção a: URL de provedor com chave na query (ex.: TMDB com `api_key` na URL) em log de `HttpClient`; `logger.LogError(ex, ...)` com exceção do Npgsql/EF que carrega valores; `ToString()` de DTO com dado pessoal.
- **Sem sinal = sem diagnóstico:** `catch` vazio, erro engolido, caminho de falha de integração sem log; falha de provedor externo precisa de log com o nome do provedor e o resultado (sem dado sensível).
- **Mobile:** sem `console.log` em produto; nada de dado pessoal ou token em log, crash report ou analytics.
- **Resiliência do log:** log nunca decide a resposta nem derruba o request.
- Vazamento de PII/segredo em log é `issue (o11y, security)`. Falta de log em caminho de falha de integração nova é `suggestion`, ou `issue` quando o fluxo é crítico e a falha ficaria invisível.

---

## Dimensões extras (além das oito)

### 9. Segurança — `security`

- **Autenticação:** o JWT do Supabase é validado pela API (assinatura, `iss`, `aud`, expiração); a API não confia em `userId` vindo do corpo/rota sem checar contra o `sub` do token.
- **Autorização por recurso (IDOR):** endpoint novo é fechado por padrão; todo acesso a recurso do usuário filtra por dono/escopo (`WHERE user_id = @sub` ou equivalente) **na consulta**, não depois; `[AllowAnonymous]` novo precisa de motivo. Exija **teste de acesso cruzado** (usuário B não lê nem altera o recurso do usuário A, e recebe 404/403 sem revelar existência). O RLS fica ligado nas tabelas, mas a API acessa o banco por connection string própria: **o RLS não protege rota da API**, então um predicado de dono ausente numa query é falha de autorização. Tabela nova precisa de RLS ligado na migration.
- **Entrada e saída:** validação na borda; injeção (SQL concatenado em `FromSqlRaw`/`ExecuteSqlRaw` em vez de parametrizado, comando, HTML); SSRF e _path traversal_ em integrações/upload; redirecionamento aberto; deep link do app confiando em parâmetro sem validar.
- **Segredos e dados:** nada de chave/token/`.env`/`mobile/.env` no repositório; `ConnectionStrings__Reelix`, chave Supabase `service_role`/secret e tokens `TMDB`/`OMDb`/`OpenAI`/`ExpoPush` só no servidor (user-secrets/variável de ambiente), **nunca** em `EXPO_PUBLIC_*` nem no bundle; só `EXPO_PUBLIC_*` (públicas) e a publishable key no cliente; token de sessão em armazenamento seguro no dispositivo (não em log); mensagem de erro sem stack nem detalhe interno; dado pessoal minimizado, sem retenção indefinida (LGPD).
- **Abuso:** rate limit (política ASP.NET) em endpoint sensível/caro (busca que chama provedor pago, IA, push), comparação em tempo constante para segredos, enumeração de contas.
- **Dependência:** pacote novo com CVE conhecida, abandonado ou de licença incompatível (`npm audit`, advisories do NuGet).
- Falha de segurança comprovada é `issue (security)`. Defesa em profundidade é `suggestion`. Ao citar um segredo vazado, indique arquivo e linha **sem repetir o valor**.
- Decisões já tomadas e documentadas em ADR/`.docs/` **não** são defeitos: registre `note` se relevante.

### 10. Arquitetura e design — `architecture`

Dependências apontando para dentro (`Api → Application → Domain`, `Infrastructure` por inversão); portas no `Application`/`Domain`; abstração justificada por dois usos reais (KISS/WET); acoplamento a detalhe (EF, SDK de provedor, HTTP) no domínio; camada que conhece demais outra; decisão estrutural **sem ADR** (nova dependência relevante, contrato entre partes, modelo de dados, autenticação/autorização, estratégia de testes, observabilidade, deploy) é `chore (architecture, docs)` bloqueante conforme `RULES.md`.

### 11. Resiliência — `bug` ou `architecture`

Timeout, retry limitado com _backoff_, erro tipado, falha parcial (provedor de catálogo fora do ar não derruba a tela inteira), _fail-open_ vs _fail-closed_ consciente, limpeza de recursos (`using`/`await using`, timer, listener, conexão), ordem correta commit → efeito colateral (push/e-mail só **depois** do commit), idempotência de reentrega.

### 12. Dados e migrations — `data`

- **Migration EF Core** (`backend/Reelix.Infrastructure/Persistence/Migrations`): gerada, com **`Down` funcional** e **snapshot** (`*ModelSnapshot.cs`) atualizado e coerente com o modelo; migration já aplicada não se edita (crie outra); nome descritivo; sem mudança de modelo sem migration correspondente.
- **Segurança de dados:** tabela nova com **RLS ligado**; coluna sensível identificada; FK com `ON DELETE` pensado; seed sem dado real.
- **Compatível com o código antigo** (expandir/contrair): coluna `NOT NULL` nova precisa de default ou etapa em duas migrations; renomear/remover coluna em uso quebra app e API em campo.
- **Sem lock longo** em tabela grande (`ADD COLUMN ... NOT NULL DEFAULT` volátil, `CREATE INDEX` sem `CONCURRENTLY` onde couber via SQL cru).
- **Modelagem:** tipos explícitos (`timestamptz`, `numeric`, `uuid`), FKs e índices justificados, unicidade garantida no banco, convenção de nomes do projeto no mapeamento.
- SQL cru parametrizado; consulte a skill `supabase-postgres-best-practices`.

### 13. API e compatibilidade — `api`

Mudança que quebra cliente existente (campo removido/renomeado, status alterado, `type`/código de erro mudado) exige migração e aviso, porque versões antigas do app seguem em uso; versão nova de API exige ADR; erro novo no formato `ProblemDetails` (pt-BR) e documentado; status HTTP coerentes (`400` validação, `401`, `403`, `404`, `409`, `429`, `503`); respostas e erros sem vazar existência de conta ou recurso de outro usuário.

### 14. Dependências e build — `deps`

Dependência nova (NuGet ou npm) com problema, alternativa e custo justificados; instalada no projeto consumidor (`.csproj` da camada certa; `mobile/` no npm), sem pacote de infraestrutura entrando em `Domain`/`Application`; licença e manutenção; peso no bundle; versões fora do escopo atualizadas; `package-lock.json` editado à mão; pacote Expo fora da versão compatível com o SDK (use `npx expo install`); resultado de `npm audit` piorado.

### 15. Escopo, PR e documentação — `scope`, `docs`

PR focado (um assunto); refatoração, renomeação ou reformatação **fora da tarefa** (`RULES.md`) vira `suggestion (scope)` pedindo para separar; descrição clara com link para spec/plano/ADR; `.docs/` atualizado quando a realidade mudou (código vence docs, e o doc deve acompanhar na **mesma** mudança); lista de skills/agentes em `.docs/AGENTS.md` e `RULES.md` coerente quando skills ou agentes mudam; artefato gerado, segredo ou `.env` commitado; `TODO` sem dono; plano de rollback para mudança arriscada (migration, config, deploy).

---

## Calibragem de severidade

| Situação | Comentário |
| --- | --- |
| Bug comprovado, perda de dado, falha de segurança (IDOR, segredo no cliente), quebra de contrato sem migração | `issue` |
| Regra "não negociável" dos docs violada (`any`, PII em log, regra de negócio em controller, dependência de camada invertida, input sem label, `console.log`, hex solto no lugar de token) | `issue` |
| Comportamento novo ou bug corrigido **sem teste** onde já existe runner | `issue (tests)` |
| Migration sem `Down`/snapshot ou sem tratamento do caminho de falha de integração | `issue` |
| Melhoria clara, mas o PR funciona sem ela | `suggestion` |
| Dúvida cuja resposta pode mudar a correção | `question (blocking)` |
| Dúvida de curiosidade ou contexto | `question` |
| Preferência pessoal, estilo não coberto pelo lint | `nitpick` |
| Ideia para depois | `thought` |
| Algo bem feito | `praise` |

**Orçamento de atenção:** priorize 1 `issue` bem fundamentado acima de 10 `nitpick`. Se a revisão passa de ~25 comentários, você está comentando demais: junte padrões repetidos e corte o que o autor não faria diferente.
