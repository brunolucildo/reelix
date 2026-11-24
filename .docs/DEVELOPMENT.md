# Desenvolvimento

> **Público:** qualquer pessoa ou agente que altere código.
> Aplique estas regras em toda mudança. Elas complementam `ARCHITECTURE.md` (estrutura) e `QA.md` (validação).
> **Verificado contra o código em:** 2026-11-19. Onde o código atual ainda não cumpre uma regra, a seção diz **"Hoje"**; a regra vale para todo código novo e para o código tocado.

## 1. Princípios

| Princípio              | Como aplicar                                                                                                                    |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| **Clean Architecture** | Dependências apontam para dentro: `Api → Application → Domain`; `Infrastructure` implementa ports. Domain não conhece framework, banco nem HTTP. |
| **SOLID**              | Responsabilidades pequenas, interfaces estreitas (ISP), dependência de abstrações (ports) onde há variação ou necessidade de teste. Sem interface para toda classe. |
| **KISS**               | A solução mais simples que atende ao requisito. Sem abstração preventiva, sem microsserviços, sem camada mecânica.              |
| **DRY**                | Não duplique **conhecimento**: regra de negócio, validação, contrato e configuração vivem em um só lugar.                       |
| **WET (com critério)** | Duas ocorrências concretas podem ficar separadas até o conceito ficar claro. Abstração errada custa mais que duplicação.       |
| **DDD pragmático**     | Linguagem do produto (tracking, progresso, watchlist, avaliação, consentimento); invariantes no domínio; agregados pequenos; eventos de domínio só com necessidade concreta. |
| **Clean Code**         | Nomes que revelam intenção, funções curtas, comentários só para o "porquê", sem código morto.                                    |
| **Segurança por design** | Negar por padrão, autorizar cada recurso no servidor, validar nas bordas, nunca logar segredo.                                |

> **DRY e WET não se contradizem:** DRY vale para conhecimento; WET é tolerado para estrutura acidental.

## 2. Arquitetura de código

### 2.1 Backend (`backend/`)

Quatro projetos, uma solução (`Reelix.sln`), um monólito modular:

| Camada (projeto)         | Contém                                                              | Não contém                                  |
| ------------------------ | ------------------------------------------------------------------- | ------------------------------------------- |
| `Reelix.Domain`          | Entidades, value objects, invariantes, serviços de domínio puros    | Framework, EF, HTTP, SDKs, relógio do sistema |
| `Reelix.Application`     | Casos de uso, ports (interfaces), comandos/resultados, erros de aplicação | EF Core, `HttpClient`, `HttpStatusCode`, detalhes de transporte |
| `Reelix.Infrastructure`  | EF Core/Npgsql, repositórios, configurations, adaptadores de provedores, migrations | Regra de negócio                      |
| `Reelix.Api`             | Rotas (minimal API), handlers, DTOs de request/response, DI/composition root, workers finos | Regra de negócio, acesso direto ao banco |

**Alvo (vale para código novo e para o que for tocado; `ARCHITECTURE.md` §4.2):**

```text
backend/Reelix.Application/<Modulo>/
  UseCases/<Fluxo>/<Fluxo>UseCase.cs     # uma classe, um fluxo, um ExecuteAsync
  UseCases/<Fluxo>/<Fluxo>Command.cs     # ou Query / Result, ao lado do caso de uso
  Ports/                                 # interfaces pequenas por necessidade (ISP)
  Errors/                                # erros de aplicação tipados
backend/Reelix.Api/<Modulo>/
  <Modulo>Endpoints.cs                   # só MapGroup/rotas e metadados OpenAPI
  Handlers/<Fluxo>Handler.cs             # um handler por endpoint, injeta exatamente um caso de uso
  Contracts/                             # DTOs de request e response da API
backend/Reelix.Infrastructure/
  Persistence/<Modulo>/                  # repositórios e EF configurations do módulo
  <Provedor>/                            # adaptadores (Catalog, Notifications, Ratings...)
```

**Hoje:** um `*Service` por módulo com vários métodos, `*Contracts.cs` com DTOs, ports e exceções misturados e endpoints que injetam o service direto. A fundação transversal já existe (spec `0013`): `TimeProvider`, `ICurrentUser`, `FindAsync`/`EnsureAsync`, erros tipados com mapeador único (`WithApplicationProblems`), `PageRequest`/`PagePolicy`, `CursorCodec`, DI por módulo e rate limit por tabela; use-os em vez de recriá-los. Ao tocar um fluxo, prefira movê-lo para o alvo em vez de acrescentar um método ao service gordo. A refatoração completa é conduzida por specs.

Regras:

- **Um endpoint, um handler, um caso de uso** (alvo). Dois endpoints com o mesmo comportamento compartilham um serviço de capacidade, não um caso de uso.
- **Nenhuma regra de negócio em repositório, endpoint ou adaptador.** Avaliação de conquistas, decisão de eventos sociais e regras de visibilidade vivem em casos de uso/serviços de domínio, testáveis sem banco. (Hoje há exceções em `GamificationRepository` e `NotificationsRepository`: não replique.)
- **Tempo:** injete `TimeProvider` (nunca `DateTime.UtcNow`/`DateTimeOffset.UtcNow` em Domain/Application) e passe o instante ao domínio. Não há mais uso direto de `UtcNow` em Domain, Application nem Infrastructure; testes usam um relógio fixo escrito à mão.
- **Fronteiras de módulo:** um módulo não lê tabelas de outro nem injeta o service concreto de outro. Exponha um port estreito no módulo dono (ex.: "título existe no catálogo" no Catalog).
- **Ports com o nome da necessidade**, não do fornecedor (`IPushSender`, não `IExpoPushClient`); Application não conhece `HttpStatusCode` nem `HttpRequestException`: o adaptador traduz erros do provedor em erros de aplicação.
- **Contrato HTTP na Api:** DTOs de request/response ficam em `Reelix.Api`; tipos de `Reelix.Domain` não aparecem em contratos HTTP nem em assinaturas de endpoints.
- **Identidade:** leia o usuário autenticado por **uma** abstração (ex.: `ICurrentUser`/extensão de `ClaimsPrincipal`); não duplique `TryGetUserId` (os existentes delegam a `CurrentUserExtensions` até as specs `0014` a `0016`). O dono vem do `sub` validado, jamais do corpo ou da rota.
- **GET não escreve.** Criar conta/perfil/preferências preguiçosamente deve ser um passo explícito, não efeito colateral de uma consulta.
- **Concorrência** se resolve no banco (índice único, `ON CONFLICT`/tratamento de violação), nunca com "consulta e depois insere".
- Todo método de I/O recebe e propaga `CancellationToken`.

### 2.2 Módulos e fatias verticais

- Organize por módulo de produto (Catalog, Tracking, Lists, Ratings, Social, Notifications, Gamification, Users, Authentication), nunca por tipo técnico global.
- Compartilhamento entre módulos só quando usado por 2+ módulos e estável (ex.: paginação, cursor, `ICurrentUser`), em um lugar identificado.
- DI por módulo: um método de extensão `AddXxx` por módulo e por camada, no mesmo padrão e nível de pasta (hoje há inconsistência; não crie classes de DI com o mesmo nome em namespaces diferentes).

### 2.3 Mobile (`mobile/`)

Expo Router em `mobile/src/app` (cada arquivo é uma rota; `_layout.tsx` define navegadores). Código não-rota fica **fora** de `src/app`.

**Estrutura (spec `0018` aplicada; `ARCHITECTURE.md` §4.3):**

```text
mobile/src/
  app/                    # rotas finas: compõem componentes e hooks da feature
  features/<modulo>/      # api/ (chamadas + schemas zod), hooks/ (queries e mutations), components/, query-keys.ts, schemas.ts (formulários)
  components/ui/          # primitivos reutilizáveis (PrimaryButton, IconAction, ModalDialog, ConfirmDialog, ToggleRow, Chip, SmallButton, Pager...)
  components/navigation/  # AppHeader, UserDrawer, BackButton (compartilhados; consomem hooks das features)
  lib/                    # http-client único, ApiError, formatação pt-BR, paginação, stale-time
  providers/              # auth, toast
  theme/                  # tokens
```

**Hoje:** `lib/http-client` único com `ApiError` e 401 global (spec `0017`) e, por feature, `api/`, `hooks/`, `components/` e `query-keys.ts` (spec `0018`): account, auth, catalog, gamification, home, lists, notifications, onboarding, profile, ratings, social e tracking. `catalog` é a base e não importa de outras features; parâmetros de rota do catálogo vêm de `features/catalog/route-params`. Código novo de dados nasce em `features/<modulo>/api`, chama `httpRequest`, usa a fábrica de chaves do módulo e é exposto por um hook em `features/<modulo>/hooks`; nunca `fetch` direto, `queryKey` literal, `staleTime` numérico, `Intl`/`toLocale*` em tela nem texto técnico ("HTTP n") para o usuário. Rota e componente nunca chamam `useQuery`/`useMutation`/`useQueryClient` nem importam valores de `api/` (só `import type`). Regras:

- **Telas são composição.** Dados e mutations vivem em hooks da feature; formulário e validação em hook/schema; a tela só liga as peças e trata estados (carregando, vazio, erro).
- **Um cliente HTTP** (`lib/http-client`): base da API, Bearer do Supabase, parse de `ProblemDetails`, erro tipado `ApiError { status, code?, title, detail }`, validação da resposta com zod. 401 tratado **globalmente** (limpar sessão/cache e pedir novo login), não tela a tela.
- **Query keys por fábrica** por feature (modelo: `trackingQueryKeys`); o mesmo recurso nunca aparece sob chaves diferentes; invalidação por prefixo da fábrica.
- **Estado do servidor** em TanStack Query; estado de UI efêmero em `useState`; nunca duplique dado do servidor em store global sem motivo.
- **Formulários** com React Hook Form + zod; erros por campo inline.
- Guardas de rota ficam em `Stack.Protected` (`_layout.tsx`); telas não repetem `Redirect` de sessão.
- Primitivos de UI em `components/ui`; nada de botão/toggle/chip/pager reimplementado por tela.

## 3. TypeScript

- `strict: true` já está no `tsconfig` (herda `expo/tsconfig.base`); não relaxe. Use o alias `@/*` → `src/*` e `@/assets/*`.
- Proibido `any`. Use `unknown` e estreite o tipo; proibido `@ts-ignore` sem justificativa.
- Prefira uniões discriminadas a flags booleanas; `import type` para tipos.
- Evite `as` e `!`; quando inevitável, comente a invariante.
- Valide dados externos na fronteira (resposta da API com zod, variáveis `EXPO_PUBLIC_*`, deep links, storage).
- Tipos da API são inferidos dos schemas zod (`z.infer`); não escreva um tipo paralelo.

## 4. React Native e Expo

- **Antes de tocar em API de Expo/EAS/React Native**, confira a versão do `expo` no `package.json` e a doc versionada (`https://docs.expo.dev/versions/v58.0.0/`); não responda de memória. Veja `mobile/AGENTS.md`.
- Instale módulos Expo com `npx expo install <pacote>` (resolve versões do SDK); prefira módulos Expo recomendados a bibliotecas de terceiros.
- Se `mobile/ios` e `mobile/android` não existirem, são gerados (CNG): configure em `app.json` e config plugins, nunca à mão. Módulos nativos exigem **development build** (`eas build --profile development`); Expo Go só tem os módulos embutidos.
- `Pressable` para toques; `FlatList`/`SectionList` para listas longas (não `ScrollView` + `map`); `expo-image` para imagens remotas (com `alt`/`accessibilityLabel` quando informativa).
- Animações: use `react-native-reanimated` quando precisar de desempenho (hoje o app usa `Animated` com `useNativeDriver: false`).
- Sessão e tokens: `expo-secure-store` em iOS/Android; no web o fallback é `localStorage` e **não** é fronteira de segurança.
- Nunca use `Alert.alert` para confirmar ações destrutivas (é no-op no React Native Web); use `ModalDialog`.
- Variáveis `EXPO_PUBLIC_*` são públicas; nunca coloque segredo nelas.

## 5. API (ASP.NET Core)

- Handler fino: lê o usuário (`ICurrentUser`), converte o DTO de request em comando, chama **um** caso de uso, converte o resultado em DTO de response.
- Valide a entrada na borda (tipo, tamanho, intervalo, enums) antes do caso de uso; rejeite com 400 e `ProblemDetails`.
- Erros de aplicação são mapeados para HTTP **em um único lugar** (ex.: `IExceptionHandler` ou um mapper de resultados); não faça `try/catch` por endpoint. Não vaze stack trace nem detalhes internos.
- Toda integração externa tem timeout, retry limitado, erro tipado e não derruba a requisição inteira por falha parcial.
- **A API é fechada por padrão:** endpoints de produto exigem `RequireAuthorization()`; só rotas explicitamente públicas ficam abertas. Hoje `/swagger`, `/openapi/*` e `/api/v1/dev/auth/config` existem **só em `Development`**.
- Limite de requisições: políticas do `AddRateLimiter` por usuário (`sub`; sem `sub`, por IP), janela fixa de 1 minuto, aplicadas por rota com `.RequireRateLimiting("<politica>")`. Políticas atuais: `reviews-write` (10/min), `social-search` (20/min), `social-write` (8/min), `notifications-write` (8/min). Resposta 429 em `ProblemDetails` (pt-BR). Crie política nova para escrita ou busca abusável.
- Use paginação, filtro e limites em toda coleção; valide `page`/`pageSize` em um único lugar.
- Operações repetíveis devem ser idempotentes (`PUT`/`DELETE` repetidos não duplicam nem falham).
- Documente cada endpoint no OpenAPI (nome, resumo em inglês, `Produces`).

## 6. Dados (PostgreSQL e EF Core)

- Migrations versionadas pelo EF (`backend/Reelix.Infrastructure/Persistence/Migrations`); schema nunca é alterado "na mão". Gere com `dotnet tool run dotnet-ef migrations add <Nome> --project ... --startup-project ...`, revise o SQL e mantenha o `Down`.
- Migration aplicada em algum banco **não se edita**: crie outra. O CI gera o script idempotente como artefato; aplicar é decisão manual (nunca em ambiente compartilhado sem autorização).
- Nomes em `snake_case` no banco (convenção das configurations existentes); tipos explícitos; FKs, unicidade e `CHECK` justificados; timestamps em UTC.
- Acesso a dados só por repositórios na `Infrastructure`; LINQ/EF parametrizado — nunca concatene entrada em SQL (`FromSqlInterpolated` ou parâmetros quando precisar de SQL).
- Evite N+1 e carregar coleções sem paginação; projete só as colunas necessárias; crie índice a partir de padrão de consulta real e confira o plano.
- RLS está habilitado nas tabelas e a Data API do Supabase não deve ter acesso; **a API é o único cliente**, com connection string própria. Tabela nova: habilite RLS e revogue privilégios dos papéis da Data API na mesma migration. Consulte `supabase-postgres-best-practices`.
- Identificadores do provedor (TMDB/OMDb) ficam ao lado de IDs internos; nunca use o ID do provedor como identidade interna única.
- Dados pessoais: minimize coleta, não logue PII, defina retenção (a política de exclusão/exportação é decisão pendente — REELIX-008).

## 7. Erros, logs e observabilidade

- Use `ILogger<T>` com templates estruturados (`"Falha ao ... {Operation}"`); logs e mensagens de código em inglês. Nunca `Console.WriteLine` em código de produto.
- Não logue token, senha, `Authorization`, connection string, corpo de requisição inteiro, e-mail ou texto de comentário.
- Falha de provedor (catálogo, push, IA) não derruba a operação inteira: registre, isole e devolva erro tipado ou estado parcial honesto (nunca totais "completos" calculados de dados parciais).
- Mobile: erros de API/rede em toast (`useErrorToast`); nunca `console.log` de dados do usuário em produção.
- Observabilidade (métricas, traces, alertas, crash reporting) **não está implantada** (REELIX-016); ao implementá-la, evite labels com `userId` ou IDs de títulos e nunca inclua tokens ou dados pessoais.

## 8. Acessibilidade e responsividade

- Alvo de toque mínimo de 44×44 px (`dimensions.minimumTouchTarget`); `accessibilityRole` e `accessibilityLabel` em controles apenas-ícone e toggles; `accessibilityState` para selecionado/desabilitado/carregando; estado nunca só por cor.
- Foco visível em campos (borda `brandSecondary`); mensagens de erro em texto e anunciadas (`accessibilityLiveRegion`).
- Contraste mínimo WCAG AA (4,5:1 texto normal); texto escalável (não trave tamanhos com altura fixa).
- Layout validado em telefone pequeno e grande e, no Expo web, em larguras de 375, 768 e 1280. Orientação hoje travada em retrato.
- Siga `DESIGN.md` e `DESIGN_SYSTEM.md`.

## 9. Testes

Abordagem TDD quando fizer sentido: escreva o teste do comportamento antes ou junto da implementação (skill `tdd`).

| Nível                     | Ferramenta                              | Comando                                           | Onde vive                                    | Estado            |
| ------------------------- | --------------------------------------- | ------------------------------------------------- | -------------------------------------------- | ----------------- |
| Unitário e integração .NET | xUnit (+ `Mvc.Testing`, Testcontainers) | `dotnet test Reelix.sln --configuration Release`  | `backend/Reelix.UnitTests`                   | Existe (339)      |
| Arquitetura .NET          | xUnit (análise de dependências)         | `dotnet test`                                     | projeto de testes de arquitetura             | **Futuro**        |
| Unitário/componente mobile | Jest (`jest-expo`) + RNTL               | `npm test` (em `mobile/`)                         | `mobile/src/**/*.test.ts(x)`                 | **Futuro**        |
| E2E                       | Playwright sobre o Expo web             | `npm run test:e2e`                                | pasta de E2E a definir pela spec             | **Futuro**        |

Regras:

- Teste comportamento observável, não implementação; nomes que descrevam cenário e resultado (`Sujeito_Comportamento`).
- Não faça teste depender de dado real, rede externa, hora real ou ordem de execução. Controle o tempo com `TimeProvider` (substituível).
- Bug corrigido ganha teste de regressão.
- Autorização e isolamento entre contas têm teste de **acesso negado** com dois usuários.
- Testes com PostgreSQL real usam Testcontainers (`postgres:17-alpine`), aplicam as migrations e usam dados fictícios únicos; nunca apontam para o banco Supabase configurado nos User Secrets.
- Autenticação nos testes de API usa JWT assinado por chave RSA em memória (padrão das factories existentes); nunca um token real.
- Ao criar um novo tipo de teste, crie o script real, adicione o gate ao CI e atualize `QA.md` e `TESTING.md`. Não invente comandos.

## 10. Dependências

- NuGet: adicione ao projeto consumidor (nunca ao `Domain`); mantenha o `restore` com NU1903/NU1904 como erro. npm: apenas em `mobile/`, com `npx expo install` para módulos Expo.
- Justifique a dependência no relatório final: problema, alternativa considerada e custo.
- Prefira a biblioteca já presente no stack antes de adicionar outra; verifique licença e manutenção.
- Não atualize versões fora do escopo da tarefa; não force `npm audit fix --force` (REELIX-019).

## 11. Definição de pronto

Uma mudança só está pronta quando:

- [ ] O comportamento solicitado funciona e o escopo não foi extrapolado.
- [ ] `dotnet build Reelix.sln -c Release` e `dotnet test Reelix.sln -c Release` passam (quando a mudança toca o backend).
- [ ] `npm run lint` e `npx tsc --noEmit` passam em `mobile/` (quando a mudança toca o mobile).
- [ ] Testes foram adicionados/atualizados quando há comportamento novo (e os gates futuros de `QA.md` §3, assim que existirem).
- [ ] Autorização por recurso e validação de entrada foram verificadas em endpoint novo.
- [ ] Acessibilidade e estados vazio/carregando/erro foram verificados em UI; wireframe atualizado.
- [ ] Contrato HTTP público preservado, ou cliente mobile, spec e docs atualizados na mesma mudança.
- [ ] Nenhum segredo, PII ou artefato gerado foi commitado.
- [ ] Plano e ADR foram registrados quando aplicável.
- [ ] `.docs` foi atualizado se a realidade mudou.
- [ ] O relatório final lista comandos realmente executados.

## 12. Anti-padrões proibidos

- Regra de negócio em endpoint, repositório, tela ou componente.
- `try/catch` de mapeamento de erro dentro de cada endpoint; `exception.Message` cru devolvido ao cliente.
- `DateTime.UtcNow` em Domain/Application; `static` mutável (ex.: cache estático em serviço).
- Tipo de `Reelix.Domain` em contrato HTTP; DTO de Application usado como corpo de request.
- Repositório lendo tabela de outro módulo; adaptador dependendo de service concreto de outro módulo.
- `GET` com efeito colateral de escrita.
- Chamada de API ou `useQuery`/`useMutation` direto na tela; `queryKey` inline sem fábrica; novo cliente HTTP.
- Hex de cor, tamanho de fonte ou espaçamento numérico espalhado em telas (use tokens).
- Erro de API renderizado inline (use toast); validação de campo em toast.
- `any`, `@ts-ignore` e `eslint-disable` sem justificativa.
- Copiar componente para "mudar só uma coisinha".
- Deixar `Console.WriteLine`, `console.log`, código morto ou dado fictício simulando módulo real em produção.
- Afirmar que um teste passou sem executá-lo.

## 13. Convenções de API e idioma

Valem para todo código novo.

### 13.1 Espaço de URLs

| Zona                         | Prefixo       | Acesso                                                   | Onde vive                                             |
| ---------------------------- | ------------- | -------------------------------------------------------- | ----------------------------------------------------- |
| API                          | `/api/v1/*`   | Autenticada (Bearer JWT do Supabase), salvo `Development` | `backend/Reelix.Api` (`*Endpoints.cs`)                |
| Ferramentas de desenvolvimento | `/swagger`, `/openapi/v1.json`, `/dev-auth.html`, `/api/v1/dev/auth/config` | Só `Development`            | `Program.cs`, `wwwroot/`                              |
| Rotas do app                 | `reelix://…` e rotas de `mobile/src/app` | `Stack.Protected` por sessão; públicas: entrada, cadastro, recuperação, callback | `mobile/src/app/_layout.tsx` |

- Versione a API em `/api/v1`; uma `v2` exige ADR. Recursos em substantivos, no plural quando coleção; IDs de recurso como `{id:guid}`.
- Deep link de autenticação: `reelix://auth/callback`. Destinos de notificação aceitos: `/social` e `/notifications` (allowlist no domínio e no cliente).

### 13.2 Contratos da API

- JSON em `camelCase`; datas em ISO 8601 UTC; paginação por `page`/`pageSize` (ou cursor opaco em feeds), sempre com limites validados.
- Todo erro é `ProblemDetails` (`application/problem+json`) com `status`, `title` e `detail`; **`title` e `detail` são exibidos ao usuário, então ficam em pt-BR** e não revelam chaves de configuração, IDs internos nem stack trace. Use `ArgumentException.ToUserMessage()` (`backend/Reelix.Api/ArgumentExceptionExtensions.cs`) para remover o sufixo `(Parameter '…')` — hoje nem todos os módulos o aplicam (dívida).
- Códigos: 400 validação, 401 sem sessão válida, 403 sem permissão, 404 inexistente **ou privado**, 409 conflito, 429 limite excedido, 502/503/504 indisponibilidade de provedor ou configuração ausente.
- CORS: só as origens de `Cors:AllowedOrigins` (em `Development`, `localhost`/`127.0.0.1` nas portas `8081` e `8087`); sem curingas. O app nativo não depende de CORS.
- O cliente mobile valida a resposta com zod; toda mudança de contrato atualiza o schema do cliente na mesma mudança.

### 13.3 Idioma

- **pt-BR:** copy de interface do app, `title`/`detail` de `ProblemDetails`, `.docs/`, specs, planos, ADRs e mensagens de commit.
- **Inglês:** identificadores, comentários, mensagens de log/exceção internas e resumos de OpenAPI.
- Não há biblioteca de i18n; adotar uma (vários idiomas) exige decisão e ADR.

### 13.4 Autenticação e sessão

- Cabeçalho `Authorization: Bearer <jwt>` do Supabase. A API valida assinatura (JWKS ou segredo legado), emissor (`<Supabase:Url>/auth/v1`), audiência (`authenticated`) e validade (tolerância de 30 s). O `sub` é lido sem mapeamento de claims (`MapInboundClaims = false`).
- Resolva a conta com `IUserAccountRepository.GetOrCreateAsync(sub)`; o ID interno ≠ `sub`. Dados de outras contas privadas respondem 404.
- Mobile: a sessão é gerenciada por `@supabase/supabase-js` (PKCE) com `expo-secure-store`; `auth-provider.tsx` expõe a sessão e o `signOut` (que revoga o dispositivo de push, limpa o cache do React Query e a sessão local). `Stack.Protected` fecha as rotas privadas.
- Mensagens de erro de autenticação não revelam se um e-mail tem conta (`auth-errors.ts`).
