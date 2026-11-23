# 0016 — Backend: Ratings, Social, Users e Authentication em casos de uso

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** pronta
- **Slug:** backend-ratings-social-users-auth-casos-de-uso
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-19
- **Owner:** Tech lead (a confirmar)
- **Relacionados:** `.plans/arquitetura-design-testes-breakdown.md` (D1–D5, D10–D11); `.adrs/0001-*`, `.adrs/0003-*`, `.adrs/0006-*`, `.adrs/0007-*`, ADR `0012` (proposto, entregue pela `0013`); depende de `0013`, `0014` e `0015` (port único de catálogo, `CatalogDefaults`); fecha o épico de backend e habilita a `0021`; specs retroativas `0001`, `0002`, `0006`, `0007`, `0009`; `.docs/ARCHITECTURE.md` §4.2, §4.4, §4.8 e §6.1.

## 1. Escopo

### Dentro do escopo

- Ratings/Comments: 9 rotas → 9 handlers + 9 casos de uso; worker de moderação chama 1 caso de uso; `RatingsCommentsService` (9 métodos) removido.
- Social: 11 rotas → 11 handlers + 11 casos de uso; `SocialService` (9 métodos) removido e nenhum retorno é `FriendRelationship` (Domain).
- Users: Onboarding (3) e Profile (3) → 6 handlers + 6 casos de uso; `OnboardingService` sem tipo concreto de outro módulo e sem `pt-BR`/provedor fixos.
- Authentication como módulo: `GET /auth/me` com handler e caso de uso (`UseCases/`, `Handlers/`, `Contracts/`).
- Api sem tipos de Domain (`ReviewTargetType`, `FriendRelationshipStatus`) e sem DTO de Application como corpo HTTP (`RatingInput`, `CommentInput`, `SaveOnboardingProgress`, `UpdateUserProfile`).
- Regra × consulta decidida item a item (§4.1), tirando audiência e consentimento de `RatingsCommentsRepository` e `SocialRepository`; fronteiras finais, sem módulo lendo tabela de outro (§4.2), com Ratings e Users no port único de catálogo.
- `ReelixDbContext` organizado por módulo (3 estilos → 1) sem mudar o modelo; término do épico: auditor sem achados `high` e `.docs/ARCHITECTURE.md` §6.1 atualizada.

### Fora do escopo

- Fundação transversal (`0013`), Gamification e Notifications (`0014`), Catalog, Tracking e Lists (`0015`).
- Testes de arquitetura, cobertura e E2E (`0021`–`0023`; o mapa de DbSets daqui vira regra na `0021`); reativar spoilers por IA, denúncias, upload de avatar, exclusão de conta; mudar rota, status, JSON, migrations, schema, mobile.

## 2. Contexto

Hoje estes quatro módulos têm 42 achados do auditor (11 high): `RatingsCommentsService` e `SocialService` com 9 métodos, `RatingsCommentsEndpoints` (346 linhas) e `SocialEndpoints` (366, com 11 `catch`), `Authentication` sem `UseCases/`. `SocialService` devolve `FriendRelationship` e os endpoints importam `Reelix.Domain` (3 arquivos hoje, Tracking sai na `0015`). Os repositórios decidem regra: visibilidade e máscara de spoiler de comentário, nome de exibição, descobribilidade e janelas de consentimento do feed, criação do evento social. Há 19 pares (módulo → DbSet de outro módulo) em 39 referências, por exemplo Social lendo `tracking_records`, `user_ratings` e `user_favorite_titles`, e Gamification lendo `friend_relationships` e `user_profiles`. `ReelixDbContext` (362 linhas) mistura `IEntityTypeConfiguration` (Tracking, Ratings, Catalog), `Apply(ModelBuilder)` (Lists, Notifications, Gamification) e 8 configurações inline. Os `GET /onboarding` e `GET /profile` gravam (`GetOrCreate`) e o app não chama `GET /auth/me`.

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Quem avalia, comenta, socializa e edita perfil; quem mantém regras de privacidade e consentimento |
| Decisão ou ação que muda | Regras de visibilidade e consentimento ficam em um lugar testável sem banco; módulos conversam por ports |
| Resultado esperado       | Mesmo contrato HTTP; backend inteiro no padrão-alvo; privacidade e isolamento entre contas preservados |
| Métrica de sucesso       | Auditor com 0 `high` no backend (29 hoje); pares de leitura entre módulos 19 → 1; `.docs/ARCHITECTURE.md` §6.1 sem as dívidas resolvidas |

## 4. Requisitos

Gates `G1`–`G10` e linha de base em §10.

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | **Batch 0**: matriz rota × teste das 27 rotas e do worker: JSON completo (`authorAvatarKey`, `isOwn`, flags `favoritesShared`/`watchHistoryShared`/`ratingsShared`, "Você", "Usuário Reelix"), entradas de enum nos corpos (`audience` em string de qualquer caixa e em inteiro), erros 400/404/409 (4 títulos sociais), rate limits (`reviews-write` ×5, `social-search` ×1, `social-write` ×5 ⇒ 429 na tentativa N+1), `GET /onboarding` e `GET /profile` com subject novo, pedido de amizade duplicado concorrente, moderação (lote 5, lease 2 min, 3 tentativas, nova tentativa em +1 min) | Must | Verdes no código antigo; matriz anexada; nenhuma rota sem teste de status + JSON |
| R2  | 27 casos de uso + 1 de worker, cada um com **um** `ExecuteAsync(Command\|Query, ct)` e `Command`/`Result` ao lado; 27 handlers 1:1; `RatingsCommentsService`, `SocialService`, `OnboardingService`, `UserProfileService` removidos; `Accept` e `Decline` são casos de uso distintos | Must | G1 exit 0 em Ratings, Social, Users e Authentication |
| R3  | Domain fora dos contratos: `Command`/`Query`/`Result` e DTOs da Api sem tipos de Domain (enums espelhados em Application/Api, mesmo texto JSON); os 4 métodos de `SocialService` que devolviam `FriendRelationship` passam a devolver resultado próprio (`relationshipId`, `status`); corpos HTTP viram DTOs da Api com as mesmas entradas aceitas (nulos opcionais, `bool?` de consentimento = manter) | Must | G2 = 0 (3 arquivos hoje, 2 após a `0015`); JSON dourado idêntico |
| R4  | Regra × consulta conforme §4.1: especificações e políticas no Domain/Application (`CommentVisibility`, `DisplayNamePolicy`, descobribilidade, consentimento do feed, visibilidade do perfil de amigo, validação de `SpoilerClassification`); repositórios só persistem, consultam e traduzem erro de banco | Must | Testes unitários sem `DbContext` cobrem cada política; Given 2 pedidos de amizade simultâneos When concluídos Then 1 relação, 1 evento e 409 na outra chamada; G4 |
| R5  | Fronteiras (§4.2): nenhum repositório referencia DbSet de outro módulo; leituras cruzadas por port do dono (perfil, amizades, atividade de tracking, atividade de avaliações, favoritos compartilhados, estatísticas de exibição, referências e gêneros do catálogo, trava de conta); `UserAccountRepository` passa para `Persistence/Authentication`; atividade do feed combina as fontes ordenadas por `(data, id)` e devolve o mesmo topo. Ratings e Users adotam o port único de catálogo (`0015`): `CatalogTargetExistsAsync` e `CatalogTitlesExistAsync` removidos, `EpisodeBelongsToSeriesAsync` vai ao Catalog | Must | G3: 19 pares → 1 (exceção E1); feed idêntico ao baseline nos cenários de consentimento e de cursor; G5 = 0 com 404 "Conteúdo não encontrado." e 400 "Um ou mais filmes favoritos não estão no catálogo." iguais |
| R6  | Authentication: `GetAuthenticatedAccountUseCase` (renomeado) + handler + DTO; resposta `{id, email, createdAt}` com `email` vindo da claim (não persistido); é o ponto documentado de provisionamento (`Ensure`) | Must | `module-without-usecases-folder` de Authentication = 0; JSON idêntico ao de hoje |
| R7  | Users: `OnboardingService` sem `CatalogGenreService` concreto e sem `"pt-BR"` nem `CatalogProvider.Tmdb` literais (usa a constante da `0015`); validações iguais (etapa válida e diferente de `Completed`, ≤ 20 gêneros, ≤ 10 favoritos, sem repetição, gêneros contra a lista TMDB, favoritos no catálogo) e mensagens iguais; `GET /onboarding` e `GET /profile` sem criar linha (resposta de uma linha nova, medida no Batch 0); `PUT`/`POST` usam `Ensure` | Must | Given subject novo When `GET /onboarding` e `GET /profile` Then respostas do baseline e 0 linhas novas; G6 |
| R8  | Moderação: `OpenAiCommentSpoilerClassifier` traduz `HttpRequestException`, `JsonException` e timeout para um erro de aplicação; o caso de uso do worker o captura e chama `FailModeration` com os mesmos parâmetros; logs sem conteúdo de comentário | Must | Given OpenAI fora do ar When lote Then tentativa registrada e nova tentativa em +1 min como hoje; `HttpRequestException` em `Reelix.Application` = 0 |
| R9  | Endpoints finos: `Results.Unauthorized`, `TryGet*Id`, `catch (` e `Results.Problem` somem dos 4 `*Endpoints.cs`; erros pelo mapeador da `0013` (Social passa a `ToUserMessage()`, única mudança deliberada de contrato, se aceita na `0013`). Contrato intacto: rotas, status, JSON, `WithName`, `Produces*`, rate limits, autorização; sem migration | Must | G7; `RouteSurfaceTests` sem alterar baseline; G8 vazio |
| R10 | `ReelixDbContext`: as 8 configurações inline (`UserAccount`, `UserProfile`, `FriendRelationship`, `SocialEvent`, `UserPreferences`, `UserGenrePreference`, `UserFavoriteTitle`, `ExternalCatalogReference`) viram `IEntityTypeConfiguration<T>` em `Persistence/<Modulo>/` e todas as configurações carregam do mesmo jeito; o modelo e o snapshot não mudam | Must | G8: `ReelixDbContext.cs` ≤ 130 linhas, 0 `modelBuilder.Entity<` inline; `git diff` em `Migrations/` vazio; testes `*PersistenceModelTests` e `*MigrationTests` sem mudança |
| R11 | Término do épico: 0 achados `high` no backend; medium restante só com exceção documentada (`--strict --allow`); `.docs/ARCHITECTURE.md` §4.2 ("Hoje") e §6.1 (itens resolvidos viram "Já está bem feito"), `.docs/DEVELOPMENT.md` §2.1 ("Hoje" removido) e `.docs/QA.md` (contagem de testes) atualizados; ADR `0012` com a tabela §4.2 | Must | G1 e G9 |

### 4.1 Decisão: regra × consulta

| Item (hoje) | Decisão | Justificativa |
| ----------- | ------- | ------------- |
| Visibilidade de comentário (publicado e público, ou do próprio; `RatingsCommentsRepository.cs:187–191`, `237–240`) e máscara de spoiler (`ContainsSpoiler` ou `PossibleSpoiler` ⇒ conteúdo nulo; `:205–215`, `150–163`) | **Regra**: especificação pura no Domain, aplicada pelo repositório como expressão (precisa rodar no SQL por causa da paginação); máscara e autor "Você"/perfil público em Application, sobre a página já carregada (≤ 50 itens) | Decide quem vê o quê; privacidade testável sem banco |
| "Usuário Reelix" (6 cópias: Social 4, Gamification 1, Ratings 1) | **Regra**: `DisplayNamePolicy` única | Mesmo conhecimento duplicado |
| Descobrível: `ProfileIsPublic` ou `IsDiscoverable` (`SocialRepository.cs:23`, `66`, `253`) | **Regra**: especificação no Domain (Users), expressão aplicada pelo repositório | Consentimento de descoberta |
| Janelas de consentimento do feed (`…SharedWithFriends` e `…Since` ≤ data, `:336–338`, `365–368`) e campos expostos do perfil de amigo (`:264–305`) | **Regra**: políticas no Domain/Application; o repositório devolve fatos | Consentimento por categoria |
| Pedido de amizade e evento `friend.requested`/`friend.accepted` (`GetOrCreateRequestAsync`, `notifyOtherParticipant`) | **Regra**: o caso de uso decide o evento (tipo da `0014`); o repositório recebe o evento pronto e grava na **mesma** transação (ADR 0007) | "Quem é notificado" é negócio |
| Validação de `SpoilerClassification` (confiança 0–1, razão ≤ 200) e do lote (1–50) no repositório (`:252`, `297–302`) | **Regra**: o valor valida a si mesmo; limites do lote no caso de uso | Validar entrada não é persistir |
| Upsert `ON CONFLICT`, `ILIKE` com escape, ordenação e cursor, `ExecuteDelete`, claim `SKIP LOCKED`; violação de unicidade/concorrência ⇒ erro (`SocialRepository.cs:96–105`, `136–141`) | **Consulta/persistência**: fica no repositório; a violação vira erro de conflito tipado, com o título da rota | Sem decisão de negócio; concorrência no banco; só o adaptador conhece o erro do banco |

### 4.2 Dependências permitidas entre módulos (alvo)

| Módulo | DbSets próprios | Consome (port do dono) | Expõe |
| ------ | --------------- | ---------------------- | ----- |
| Authentication | `UserAccounts` | — | resolução de conta (`0013`), trava de conta |
| Catalog | `ExternalCatalogReferences`, `CatalogReferenceGenres`, `CatalogEpisodeSeries` | provedores TMDB/OMDb | existência de título, gêneros, episódio↔série, referências por id, fatos de gênero/duração |
| Users | `UserProfiles`, `UserPreferences`, `UserGenrePreferences`, `UserFavoriteTitles` (E2) | Catalog, Authentication | diretório de perfis (nome, avatar, consentimentos), provisionamento de preferências |
| Lists | `UserFavoriteTitles` (E2), `UserWatchlistTitles`, `UserCustomLists`, `UserCustomListTitles` | Catalog, Users | favoritos compartilhados |
| Tracking | `TrackingRecords` | Catalog | episódio rastreado, atividade e estatísticas de exibição |
| Ratings | `UserRatings`, `UserComments` | Catalog, Tracking, Users | atividade de avaliações |
| Social | `FriendRelationships`, `SocialEvents` | Users, Tracking, Ratings, Lists, Catalog | diretório de amizades |
| Notifications | `NotificationPreferences`, `NotificationDevices`, `UserNotifications`, `NotificationDeliveries`, `ProcessedNotificationEvents` | Social (amizades; leitura do outbox `SocialEvents`, exceção **E1**, ADR 0007), Authentication | — |
| Gamification | `AchievementDefinitions`, `UserAchievements`, `GamificationPreferences` | Tracking, Catalog, Social, Users, Authentication | — |

E1: o outbox `social_events` é o contrato publicado de Social para Notifications. E2: `user_favorite_titles` é compartilhada por Users (agregado `UserPreferences`, FK para `user_preferences`) e Lists.

## 5. Fluxos e estados

- **Caminho feliz:** handler lê `ICurrentUser` ⇒ comando ⇒ caso de uso resolve a conta (`Ensure` na escrita, `Find` na leitura) ⇒ ports ⇒ política de domínio ⇒ resultado ⇒ DTO. **Vazio:** sem conta ou sem perfil ⇒ resposta de uma linha nova, sem gravar. **Carregando:** não se aplica.
- **Erro:** 400 (`0013` §5 linhas 1–3), 404 conteúdo ou recurso de outra conta (nunca 403 para privado), 409 social com os 4 títulos, 503/502 de provedor só no onboarding (`CatalogProviderException`). **Sucesso parcial:** falha do classificador de spoilers não derruba o lote; o comentário segue `pending` e tenta de novo.
  **Cancelamento/retry:** `PUT`/`DELETE` idempotentes; pedido de amizade duplicado ⇒ 409 por índice único; o worker repete o lote e mantém `SKIP LOCKED`.

## 6. Acceptance Criteria

- [ ] Given as 27 rotas When comparadas ao JSON, aos status e aos rate limits do Batch 0 Then idênticos (R1, R9).
- [ ] Given comentário público, privado, com spoiler e de outra conta When listado e revelado por dono e por terceiro Then visibilidade, máscara e nome iguais ao baseline (R4).
- [ ] Given amizade aceita e consentimento ligado e depois desligado When feed Then só atividades da janela vigente, na mesma ordem e com o mesmo cursor (R5).
- [ ] Given subject novo When `GET /onboarding` e `GET /profile` Then respostas do baseline e 0 linhas em `user_accounts`, `user_preferences` e `user_profiles` (R7).
- [ ] Given `dotnet build Reelix.sln -c Release`, `dotnet test Reelix.sln -c Release` (≥ 202 testes + os novos) e G1–G10 de §10 Then sem erro e no valor-meta.

## 7. O que fazer

1. Batch 0 (R1): matriz, JSON dourado, rate limits, enum de entrada, concorrência; verdes sobre o código antigo.
2. Domain/Application: políticas e especificações de §4.1 com testes unitários, ports do dono para cada leitura de §4.2; casos de uso e handlers (Ratings → Social → Users → Authentication), DTOs na Api e erros tipados.
3. Infrastructure: repositórios sem regra e sem DbSet alheio; adaptador do classificador com tradução de erro; `UserAccountRepository` para `Persistence/Authentication`; configurações do `ReelixDbContext` por módulo.
4. DI por módulo; script de DbSets por módulo (reutilizado pela `0021`); `.docs` e ADR `0012`; gates; `commit-and-push` pelo `implement-me`.

## 8. O que não fazer

- Não mudar regra de privacidade, consentimento, janela de feed, textos, limites ou cursor; não tocar em `Migrations/` nem no snapshot.
- Não mover regra para o endpoint; não deixar `Reelix.Domain` na Api nem DTO de Application como corpo.
- Não ler tabela de outro módulo em código novo; não injetar caso de uso nem service concreto de outro módulo.
- Não "consertar" produto (ex.: reativar IA de spoilers); não logar conteúdo de comentário, token ou `sub`; não relaxar, pular nem alterar asserção de teste.

## 9. Riscos e dependências

| Tipo | Descrição | Mitigação |
| ---- | --------- | --------- |
| Dependência | `0013`, `0014` e `0015` concluídas; Docker para Postgres | Ordem de execução do mapa; implementar em série |
| Risco | Privacidade: regressão em visibilidade de comentário, consentimento do feed ou campos do perfil de amigo; o feed por fontes separadas muda ordem, desempate ou topo | Testes com dois e três usuários, janela de consentimento e cursor no Batch 0; 404 (não 403) para privado; cada fonte devolve `limit` + 1 ordenado por `(data, id)` e a mescla usa o mesmo desempate |
| Risco | Contrato HTTP: enums de entrada (string em qualquer caixa e inteiro), propriedades `init`, nulos opcionais mudam ao trocar o DTO | JSON dourado e testes de entrada; mesmo `JsonStringEnumConverter` |
| Risco | Concorrência: pedido duplicado, evento do outbox gravado fora da transação, `SKIP LOCKED` da moderação | Evento passado ao repositório e salvo no mesmo `SaveChanges`; testes concorrentes |
| Risco | Migrations: configurações movidas alteram o snapshot; o auditor ignora trechos depois de `$"""` | G8 e testes de modelo, nenhuma migration nova; gates por `grep` e pelo script de DbSets |

## 10. Resultado

Linha de base de 2026-11-19 (`audit-slices.mjs --scope backend`: 105 achados, 29 high; Ratings 13 achados, Social 12, Users 16, Authentication 1; os `high` de Gamification, Notifications, Catalog, Tracking e Lists saem nas `0014`/`0015`):

| Gate | Verificação | Hoje | Meta |
| ---- | ----------- | ---- | ---- |
| G1 | `node .claude/skills/refactor-slices/scripts/audit-slices.mjs --scope backend --module <Ratings\|Social\|Users\|Authentication> --strict` | 13, 12, 16 e 1 (exit 1) | exit 0 nos quatro |
| G2 | `grep -rln "Reelix.Domain" backend/Reelix.Api --include=*.cs` | 3 (Ratings, Social, Tracking) | 0 |
| G3 | Script de DbSets por módulo (mapa de §4.2): pares módulo → DbSet de outro módulo e referências | 19 pares, 39 referências (`user_accounts` contada na pasta Users) | 1 par (E1), 1 referência |
| G4 | `grep -rn "Usuário Reelix" backend --include=*.cs --exclude-dir=UnitTests --exclude-dir=obj --exclude-dir=bin` | 6 linhas (Social 4, Gamification 1, Ratings 1) | 1 definição (`DisplayNamePolicy`) |
| G5 | `grep -rnE "CatalogTargetExistsAsync\|CatalogTitlesExistAsync" backend --include=*.cs --exclude-dir=obj --exclude-dir=bin` | 11 linhas | 0 |
| G6 | `grep -rnE "\"pt-BR\"\|CatalogGenreService" backend/Reelix.Application/Users --include=*.cs` · `grep -rln HttpRequestException backend/Reelix.Application/Ratings` | 2 · 1 arquivo | 0 · 0 |
| G7 | 4 `*Endpoints.cs` (Ratings, Social, Onboarding, Profile): linhas · `Results.Unauthorized` · `TryGet*Id` · `catch (` · `Results.Problem`; e `grep -rnE 'Find(First\|FirstValue)\("sub"\)' backend/Reelix.Api` | 346/366/92/85 · 25 · 4 · 19 · 18; 13 (antes da `0013`) | ≤150 cada · 0 · 0 · 0 · 0; 1 |
| G8 | `ReelixDbContext.cs` (linhas · `modelBuilder.Entity<` inline) e `git diff --exit-code -- backend/Reelix.Infrastructure/Persistence/Migrations` | 362 · 8; — | ≤130 · 0; vazio |
| G9 | Achados `high` do backend (script abaixo) e `--strict --allow <regras documentadas>` | 29 `high` de 105 | 0 `high`; restante só com exceção documentada |
| G10 | `grep -rln "using Reelix.Domain" backend/Reelix.Application/Ratings backend/Reelix.Application/Social backend/Reelix.Application/Users backend/Reelix.Application/Authentication --include=*Command.cs --include=*Query.cs --include=*Result.cs` e `accounts.GetOrCreateAsync` nesses módulos | n/a (ainda não há `Command`/`Result`) · 19 chamadas | 0 · 0 |

Achados `high`: `node .claude/skills/refactor-slices/scripts/audit-slices.mjs --scope backend --json | node -e "const f=JSON.parse(require('fs').readFileSync(0,'utf8')).findings;console.log(f.filter(x=>x.severity==='high').length)"` deve imprimir `0`.

- **Comando/verificação:** `dotnet build Reelix.sln -c Release`; `dotnet test Reelix.sln -c Release` (Docker ativo); G1–G10.
- **Evidência esperada:** saídas no relatório do `implement-me` com a matriz rota × teste, o antes/depois dos gates e o diff de `.docs` e ADR.
- **Comportamento observável:** igual ao atual; novidade verificável: `GET /onboarding` e `GET /profile` não gravam, e nenhum repositório lê tabela alheia.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| Pergunta: confirmar `user_favorite_titles` como exceção E2 compartilhada (premissa) ou atribuí-la a Lists e fazer o onboarding de Users gravar favoritos por port? | Agregado `UserPreferences` e FK; sem efeito no contrato | Não | Tech lead |
| Pergunta: o ADR `0012` passa a `aceito` ao fim da `0016`? | Registro da decisão | Não | Tech lead |
| Premissa: E1 (leitura do outbox `social_events` por Notifications) é a única exceção a "não ler tabela alheia", por port e ADR 0007 | Fronteira Social/Notifications | Não | Tech lead |
| Premissa: a trava de conta (`SELECT … FOR UPDATE` em `user_accounts`) vira port do Authentication com SQL idêntico; `GET /onboarding` e `GET /profile` sem linha devolvem o valor de uma linha nova (confirmado no Batch 0) | Concorrência de Gamification; contrato de leitura | Não | Plano/Tech lead |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação em modo lote; linha de base do auditor, de `grep` e de DbSets por módulo (19 pares, 39 referências) | Épico arquitetura-design-testes, spec 4 de 4 do backend |
