# 0013 — Backend: fundação transversal

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** concluída
- **Slug:** backend-fundacao-transversal
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-23
- **Owner:** Tech lead (a confirmar)
- **Relacionados:** `.plans/arquitetura-design-testes-breakdown.md` (D1–D5, D10–D11, Q1, Q5); `.adrs/0001-*` (complementa); ADR novo `.adrs/0012-use-case-por-fluxo-handler-por-endpoint-e-erros-de-aplicacao.md` (proposto, entregue por esta spec; número provisório, renumerado ao mesclar); specs vizinhas `0014`, `0015`, `0016` (migram os fluxos) e `0021` (testes de arquitetura); `.docs/ARCHITECTURE.md` §4.2 e §6.1; `.docs/DEVELOPMENT.md` §2.1, §5, §9, §13; `.claude/skills/refactor-slices/references/target-architecture.md`.

## 1. Escopo

### Dentro do escopo

- Relógio: `TimeProvider` registrado e injetado em Application/Infrastructure; o Domain recebe o instante por parâmetro (`DateTimeOffset now`); os pontos de chamada existentes passam `clock.GetUtcNow()`.
- Identidade: leitura única do `sub` (`ICurrentUser` ou extensão de `ClaimsPrincipal`) e um port de conta com passos explícitos `Ensure` (cria, escrita) e `Find` (não cria, leitura).
- Erros: hierarquia de erros de aplicação tipados (exceções, premissa D4/Q1); os tipos de erro existentes passam a herdá-la; **um** mapeamento para `ProblemDetails` em pt-BR; ADR proposto.
- Paginação (`PageRequest` + políticas nomeadas) e cursor opaco (um codec) em helpers únicos.
- Padrão de DTO de request/response na Api: convenção de pasta, mapeamento e helper de teste de identidade de JSON (piloto: `GET /auth/me`).
- DI por módulo no mesmo padrão e nível de pasta; opções TMDB/OMDb com `ValidateOnStart`.
- `Program.cs` enxuto: registro único das 4 políticas de rate limit e `GET /auth/me` fora do Program.
- `CatalogGenreService` sem cache `static`.
- Testes de caracterização da superfície de rotas e dos contratos de erro (§5) **antes** de mover código.

### Fora do escopo

- Migrar fluxos para caso de uso + handler e remover `try/catch`, `TryGet*Id` e `Results.Unauthorized` dos endpoints: Gamification e Notifications (`0014`); Catalog, Tracking e Lists (`0015`); Ratings, Social, Users e Authentication, incluindo o `GET /auth/me` como use case (`0016`).
- Regra de negócio em repositórios, fronteiras de tabela e organização do `ReelixDbContext` (`0014`–`0016`).
- Testes de arquitetura, cobertura, host de teste E2E e URLs de provedor configuráveis (`0021`/`0023`; Q5 do mapa); mudar rota, status, JSON, OpenAPI, schema, migrations, pacotes NuGet ou mobile (D1).

## 2. Contexto

O auditor mecânico (2026-11-19) acusa 105 achados no backend (29 high). Vários são transversais e se repetem em todos os módulos: 57 usos de relógio global (41 no Domain), a leitura do `sub` em 13 pontos da Api, quatro variantes de `ExecuteAsync` e `try/catch` inline por endpoint, `ValidatePage` em três cópias com limites divergentes, cursor base64 em quatro pontos, DI com nomes de classe repetidos e um `Program.cs` de 244 linhas. As specs `0014`–`0016` só conseguem mover fluxos para o padrão-alvo se esta base existir. O app mobile **não** chama `GET /auth/me` (`fetchAuthenticatedAccount` não tem uso em `mobile/src`): a conta nasce na primeira chamada que a resolve, e hoje isso inclui `GET`.

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Quem evolui o backend (pessoas e agentes) e, indiretamente, o usuário final, que não deve perceber mudança |
| Decisão ou ação que muda | Cada fluxo novo ou movido passa a usar a mesma fundação (tempo, identidade, erro, paginação, DI) em vez de reimplementá-la |
| Resultado esperado       | Regras temporais determinísticas nos testes, erro HTTP em um só lugar, identidade lida uma vez, contrato HTTP idêntico |
| Métrica de sucesso       | Itens transversais da linha de base (§10) zerados ou no valor-meta, 202+ testes verdes e superfície de rotas inalterada |

## 4. Requisitos

Gates `G1`–`G10` e a linha de base estão em §10.

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | `TimeProvider` (`TimeProvider.System`) registrado e injetado; Domain recebe `now` em fábricas e métodos (parâmetro obrigatório, sem default); Infrastructure sem relógio direto. Testes usam um relógio fixo escrito à mão (sem pacote novo) | Must | Given a solução compilada When roda G1 Then 0 linhas; Given relógio fixo When `UserNotification.Create`/`FriendRelationship.Create` Then o instante gravado é o recebido |
| R2  | Leitura única do `sub`: ausente, não-Guid ou `Guid.Empty` ⇒ 401 igual ao de hoje; a chave de partição do rate limiter usa o mesmo componente; os 8 `TryGet*Id` passam a delegar a ele sem mudar fluxos | Must | Given token sem `sub` válido When rota protegida Then 401 com o mesmo corpo de `Results.Unauthorized()` (linha 11 de §5); G2 = 1 |
| R3  | Port de conta com `Ensure` (cria; mantém o tratamento da violação do índice único `IX_user_accounts_supabase_user_id`) e `Find` (null se não existe). `GET /auth/me` usa `Ensure` e é o ponto documentado de provisionamento; os módulos adotam nas specs `0014`–`0016` | Must | Given subject sem conta When `Find` Then null e 0 linhas novas em `user_accounts`; When 8 `Ensure` concorrentes (Postgres) Then 1 linha e mesmo `Id` para todos |
| R4  | Hierarquia de erros de aplicação (validação 400, não encontrado 404, conflito 409, dependência externa com código/`RetryAfter`, não autenticado 401). `CatalogProviderException`, `TrackingContentNotFoundException`, `TrackingSeriesMismatchException`, `CatalogTitleNotFoundException`, `CatalogTargetNotFoundException` e `NotificationDeviceConflictException` herdam dela sem mudar mensagem. **Um** mapeador (`IExceptionHandler` ou filtro de endpoint, decidido no ADR) gera `ProblemDetails` pt-BR | Must | Given rota sintética de teste que lança cada tipo When chamada Then (status, title, detail, type, `code`, `Retry-After`) são os das linhas 1, 3–7, 9 e 10 de §5 (9 e 10 por tipos novos de conflito e de indisponibilidade que `0014`/`0016` passam a lançar no lugar de `InvalidOperationException`); erro esperado (4xx) não gera log em nível Error; erro inesperado ⇒ 500 (linha 13) sem stack nem mensagem interna |
| R5  | O mapeador aplica `ToUserMessage()` a toda `ArgumentException`; título por rota continua o atual (metadado da rota ou carregado pelo erro, decisão do ADR) | Must | Given `ArgumentException` com `ParamName` When mapeada Then `detail` sem o sufixo ` (Parameter '…')` |
| R6  | `PageRequest.Validate(page, pageSize, política)` com políticas nomeadas que **preservam** limites e textos: Lists (1–1.000.000, 1–100), Ratings (1–100.000, 1–50), Tracking (1–100.000, 1–100, `ArgumentOutOfRangeException` por parâmetro) e Catalog (página 1–100) | Must | Given cada política When valores 0, 1, máximo, máximo+1 Then mesmas aceitações e **mesmo texto** que o código antigo (baseline escrito antes); G3 = 0 |
| R7  | `CursorCodec` base64url sem padding, limite de 200 caracteres, com os dois formatos de hoje: JSON (`SocialCursor`, `NotificationCursor`) e texto `"{contagem}:{guid}"` (ranking de Gamification), byte a byte; erros com as mesmas mensagens por módulo | Must | Given cursores gerados pelo código antigo (golden strings) When decodificados e recodificados Then idênticos; G4 = 0 fora do codec |
| R8  | Padrão de DTO: `Api/<Modulo>/Contracts/<Fluxo>Request.cs`/`Response.cs` + mapeamento explícito; helper de teste serializa com as `JsonOptions` do host (camelCase, enums em string camelCase) e compara com JSON dourado; piloto `AuthenticatedAccountResponse` | Must | Given `AuthenticatedAccount` de exemplo When serializado pelo piloto Then `{"id","email","createdAt"}` idêntico ao de hoje |
| R9  | DI: `Add<Modulo>Application()` e `Add<Modulo>Infrastructure()` por módulo, `<Modulo>Module.cs` no mesmo nível (Application: `<Modulo>/`; Infrastructure: raiz do projeto); `AddApplication`/`AddInfrastructure` só agregam. Opções TMDB/OMDb com `.Validate(...).ValidateOnStart()` que rejeita só valor presente com caractere de controle ou espaço nas pontas; ausente/vazio segue válido (503 em runtime, como hoje) | Must | Given host sem credencial de catálogo When inicia Then sobe e o catálogo responde 503; Given token com `\n` When inicia Then falha na partida; G5 sem nome de classe repetido |
| R10 | `Program.cs` ≤ 120 linhas: 4 políticas de rate limit por **um** registro dirigido por tabela (`reviews-write` 10/min, `social-search` 20/min, `social-write` 8/min, `notifications-write` 8/min; partição `sub`→IP→`unknown`; sem fila); `GET /auth/me` em `Api/Authentication/AuthenticationEndpoints.cs` com o mesmo `WithName`, `WithTags`, `RequireAuthorization` | Must | Given rota sintética por política When N+1 chamadas no mesmo minuto Then a última é 429 com title/detail da linha 12 de §5; `Program.cs` sem `MapGet("/api/v1/auth/me"` |
| R11 | Cache de gêneros sem `static` mutável: TTL 6 h, chave provedor/tipo/idioma, relógio injetado; Application não ganha pacote de cache (o cache vive em Infrastructure ou atrás de port) | Must | Given relógio fixo When 2 chamadas em 6 h Then 1 chamada ao provedor; When +6 h e 1 s Then nova chamada; dois hosts não compartilham cache; G6 = 0 |
| R12 | Superfície preservada: teste lê `EndpointDataSource` e fixa (método, template, `EndpointName`, exige autorização, política de rate limit) das 70 rotas de produto (rotas só de `Development` ficam fora); `GET /auth/me` fixa-se com `WithName`/`WithTags` | Must | Given baseline escrito sobre o código antigo When roda depois de cada passo Then idêntico; G9 verde |
| R13 | ADR `.adrs/0012-…` (proposto): use case por fluxo + handler 1:1, exceções tipadas (alternativa `Result<T>` e por quê não), `TimeProvider`, `ICurrentUser`, `Ensure`/`Find`, DTO na Api, DI, origem do título por rota, escolha entre `IExceptionHandler` e filtro. `.docs/ARCHITECTURE.md` §4.2/§6.1 e `.docs/DEVELOPMENT.md` §2.1 atualizados | Must | Given o PR When revisado Then ADR existe com template de `RULES.md` §5 e §6.1 marca itens 4 e 10 resolvidos e 3 e 9 parcialmente |

## 5. Fluxos e estados

Refatoração sem UI: estados vazio e carregando não se aplicam. **Caminho feliz:** chamada autenticada ⇒ `ICurrentUser` ⇒ (fluxo atual, ainda no service) ⇒ resposta idêntica. **Sucesso parcial/cancelamento:** não se aplicam. **Erro: contrato preservado** (observado em `*Endpoints.cs` em 2026-11-19; módulos migram para o mapeador em `0014`–`0016`, cada uma fixando suas linhas por fluxo **antes** de mover):

| # | Origem hoje | Rotas | Status | title (pt-BR) | detail / extras |
| - | ----------- | ----- | ------ | ------------- | --------------- |
| 1 | `ArgumentException` (`ToUserMessage`) | Catalog, Lists, Ratings, Tracking, Onboarding, Profile | 400 | "Solicitação de catálogo inválida." · "Dados da lista inválidos." · "Solicitação inválida." · "Solicitação de acompanhamento inválida." · "Preferências de onboarding inválidas." · "Perfil inválido." | mensagem sem sufixo `(Parameter …)` |
| 2 | `ArgumentException` (`Message` cru) | Social, Gamification, Notifications | 400 | "Busca inválida." · "Solicitação inválida." · "Paginação inválida." · "Cursor inválido." · "Período inválido." · "Consulta de ranking inválida." · "Registro de dispositivo inválido." | `Message` cru, vaza ` (Parameter '…')` quando há `ParamName`. **Única mudança deliberada**: passa a `ToUserMessage()` quando o módulo migrar |
| 3 | Parâmetro inválido sem exceção | Catalog, Tracking, Ratings, Social | 400 | "Provedor ou tipo de conteúdo inválido." · "Parâmetro de descoberta inválido." · "Parâmetro inválido." · "Tipo de conteúdo inválido." · "Direção inválida." | textos fixos de cada handler |
| 4 | `CatalogProviderException` | Catalog (`ExecuteAsync`) | `StatusCode` do erro | = mensagem | `type` `https://reelix.local/problems/{code}`; extensão `code`; `Retry-After` (s, arredondado para cima) se > 0; sem `detail` |
| 5 | `CatalogProviderException` | Catalog `genres`, Onboarding | `StatusCode` | = mensagem | `type` igual; **sem** `code` e **sem** `Retry-After` |
| 6 | `CatalogProviderException` | Tracking | `StatusCode` | "Não foi possível consultar o catálogo." | detail "Tente novamente mais tarde."; sem `type`/`code`/header |
| 7 | Não encontrado tipado | Tracking · Ratings · Lists | 404 | "Conteúdo não encontrado." · "Conteúdo não encontrado." · "Título não encontrado." | detail fixo "O conteúdo não está disponível no catálogo." (Tracking) · mensagem da exceção (demais). Lista ausente: "Lista não encontrada." / "A lista não existe ou não está disponível para esta conta." |
| 8 | `Results.NotFound()` | detalhes/temporadas do catálogo, `KeyNotFound` em Social/Gamification, recursos de outra conta | 404 | corpo gerado por `UseStatusCodePages()` + `AddProblemDetails()` (confirmar no Batch 0) | — |
| 9 | Conflito | Tracking · Notifications · Lists reorder · Social | 409 | "Episódio incompatível." · "Dispositivo já associado." · "A lista foi alterada." · "A solicitação não pode ser enviada/respondida/cancelada." e "A amizade não pode ser removida." (`InvalidOperationException`) | detail: "O episódio não pertence à série informada." · mensagem · "Os títulos da lista mudaram desde a última consulta. Atualize os dados e tente novamente." · mensagem |
| 10 | `InvalidOperationException` em `RegisterDevice` | Notifications | 503 | "Notificações push indisponíveis." | mensagem ("O registro push está desabilitado…"). Hoje **qualquer** `InvalidOperationException` da rota vira 503 |
| 11 | `Results.Unauthorized()` sem `sub` válido (63 pontos) | todas as rotas autenticadas | 401 | corpo de `UseStatusCodePages()`; JWT ausente/inválido é 401 do middleware (`WWW-Authenticate`) | — |
| 12 | Rate limiter (`OnRejected`) | rotas com política | 429 | "Limite de solicitações excedido." | "Aguarde um minuto antes de tentar novamente." (`application/problem+json`) |
| 13 | Exceção não tratada | todas | 500 | `UseExceptionHandler()` + `AddProblemDetails()` | `ProblemDetails` padrão, sem stack |

## 6. Acceptance Criteria

- [ ] Given o baseline de rotas escrito antes When roda `RouteSurfaceTests` depois de cada passo Then idêntico (R12).
- [ ] Given rota sintética que lança cada erro When chamada Then as linhas 1, 3–7, 9, 10 e 13 de §5 são reproduzidas e a linha 2 usa `ToUserMessage()`; as linhas 11 e 12 seguem em R2 e R10 (R4, R5).
- [ ] Given limites de página e cursores dourados When validados/codificados Then iguais ao código antigo (R6, R7).
- [ ] Given conta inexistente When `Find` Then null e nenhuma linha criada; `Ensure` concorrente cria uma linha (R3).
- [ ] Given a solução When G1–G10 de §10 Then todos no valor-meta.
- [ ] Given `dotnet build Reelix.sln -c Release` e `dotnet test Reelix.sln -c Release` Then sem erro e com ≥ 202 testes verdes.

## 7. O que fazer

1. Batch 0 (verde no código antigo): `RouteSurfaceTests`, baselines de limite de página e cursores dourados, caracterização das linhas de §5 alcançáveis por HTTP com fakes (Catalog 4–6, 401, 429) ou Postgres.
2. Relógio: registrar `TimeProvider`, mudar fábricas do Domain, ajustar chamadas e testes (compilador guia).
3. `ICurrentUser`, port de conta (`Ensure`/`Find`) e adaptação dos 8 `TryGet*Id`.
4. Hierarquia de erros, mapeador, rota sintética de teste e ADR.
5. `PageRequest` e `CursorCodec`; trocar as 3 cópias e os 4 pontos de base64.
6. Padrão de DTO, helper de teste e piloto `GET /auth/me`; mover a rota para `AuthenticationEndpoints`.
7. DI por módulo; opções TMDB/OMDb; `Program.cs` com registro único de rate limit; cache de gêneros; atualizar `.docs`; gates de §10; `commit-and-push` pelo `implement-me`.

## 8. O que não fazer

- Não migrar fluxos nem remover `try/catch` dos endpoints (é das specs `0014`–`0016`).
- Não unificar os limites numéricos de paginação nem os títulos de erro por rota.
- Não adicionar pacote NuGet (nem `TimeProvider.Testing`, nem NetArchTest); não editar migrations; não relaxar nem pular teste.
- Não exigir credencial de catálogo na partida; não logar `sub`, token ou corpo.
- Não tornar `GET` escritor novo; não aceitar `ownerId` do cliente.

## 9. Riscos e dependências

| Tipo | Descrição | Mitigação |
| ---- | --------- | --------- |
| Risco | Contrato HTTP: o mapeador unificado muda título, detail ou cabeçalho de algum erro | Tabela §5 como teste; só a linha 2 muda, de forma documentada |
| Risco | `IExceptionHandler` registra 4xx esperado como erro (log antes do handler); ruído e vazamento | Preferir filtro de endpoint ou testar G10 de log; ADR decide |
| Risco | Mudar assinatura do Domain quebra 22 arquivos de teste que o referenciam | Parâmetro obrigatório; só entra o argumento `now`, sem mudar asserção |
| Risco | Concorrência: `Ensure` duplicar conta | Manter índice único e tratamento atual; teste com 8 chamadas |
| Risco | Migrations: nenhuma permitida; mudança acidental de modelo | G8 (`git diff` em `Migrations/` vazio) |
| Risco | O auditor não vê regiões depois de `$"""` (não detectou `UtcNow` em `NotificationsRepository.cs:305,326`) | Gates por `grep` complementar (G1) |
| Dependência | Docker ativo para os testes Postgres; `0014`–`0016` dependem desta spec | Teste de conta sobe Testcontainers como nos existentes |

## 10. Resultado

Linha de base medida em 2026-11-19 (grep na raiz, Git Bash; `audit-slices.mjs --scope backend`: 105 achados, 29 high):

| Gate | Verificação | Hoje | Meta |
| ---- | ----------- | ---- | ---- |
| G1 | `grep -rnE "(DateTime\|DateTimeOffset)\.(UtcNow\|Now)\|DateTime\.Today" backend/Reelix.Domain backend/Reelix.Application backend/Reelix.Infrastructure --include=*.cs --exclude-dir=Migrations --exclude-dir=obj --exclude-dir=bin` | 57 (41 Domain, 9 Application, 7 Infrastructure; o auditor vê 55) | 0 |
| G2 | `grep -rnE 'Find(First\|FirstValue)\("sub"\)' backend/Reelix.Api --include=*.cs` | 13 | 1 |
| G3 | `grep -rnE "static void ValidatePage" backend --include=*.cs` | 3 | 0 |
| G4 | `grep -rnE "(To\|From)Base64String" backend/Reelix.Application backend/Reelix.Infrastructure --include=*.cs --exclude-dir=Migrations` | 6 | 0 fora de `CursorCodec` |
| G5 | `grep -rhoE "static class \w*(DependencyInjection\|Module)\b" backend --include=*.cs \| sort \| uniq -d` | 3 nomes repetidos; 8 classes de DI | 0 repetidos |
| G6 | `grep -rnE "static readonly ConcurrentDictionary" backend/Reelix.Application --include=*.cs` | 1 | 0 |
| G7 | `Program.cs`: linhas; `AddPolicy` de rate limit; `ValidateOnStart` em `backend/` | 244; 4 cópias; 2 de 4 opções | ≤ 120; 1 registro; 4 de 4 |
| G8 | `git diff --exit-code -- backend/Reelix.Infrastructure/Persistence/Migrations` e `git diff -- "*.csproj"` sem mudança de `PackageReference` | — | vazio |
| G9 | `RouteSurfaceTests` e `ErrorMappingTests` (nomes sugeridos) | não existem | verdes |
| G10 | Teste com logger em memória: 400 por validação | — | nenhum log em nível Error |

- **Comando/verificação:** `dotnet build Reelix.sln -c Release`; `dotnet test Reelix.sln -c Release` (Docker ativo); `node .claude/skills/refactor-slices/scripts/audit-slices.mjs --scope backend` (os achados `direct-clock-access` de Domain, Application e Infrastructure caem a 0; os demais seguem até `0014`–`0016`, portanto `--strict` ainda falha aqui); G1–G10.
- **Evidência esperada:** saídas dos comandos no relatório do `implement-me`, com tabela antes/depois; ADR e `.docs` no diff.
- **Comportamento observável:** nenhum. Mesmas rotas, status e JSON; a única diferença possível é o texto do erro 400 de Social/Gamification/Notifications sem o sufixo `(Parameter …)`, e só quando o módulo migrar.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| **Decidido em 2026-11-19: sim, remover o sufixo.** Pergunta: aceitar remover o sufixo ` (Parameter '…')` do `detail` de Social/Gamification/Notifications (única mudança de contrato)? Default: sim, pois `DEVELOPMENT.md` §13.2 já proíbe o vazamento | Texto de erro 400 nesses módulos | Não | Tech lead |
| **Decidido em 2026-11-19: não unificar; preservar por política.** Pergunta: unificar os limites de paginação (1.000.000/100.000; 100/50) é desejado? Default: não; preservar por política | Contrato de `page`/`pageSize` | Não | Produto/Tech lead |
| Premissa: exceções tipadas, não `Result<T>` (Q1 do mapa); o ADR registra a alternativa | Forma dos casos de uso | Não | Tech lead |
| Premissa: origem do título por rota (metadado × erro), `IExceptionHandler` × filtro e forma de expor `ICurrentUser` são decisões do plano/ADR | Implementação, não contrato | Não | Plano |
| Premissa: `ValidateOnStart` valida só forma; credencial ausente não impede a partida | E2E e testes sobem sem credencial | Não | Tech lead |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação em modo lote (`breakdown-specs`); linha de base medida por grep e auditor; tabela de erros levantada em `*Endpoints.cs` | Épico arquitetura-design-testes, spec 1 de 4 do backend |
| 2026-11-23 | Implementada (modo orquestrado). Premissas registradas: (1) mapeador por **filtro de endpoint** e título por metadado de rota ou carregado pelo erro (ADR `0012`); (2) a linha 12 da tabela de §5 tem hoje `Content-Type: application/json` (o `WriteAsJsonAsync` substitui o `problem+json`), preservado como está e fixado por teste; (3) a linha 8 foi confirmada: o 404 de `Results.NotFound()` sai como `application/problem+json` com `title` "Not Found"; (4) as classes de DI se chamam `<Modulo>ApplicationModule`/`<Modulo>InfrastructureModule` para G5; (5) `GetOrCreateAsync` fica como alias de `EnsureAsync` até `0014` a `0016`; (6) `TmdbCatalogProvider`, `OmdbCatalogProvider` e `ExpoPushClient` recebem `TimeProvider?` opcional (padrão `TimeProvider.System`, resolvido pelo DI) para não quebrar os testes de provedor; (7) a leitura do `sub` se chama `ReadSubjectId` (o auditor trata nomes `TryGet*Id` como o helper duplicado) | Execução com `implement-me`; sem alteração de escopo ou de contrato |
