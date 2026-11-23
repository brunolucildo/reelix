# 0015 — Backend: Catalog, Tracking e Lists em casos de uso

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** pronta
- **Slug:** backend-catalog-tracking-lists-casos-de-uso
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-19
- **Owner:** Tech lead (a confirmar)
- **Relacionados:** `.plans/arquitetura-design-testes-breakdown.md` (D1–D5, D10–D11); `.adrs/0001-*`, `.adrs/0005-*`, ADR `0012` (proposto, entregue pela `0013`); depende da `0013`; vizinhas `0014` (Gamification/Notifications) e `0016` (Ratings, Social, Users, Authentication e fronteiras finais); specs retroativas `0003`, `0004`, `0005`, `0008`; `.docs/ARCHITECTURE.md` §4.5, §4.7 e §6.1 (itens 2, 5, 7, 8 e 9).

## 1. Escopo

### Dentro do escopo

- Catalog: 6 rotas → 6 handlers + 6 casos de uso (busca, descoberta, detalhe, temporada, temporadas, gêneros), no lugar de `CatalogService` (311 linhas) e `CatalogGenreService`.
- Tracking: 10 rotas → 10 handlers + 10 casos de uso, no lugar de `TrackingService` (10 métodos públicos).
- Lists: 15 rotas (`/favorites`, `/watchlist`, `/title-status`, `/lists`) → 15 handlers + 15 casos de uso, no lugar de `UserListsService`.
- `*Endpoints.cs` (316, 408 e 442 linhas) só mapeiam rotas; `CatalogContracts.cs` (217), `TrackingContracts.cs` (137) e `ListsContracts.cs` (149) separados em comando/resultado, ports e erros.
- **Um** port "o título existe no catálogo" exposto pelo Catalog, adotado por Tracking e Lists (as 4 variantes de hoje: Tracking, Lists, Ratings e Users).
- `TrackingCatalog` (Infrastructure) deixa de depender do `CatalogService` concreto; Tracking e Lists não leem `catalog_external_references` direto.
- `CatalogProviderException` sem `StatusCode`/`HttpStatusCode` na Application; adaptadores TMDB/OMDb traduzem; o status HTTP sai de um mapa único na Api.
- `IUserListsRepository` (16 operações) e `ITrackingRepository` (9) segregados em ports de até 5 operações.
- Resolução de conta explícita (`Ensure` na escrita, `Find` na leitura): 16 `accounts.GetOrCreateAsync` em Tracking e Lists.
- Regra de "episódio disponível" e progresso com `TimeProvider`, em serviço puro; provedores TMDB (496 linhas) e OMDb (417) divididos sem mudar comportamento.
- Teste de caracterização de rotas e JSON **antes** de mover.

### Fora do escopo

- Fundação transversal (`0013`); Gamification e Notifications (`0014`).
- Ratings e Users passam a usar o port único do catálogo na `0016`; as outras 2 variantes (`CatalogTargetExistsAsync`, `CatalogTitlesExistAsync`) sobram até lá.
- Onboarding (`0016`): aqui só troca o tipo injetado de `CatalogGenreService` para o port de gêneros do Catalog, sem mexer no fluxo.
- Tabelas de outros módulos lidas por Social e Gamification, organização do `ReelixDbContext` (`0016`).
- Mudar chamadas aos provedores (URLs, parâmetros, ordem, cache de 10 min da descoberta e de 6 h dos gêneros), regras de progresso, rotas, JSON, migrations, mobile.

## 2. Contexto

`CatalogService`, `TrackingService` e `UserListsService` concentram todos os fluxos dos módulos; cada endpoint repete a leitura do `sub`, o `try/catch` e o mapeamento (`ExecuteAsync` em 3 variantes, 10 `catch`, 25 `Results.Unauthorized`). `CatalogProviderException(code, message, int statusCode, retryAfter)` carrega o status HTTP até a Api e `CatalogService`/`CatalogGenreService` importam `System.Net`. `TrackingCatalog` (`Persistence/Tracking/TrackingCatalog.cs`) injeta `CatalogService` e fixa `"pt-BR"`. "O título existe" tem 4 implementações, cada uma lendo `catalog_external_references`. Os `GET` criam conta pelo `GetOrCreateAsync`. `UserListsService.AddFavoriteAsync` também força `IUserPreferencesRepository.GetOrCreateAsync`, porque `user_favorite_titles` tem FK para `user_preferences` (módulo Users). `GetAvailableEpisodes` e o filtro de temporadas usam `DateTime.UtcNow` (2 pontos). A cobertura HTTP é parcial: 47 casos in-memory (maioria só 401) e 10 cenários Postgres, com 1 de listas e 2 de tracking.

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Usuário que busca, acompanha e organiza títulos; quem mantém esses módulos |
| Decisão ou ação que muda | Cada fluxo vira uma unidade testável e alterável sem tocar nos vizinhos; a existência de título é decidida por um componente do Catalog |
| Resultado esperado       | Mesmas rotas, status e JSON; `GET` sem criar conta; Application sem detalhes de HTTP |
| Métrica de sucesso       | Auditor sem achados em `Catalog`, `Tracking` e `Lists` (34 hoje, 13 high); 4 variantes de existência → 2; testes de rota fixam o contrato |

## 4. Requisitos

Gates `G1`–`G10` e linha de base em §10.

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | **Batch 0**: matriz rota × teste das 31 rotas (JSON de sucesso, 400, 401, 404, 409, erros de provedor com `type`/`code`/`Retry-After`) e testes para cada lacuna: Catalog por HTTP in-memory com provedor falso; Tracking e Lists por HTTP + Postgres. Inclui `GET` com subject **sem conta** (status e JSON de hoje) | Must | Verdes no código antigo; matriz anexada ao relatório; nenhuma das 31 rotas sem teste de status + JSON |
| R2  | 31 casos de uso (6 + 10 + 15), cada um com **um** `ExecuteAsync(Command\|Query, ct)` e `Command`/`Result` ao lado; 31 handlers 1:1; `CatalogService`, `CatalogGenreService`, `TrackingService` e `UserListsService` removidos (capacidade compartilhada, como resolver ids de referência e gravar metadados, vai para serviço de capacidade) | Must | G1 exit 0 nos 3 módulos; nenhum `*Service` com mais de 5 métodos públicos |
| R3  | Contracts separados: `contracts-mixed` (14 records + 4 ports + 1 exceção; 6 + 3 + 2; 8 + 1 + 1) ⇒ `Ports/`, `Errors/` e tipos por fluxo | Must | G1; cada arquivo ≤ 400 linhas |
| R4  | Port único no Catalog (`ExistsAsync(id, tipo)` e `AllExistAsync(ids, tipo)`, semântica de hoje: `content_type` em minúsculas) usado por Tracking e Lists; a verificação pós-violação de FK em `UserListsRepository` (3 pontos) usa o mesmo componente | Must | Given título inexistente When `PUT /favorites/{id}` Then 404 "Título não encontrado."; G3: variantes de Tracking e Lists = 0 |
| R5  | Tracking não depende de `CatalogService`: um port do Catalog entrega temporadas e temporada por id interno da série (404 quando a série não tem id externo); idioma e provedor padrão (`pt-BR`, TMDB) em constante nomeada única, reutilizada pela `0016` | Must | Given série sem id externo When progresso Then 404 como hoje; G4 = 0 |
| R6  | `CatalogProviderException` vira erro de aplicação **sem** status HTTP (código, `Kind`, `RetryAfter`); os adaptadores TMDB/OMDb o criam sem `HttpStatusCode`; a Api mapeia código → status: `catalog_provider_timeout` 504; `_unavailable` 503; `_rate_limited` 429 (+ `Retry-After`); `_configuration_error`, `_not_configured` 503; `_bad_response`, `_invalid_response` 502; `catalog_title_not_found` 404; `catalog_invalid_external_id`, `catalog_provider_not_supported`, `catalog_discovery_not_supported` 400 | Must | Given cada código When rota do Catalog Then status, `title`, `type`, `code` e `Retry-After` iguais às linhas 4–6 da tabela da `0013` §5 (Tracking mantém o formato genérico); G2 = 0 |
| R7  | Ports de até 5 operações: `IUserListsRepository` (16) ⇒ favoritos, watchlist, listas, itens de lista e status por título; `ITrackingRepository` (9) ⇒ leitura e escrita; `CatalogReferenceExistsAsync`, `CatalogTitleExistsAsync` e `GetSeriesExternalIdAsync` saem deles. `UserListsRepository` (412) ≤ 400 linhas | Must | Given `interface I…` desses módulos When contadas as operações Then ≤ 5 |
| R8  | Conta explícita: escrita (`PUT`/`POST`/`DELETE`, `title-status` é leitura) usa `Ensure`; leitura usa `Find` e, sem conta, devolve o que uma conta nova devolve (página vazia, `false`, zeros) sem criar linha; `AddFavorite` garante `user_preferences` por port estreito exposto pela Users, como passo explícito da escrita | Must | Given subject novo When `GET` de histórico, séries, progresso, favoritos, watchlist, listas e `POST /title-status` Then 200 igual ao baseline e 0 linhas em `user_accounts`; When `PUT /favorites` Then conta e preferências criadas; G7, G8 |
| R9  | "Episódio disponível" (`releaseDate` nula ou ≤ hoje UTC; temporadas com número ≥ 0 e data nula ou ≤ hoje; `DistinctBy` por id) e progresso (`Round(assistidos·100/total, 2)`, nulos sem inventário) em serviço puro que recebe `now` (`TimeProvider` da `0013`) | Must | Given relógio fixo When episódio com data = hoje, amanhã e nula Then disponível, indisponível e disponível; temporada negativa excluída; sem inventário ⇒ totais e percentual nulos |
| R10 | Endpoints finos: `Results.Unauthorized`, `TryGet*Id`, `catch (` e `Results.Problem` somem dos 3 `*Endpoints.cs`; `ExecuteAsync` helpers removidos; erros vão ao mapeador da `0013` | Must | G5; `RouteSurfaceTests` verde |
| R11 | DTOs HTTP na Api: corpos `CreateCustomList`/`UpdateCustomList` (hoje de Application) e respostas viram tipos da Api; `TrackingItemType` e demais tipos de Domain fora da Api e de `Command`/`Query`/`Result`; entradas de `type`, `sort` e `provider` aceitas como hoje (qualquer caixa) | Must | G6 = 0; JSON dourado idêntico (enums em string camelCase, `watchedAt` ISO 8601 UTC) |
| R12 | `TmdbCatalogProvider` (496) e `OmdbCatalogProvider` (417) ≤ 400 linhas por arquivo, por responsabilidade (HTTP, mapeamento, cache), mesmas URLs, parâmetros, ordem de chamadas e cache | Should | `CatalogProviderTests` passam sem mudar asserção; G1 sem `file-too-long` |
| R13 | Contrato intacto: rotas, `WithName`, `Produces*`, tags, `RequireAuthorization`, valor padrão `language = "pt-BR"` das 6 rotas do Catalog; sem migration | Must | `RouteSurfaceTests` sem alterar baseline; G9 vazio |

## 5. Fluxos e estados

- **Caminho feliz:** handler lê `ICurrentUser` ⇒ comando ⇒ caso de uso ⇒ ports ⇒ resultado ⇒ DTO. Ex.: `PUT /tracking/movies/{id}` verifica o título no Catalog, resolve a conta (`Ensure`), grava (idempotente, corrige `watchedAt`), responde 200.
- **Vazio:** `Find` sem conta ⇒ página vazia, `false` ou totais zerados; série sem inventário ⇒ totais e percentual nulos. **Carregando:** não se aplica.
- **Erro:** 400 (linhas 1 e 3 da `0013` §5), 404 conteúdo/título/lista, 409 "Episódio incompatível." e "A lista foi alterada.", erros de provedor (linhas 4–6). **Sucesso parcial:** o progresso nunca devolve totais "completos" calculados com inventário parcial. **Cancelamento/retry:** `PUT`/`DELETE` repetidos seguem idempotentes; `CancellationToken` propagado (Tracking hoje usa `context.RequestAborted`).

## 6. Acceptance Criteria

- [ ] Given as 31 rotas When comparadas ao JSON e aos status do Batch 0 Then idênticos, inclusive `Retry-After` e `code` do Catalog (R1, R6, R13).
- [ ] Given 8 `PUT /tracking/movies/{id}` simultâneos When concluídos Then 200 em todos e 1 registro por conta e título (concorrência preservada).
- [ ] Given título inexistente When `PUT /favorites`, `PUT /watchlist` e `PUT /lists/{id}/titles/{titleId}` Then 404 "Título não encontrado." (R4).
- [ ] Given subject sem conta When `GET` de leitura Then resposta de conta nova e nenhuma linha em `user_accounts` (R8).
- [ ] Given código `catalog_provider_rate_limited` com `retryAfter` de 30 s When `GET /catalog/search` Then 429 com `Retry-After: 30` (R6).
- [ ] Given relógio fixo When progresso da série Then respeita a regra de episódio disponível (R9).
- [ ] Given `dotnet build Reelix.sln -c Release`, `dotnet test Reelix.sln -c Release` e G1–G10 Then sem erro e no valor-meta.

## 7. O que fazer

1. Batch 0 (R1): matriz, testes de lacuna e JSON dourado, verdes sobre o código antigo.
2. Catalog: erros sem HTTP, port de existência, port de gêneros e de temporadas, serviço de capacidade para referências, 6 casos de uso e handlers; ajustar o tipo injetado em `OnboardingService`.
3. Tracking e Lists: serviço puro de disponibilidade e progresso, ports segregados, 25 casos de uso e handlers, port de preferências da Users para `AddFavorite`.
4. Infrastructure: dividir repositórios e provedores; `TrackingCatalog` passa a implementar o port do Catalog sem `CatalogService`.
5. Api: DTOs, handlers, `*Endpoints.cs` finos, mapa código → status; migrar os testes de serviço existentes (`CatalogServiceTests`, `TrackingServiceTests`, `UserListsServiceTests`) para os casos de uso sem mudar asserção.
6. DI por módulo; atualizar `.docs/ARCHITECTURE.md` §4.5, §4.7 e §6.1; gates; `commit-and-push` pelo `implement-me`.

## 8. O que não fazer

- Não mudar chamadas aos provedores, TTLs de cache, limites de paginação nem textos de erro.
- Não deixar `HttpStatusCode` nem `System.Net` na Application; não expor `ExternalCatalogReference` nem `DbContext` fora da persistência do Catalog.
- Não injetar caso de uso ou service concreto de outro módulo; não tocar em `Migrations/`.
- Não migrar Ratings, Users, Social nem Gamification (outras specs).
- Não relaxar, pular nem alterar asserção de teste; não criar conta em `GET`.

## 9. Riscos e dependências

| Tipo | Descrição | Mitigação |
| ---- | --------- | --------- |
| Dependência | `0013` (relógio, `ICurrentUser`, conta `Ensure`/`Find`, mapeador, `PageRequest`, DTO); `0016` conclui as variantes de existência de Ratings e Users | Não iniciar antes da `0013`; estado transitório descrito em §1 |
| Risco | Contrato HTTP: 4 formatos de erro de provedor (completo, `genres`/Onboarding, Tracking genérico) e detail fixo de `TrackingContentNotFoundException` ("O conteúdo não está disponível no catálogo.") | Testes por linha da tabela da `0013` §5 antes de mover |
| Risco | Concorrência: `Ensure` de conta, `PUT` de tracking e FK de favoritos (violação tratada no banco, com reverificação) | Manter índices e tratamento de violação; teste de 8 `PUT` simultâneos |
| Risco | `Find` sem conta diverge do `GetOrCreate` em algum `GET` (mobile não chama `/auth/me`; a conta nasce na primeira escrita) | Batch 0 mede a resposta de hoje para subject novo e a nova assertiva exige igualdade |
| Risco | Migrations: nenhuma permitida | G9 |
| Risco | O progresso chama o provedor por temporada em sequência; dividir mal altera custo ou ordem | Preservar a ordem e comparar requisições com `HttpMessageHandler` falso |
| Risco | O auditor ignora trechos depois de `$"""` | Gates por `grep` (G2, G3, G7) |

## 10. Resultado

Linha de base de 2026-11-19 (`audit-slices.mjs --scope backend`: Catalog 6 high + 8 medium; Tracking 3 + 8; Lists 4 + 5):

| Gate | Verificação | Hoje | Meta |
| ---- | ----------- | ---- | ---- |
| G1 | `node .claude/skills/refactor-slices/scripts/audit-slices.mjs --scope backend --module <Catalog\|Tracking\|Lists> --strict` | 14, 11 e 9 achados (exit 1) | exit 0 nos três |
| G2 | `grep -rnE "HttpStatusCode\|using System.Net;" backend/Reelix.Application --include=*.cs` | 5 (2 arquivos do Catalog) | 0 |
| G3 | `grep -rnE "CatalogReferenceExistsAsync\|CatalogTitleExistsAsync" backend --include=*.cs --exclude-dir=obj --exclude-dir=bin` (código de produção e testes) | 15 (13 em produção, 2 em testes) | 0; sobram só as 2 variantes de Ratings e Users (`CatalogTargetExistsAsync`, `CatalogTitlesExistAsync`) |
| G4 | `grep -rn "CatalogService" backend/Reelix.Infrastructure --include=*.cs` | 1 | 0 |
| G5 | 3 `*Endpoints.cs`: linhas · `Results.Unauthorized` · `TryGet*Id` · `catch (` · `Results.Problem` | 316/408/442 · 25 · 2 · 10 · 15 | ≤150 cada · 0 · 0 · 0 · 0 |
| G6 | `grep -rln "Reelix.Domain" backend/Reelix.Api/Catalog backend/Reelix.Api/Tracking backend/Reelix.Api/Lists` | 1 (Tracking) | 0 |
| G7 | `grep -rn "accounts.GetOrCreateAsync" backend/Reelix.Application/Tracking backend/Reelix.Application/Lists --include=*.cs` | 16 | 0 |
| G8 | `grep -rn "IUserPreferencesRepository" backend/Reelix.Application/Lists --include=*.cs` | 1 | 0 |
| G9 | `git diff --exit-code -- backend/Reelix.Infrastructure/Persistence/Migrations` | — | vazio |
| G10 | Operações por `interface I…` dos 3 módulos · `*Service` com > 5 métodos · uso de relógio direto nos 3 módulos | `IUserListsRepository` 16 · `ITrackingRepository` 9 · `UserListsService` 15 e `TrackingService` 10 · 6 usos | ≤ 5 · 0 · 0 (o relógio fecha na `0013`) |

- **Comando/verificação:** `dotnet build Reelix.sln -c Release`; `dotnet test Reelix.sln -c Release` (Docker ativo, ≥ 202 testes + os novos); G1–G10.
- **Evidência esperada:** saídas no relatório do `implement-me`, com a matriz rota × teste, a tabela antes/depois e o diff vazio de `Migrations/`.
- **Comportamento observável:** igual ao atual; novidade verificável: `GET` dos 3 módulos não cria conta.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| **Decidido em 2026-11-19: constante nomeada.** Pergunta: idioma e provedor padrão do catálogo (`pt-BR`, TMDB) ficam em constante nomeada (premissa) ou passam a configuração (`appsettings`)? | Onde trocar o idioma; sem efeito no contrato | Não | Tech lead |
| Premissa: o port de existência é exposto pelo Catalog e implementado em `Persistence/Catalog`; Ratings e Users o adotam na `0016` | Estado transitório com 2 variantes | Não | Tech lead |
| Premissa: `user_preferences` para favoritos é garantida por port estreito da Users (passo explícito de escrita); o dono de `user_favorite_titles` é decisão da `0016` | Fronteira Lists/Users | Não | Tech lead |
| Premissa: o formato de erro de provedor da rota de Tracking (genérico, sem `type`/`code`) é preservado | Contrato de erro | Não | Tech lead |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação em modo lote; linha de base do auditor e de `grep`; `TrackingService` medido em 10 métodos públicos (docs dizem 11) | Épico arquitetura-design-testes, spec 3 de 4 do backend |
