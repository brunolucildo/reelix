# 0014 — Backend: Gamification e Notifications em casos de uso

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** pronta
- **Slug:** backend-gamification-notifications-casos-de-uso
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-19
- **Owner:** Tech lead (a confirmar)
- **Relacionados:** `.plans/arquitetura-design-testes-breakdown.md` (D1–D5, D10–D11); `.adrs/0001-*`, `.adrs/0006-*`, `.adrs/0007-*`, ADR `0012` (proposto, entregue pela `0013`); depende da `0013`; vizinhas `0015` e `0016` (fronteiras de tabela); specs retroativas `0010` (notificações) e `0011` (gamificação); `.docs/ARCHITECTURE.md` §4.9, §4.10 e §6.1 (itens 1, 5 e 8).

## 1. Escopo

### Dentro do escopo

- Gamification: 5 rotas → 5 handlers + 5 casos de uso; avaliação de conquistas em serviço de domínio puro; um único ponto de escrita de `user_achievements`; estatísticas, ranking e badges por ports de leitura.
- Notifications: 7 rotas → 7 handlers + 7 casos de uso; os 2 workers (eventos sociais e entrega push) chamam 1 caso de uso por ciclo; decisão de evento social, montagem de título/corpo, cancelamento de entregas e backoff em serviços de domínio puros.
- Ports estreitos (ISP, ≤ 5 operações): `INotificationsRepository` (12 operações) e `IGamificationRepository` (5) deixam de existir; repositórios só persistem e consultam.
- `IExpoPushClient`/`ExpoPushResponse`/`ExpoReceiptResponse` viram `IPushSender` e tipos de resultado sem nome de fornecedor; o adaptador traduz erro HTTP (Application sem `HttpRequestException`).
- Testes de caracterização das conquistas, dos eventos sociais, da entrega push e do contrato HTTP **antes** de mover regra (hoje sem cobertura).
- DTOs HTTP na Api (sem tipo de Domain); `GamificationContracts.cs` (107 linhas) e `NotificationContracts.cs` (155) separados em comando/resultado, ports e erros.
- `GET /notifications/preferences` sem escrita; a escrita da reconciliação de conquistas isolada e explícita (ver Q1 de §11).

### Fora do escopo

- Fundação transversal (`TimeProvider`, `ICurrentUser`, conta `Ensure`/`Find`, mapeador de erros, cursor, DTO, DI): é da `0013` e já deve existir.
- Catalog, Tracking e Lists (`0015`); Ratings, Social, Users e Authentication (`0016`).
- Fronteiras de tabela: as leituras de `tracking_records`, `friend_relationships`, `user_profiles`, catálogo e `social_events` por Gamification e Notifications continuam nos adaptadores até a `0016` trocá-las por ports dos módulos donos.
- Mudar limiares de conquista, textos de notificação, backoff, leases, intervalos dos workers, habilitar push remoto ou alertas de catálogo; migrations; mobile.

## 2. Contexto

`GamificationRepository.GetDashboardAsync` (`Persistence/Gamification/GamificationRepository.cs:12–91`) abre transação `RepeatableRead`, trava `user_accounts` com `FOR UPDATE`, calcula estatísticas (`GetStatisticsCoreAsync`, linhas 284–380), decide `Earn`/`Revoke`/`Create` por limiar e grava, tudo dentro de um `GET`; `GetPublicBadgesAsync` chama o dashboard da conta **alvo** e portanto também grava. `NotificationsRepository.ProcessSocialEventAsync` (linhas 415–482) decide o resultado (`created`/`disabled`/`obsolete`) por string de `EventType`, monta título e corpo e cria entregas; `RecordPushResponseAsync` calcula backoff e `ClaimReceiptChecksAsync` o reagendamento de receipts. As regras só rodam com PostgreSQL. `NotificationDeliveryProcessor` captura `HttpRequestException`/`TaskCanceledException`/`JsonException`. O backend tem 202 testes e nenhum exercita a regra de conquistas, `ProcessSocialEvents` nem o backoff (`GamificationPersistenceTests` só chama o serviço com repositório falso).

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Usuário que vê conquistas, ranking e inbox; quem mantém regras de gamificação e notificação |
| Decisão ou ação que muda | Regras de conquista e de notificação passam a ser testáveis sem banco e a mudar em um lugar só |
| Resultado esperado       | Mesmas conquistas, notificações, entregas e respostas HTTP; regra fora do repositório |
| Métrica de sucesso       | Auditor sem achados em `Gamification` e `Notifications` (29 hoje, 5 high); regras cobertas por testes unitários; 0 regressão de contrato |

## 4. Requisitos

Gates `G1`–`G9` e linha de base em §10.

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | **Batch 0, conquistas** (HTTP + Postgres, semeando `tracking_records` e catálogo): para cada `AchievementRuleType` (4) em limiar−1, limiar e limiar+1; revogação ao cair abaixo; reativação preservando `firstEarnedAt`; definição inativa ignorada; `isMetadataIncomplete` para `MovieGenreCount`; JSON completo do dashboard; 8 `GET` simultâneos | Must | Verdes no código antigo; Given limiar atingido When 8 dashboards concorrentes Then 200 em todos e 1 linha por (conta, definição) |
| R2  | **Batch 0, eventos sociais**: `friend.requested` e `friend.accepted` ⇒ `created`/`disabled`/`obsolete` (relação mudou ou tipo desconhecido), título, corpo, `deepLink` `/social`, chave `social:{eventId}`, 1 entrega por dispositivo ativo; reprocessar não duplica; 2 lotes concorrentes não processam o mesmo evento (`SKIP LOCKED`) | Must | Verdes no código antigo, incluindo o teste de concorrência com 2 lotes sobre Postgres |
| R3  | **Batch 0, entrega push**: lease de 2 min; cancelamento por preferência desligada ou dispositivo inativo; ticket (receipt em +15 min); retry exponencial `5·2^(n−1)` s (teto 3600, jitter ±20 %) ou `Retry-After`; erro não retentável e `DeviceNotRegistered` ⇒ 1 tentativa e dispositivo desativado; receipts `15·2^min(n,5)` min (teto 360) e expiração em 24 h; `PUT /preferences` cancela entregas pendentes da categoria desligada | Must | Verdes no código antigo; tempo controlado pelo relógio da `0013` |
| R4  | **Batch 0, contrato HTTP** das 12 rotas: status + JSON de sucesso e erros 400 (cursor, período, ranking), 404 (dispositivo, notificação, conta alvo), 409 (dispositivo de outra conta ou token duplicado), 503 (push desativado), 429 | Must | JSON dourado gravado antes; passa sem mudar asserção depois |
| R5  | Serviços de domínio puros: `AchievementEvaluator` (definições ativas + conquistas existentes + estatísticas + `now` ⇒ decisões), `SocialEventNotificationPlanner` (evento + estado da relação + preferência + dispositivos ativos ⇒ desfecho, conteúdo e entregas) e `PushRetryPolicy` (jitter recebido por parâmetro, sem `Random.Shared`). Tipo do evento social definido no Domain (`friend.requested`/`friend.accepted`, texto persistido igual) | Must | Testes unitários sem `DbContext` nem Docker cobrem a matriz de R1–R3; G2 = 0 |
| R6  | 14 casos de uso (5 Gamification, 7 Notifications, 2 de worker), cada um com **um** `ExecuteAsync(Command\|Query, ct)` e `Command`/`Result` ao lado; 12 handlers 1:1 injetando um caso de uso; `*Endpoints.cs` só mapeiam rotas | Must | G1 exit 0 para os dois módulos; Endpoints ≤ 150 linhas |
| R7  | Ports estreitos por necessidade (sugestão: catálogo de conquistas, conquistas do usuário, leitor de estatísticas, preferências de gamificação, ranking, badges; preferências, dispositivos, inbox, outbox de eventos sociais, fila de entregas, fila de receipts, `IPushSender`), nenhum com mais de 5 operações; cada caso de uso injeta só o que usa | Must | Given `interface I…` dos dois módulos When contadas as operações Then ≤ 5 cada; `IGamificationRepository` e `INotificationsRepository` inexistentes |
| R8  | Concorrência e transações preservadas: dashboard/reconciliação em `RepeatableRead` + `FOR UPDATE` em `user_accounts`; processamento de eventos com seleção `FOR UPDATE SKIP LOCKED` + notificação + entregas + recibo na **mesma** transação; leases `SKIP LOCKED` de entregas e receipts. O limite transacional é um port estreito (o caso de uso não conhece EF) | Must | R1 e R2 verdes; G5 igual ao baseline |
| R9  | `IPushSender` (`SendAsync` ⇒ resultado, `GetReceiptAsync` ⇒ resultado); o adaptador traduz `HttpRequestException`/timeout/`JsonException` para os **mesmos** códigos persistidos (`expo_timeout`, `expo_transport_error`, retentável). Falha de transporte em `GetReceipt` continua abortando o lote e o worker repete em 30 s (preservar, não melhorar) | Must | Given Expo fora do ar When envio Then `last_error` = `expo_transport_error`; G6 = 0 |
| R10 | Escrita de conquistas só em `AchievementReconciler` (serviço de capacidade) e só ali `SaveChanges` em Gamification; `GET /statistics` e `GET /friends/ranking` sem escrita (conta ausente ⇒ resposta de conta nova, sem criar linha); regra de escrita no `GET /dashboard` e `GET /users/{id}/badges` conforme **Q1** | Must | Given interceptor de comandos When statistics e ranking Then 0 `INSERT`/`UPDATE`/`DELETE`; G7 |
| R11 | `GET /notifications/preferences` sem escrita: conta sem linha ⇒ `{socialEnabled:false,newEpisodeEnabled:false,premiereEnabled:false}` (padrão do Domain, e ausência de linha já equivale a "desligado" nos workers); inbox, contagem, marcar lido e remover dispositivo usam `Find` | Must | Given conta nova When `GET` Then 200 com 3 `false` e 0 linhas em `notification_preferences`; When `PUT` e `GET` Then valores salvos |
| R12 | Nenhum tipo de Domain em `Command`/`Query`/`Result` nem em DTO da Api (`UserAchievementStatus`, `NotificationCategory`, `NotificationDevicePlatform` ganham espelho no Application/Api); corpos `UpdateNotificationPreferences` e `RegisterNotificationDevice` viram DTOs da Api aceitando as mesmas entradas de enum (string em qualquer caixa e inteiro) | Must | G8 = 0; JSON dourado idêntico, inclusive `platform`, `category` e `status` |
| R13 | Workers finos em `Api/Notifications/Workers/`: 1 caso de uso por ciclo; intervalos (1 s/15 s e 10 s/1 min, retry de 30 s), lotes (100, 50 e 50) e lease de 2 min como constantes nomeadas e **iguais** | Should | Cada worker resolve exatamente 1 tipo `*UseCase`; comportamento por R2/R3 |
| R14 | Contrato intacto: rotas, status, JSON, OpenAPI, rate limits (`notifications-write` ×3, `social-write` em `PUT /gamification/preferences`) e autorização; sem migration | Must | `RouteSurfaceTests` da `0013` verde sem alterar baseline; G4 vazio |

## 5. Fluxos e estados

- **Caminho feliz:** handler lê `ICurrentUser` ⇒ converte DTO em comando ⇒ caso de uso resolve a conta (`Ensure` na escrita, `Find` na leitura) ⇒ ports + serviço de domínio ⇒ resultado ⇒ DTO. **Vazio:** conta sem dados ⇒ página/lista/estatística zerada, igual a hoje. **Carregando:** não se aplica (sem UI).
- **Erro:** os erros da `0013` §5 (linhas 1/2 para 400, 9 para o conflito de dispositivo, 10 para push indisponível); `KeyNotFoundException` de conta alvo ⇒ 404. `InvalidOperationException` deixa de existir no módulo (hoje vira 503 em qualquer ponto de `POST /devices`).
- **Sucesso parcial:** em um lote de envio, falha de um item grava retry só nele e os demais seguem. **Cancelamento/retry:** erro no lote ⇒ log e repetição em 30 s com os eventos na fila; encerramento da aplicação propaga `OperationCanceledException`.

## 6. Acceptance Criteria

- [ ] Given conta com N filmes no limiar−1, limiar e limiar+1 When dashboard Then conquista ausente, concedida e mantida; remover um registro revoga e voltar reativa (R1).
- [ ] Given evento `friend.requested` com relação pendente e preferência social ligada When processado Then 1 notificação "Nova solicitação de amizade" com `deepLink` `/social` e 1 entrega por dispositivo ativo; reprocessar não duplica (R2).
- [ ] Given Expo respondendo `MessageRateExceeded` com `Retry-After` When registrado Then a próxima tentativa respeita o prazo e o limite de tentativas (R3, R9).
- [ ] Given `PUT /notifications/preferences` desligando a categoria When há entregas pendentes Then são canceladas com `preference_disabled` (R3).
- [ ] Given conta nova When `GET /notifications/preferences`, `/gamification/statistics` e `/gamification/friends/ranking` Then 200 sem criar linha (R10, R11).
- [ ] Given as 12 rotas When comparadas ao JSON dourado Then idênticas, com os mesmos status de erro (R4, R12, R14).
- [ ] Given `dotnet build Reelix.sln -c Release`, `dotnet test Reelix.sln -c Release` e G1–G9 Then sem erro e no valor-meta.

## 7. O que fazer

1. Batch 0: escrever e rodar verdes sobre o código antigo os testes de R1–R4 (semeadura por SQL/`DbContext` como em `PostgresAuthorizationFixture`).
2. Domain: `AchievementEvaluator`, `SocialEventNotificationPlanner`, `PushRetryPolicy` e tipo do evento social, com testes unitários.
3. Application: ports, `Command`/`Result`, `AchievementReconciler` e os 14 casos de uso; remover `GamificationService`, `NotificationsService`, os dois processors e os `*Contracts.cs`.
4. Infrastructure: dividir `GamificationRepository` (422) e `NotificationsRepository` (500) em adaptadores por port, só persistência e consulta; `IPushSender` no adaptador do Expo com a tradução de erros.
5. Api: DTOs, 12 handlers, `*Endpoints.cs` finos, workers em `Workers/`; `Program.cs` sem novo registro além do módulo.
6. DI por módulo; atualizar `.docs/ARCHITECTURE.md` §4.9, §4.10 e §6.1 (itens 1, 5 e 8 nos dois módulos); gates; `commit-and-push` pelo `implement-me`.

## 8. O que não fazer

- Não mudar limiar, texto, backoff, lease, intervalo, lote nem formato de cursor; não tocar em `Migrations/`.
- Não deixar regra (`Earn`, `Revoke`, `switch` por tipo de evento) em repositório; não usar `Random.Shared` nem relógio global nas regras.
- Não "consertar" o aborto do lote em falha de receipt nem o `GET` do dashboard sem decisão em Q1.
- Não ler tabela de outro módulo em código novo; os acessos atuais seguem até a `0016`.
- Não logar token Expo, corpo de notificação ou `sub`; não relaxar, pular nem alterar asserção de teste.

## 9. Riscos e dependências

| Tipo | Descrição | Mitigação |
| ---- | --------- | --------- |
| Dependência | `0013` (relógio, identidade, erros, DTO, DI); Docker para os testes Postgres | Não iniciar antes da `0013` |
| Risco | Contrato HTTP: JSON de enums e propriedades extras (`authorAvatarKey`-like) muda ao trocar o DTO | JSON dourado do Batch 0; `JsonStringEnumConverter` do host |
| Risco | Concorrência: lock/transação perdidos ao separar leitura e escrita; duas conquistas para a mesma definição | R1 e R2 com concorrência; manter PK de `user_achievements` e `FOR UPDATE`/`SKIP LOCKED` (G5) |
| Risco | O Batch 0 de concorrência pode expor falha já existente | Registrar como achado e preservar; não corrigir nesta spec |
| Risco | Migrations: nenhuma permitida | G4 |
| Risco | O auditor ignora trechos depois de `$"""` (SQL cru dos dois repositórios) | Gates por `grep` (G2, G5, G6) além do `--strict` |
| Risco | `GetPublicBadgesAsync` grava na conta de outra pessoa ao consultar | Preservado até a Q1; registrar no caso de uso |

## 10. Resultado

Linha de base de 2026-11-19 (`audit-slices.mjs --scope backend`: Gamification 2 high + 10 medium; Notifications 3 high + 14 medium):

| Gate | Verificação | Hoje | Meta |
| ---- | ----------- | ---- | ---- |
| G1 | `node .claude/skills/refactor-slices/scripts/audit-slices.mjs --scope backend --module Gamification --strict` e `--module Notifications --strict` | 12 e 17 achados (exit 1) | exit 0 nos dois |
| G2 | `grep -rnE "\b(Earn\|Revoke)\(\|\bswitch\b" backend/Reelix.Infrastructure/Persistence/Gamification backend/Reelix.Infrastructure/Persistence/Notifications --include=*Repository.cs` | 5 | 0 |
| G3 | Endpoints (linhas) · `Results.Unauthorized` · `TryGet*Id` · `catch (` · `Results.Problem` nos 2 `*Endpoints.cs` | 165/196 · 12 · 2 · 7 · 6 | ≤150 cada · 0 · 0 · 0 · 0 |
| G4 | `git diff --exit-code -- backend/Reelix.Infrastructure/Persistence/Migrations` | — | vazio |
| G5 | `grep -rn "FOR UPDATE SKIP LOCKED" backend/Reelix.Infrastructure/Persistence/Notifications --include=*.cs` e `grep -rn "FOR UPDATE" backend/Reelix.Infrastructure/Persistence/Gamification --include=*.cs` | 3 e 2 | 3 e 2 |
| G6 | `grep -rnE "HttpRequestException\|TaskCanceledException\|JsonException\|IExpoPushClient\|ExpoPushResponse\|ExpoReceiptResponse" backend/Reelix.Application/Notifications backend/Reelix.Application/Gamification` | 13 linhas | 0 |
| G7 | `grep -rn "SaveChangesAsync" backend/Reelix.Infrastructure/Persistence/Gamification --include=*.cs` (só em adaptador de escrita) | 2 (linhas 64 e 118 de `GamificationRepository.cs`, junto com leituras) | só no adaptador de escrita |
| G8 | `grep -rln "using Reelix.Domain" backend/Reelix.Application/Gamification backend/Reelix.Application/Notifications backend/Reelix.Api/Gamification backend/Reelix.Api/Notifications --include=*Command.cs --include=*Query.cs --include=*Result.cs --include=*Request.cs --include=*Response.cs` | n/a (ainda não há `Command`/`Result`); hoje os 2 `*Contracts.cs` expõem `UserAchievementStatus`, `NotificationCategory` e `NotificationDevicePlatform` | 0 |
| G9 | Operações por `interface I…` (R7) · `INotificationsRepository` · `IGamificationRepository` | 12 · 5 | ≤ 5 cada · inexistentes |

- **Comando/verificação:** `dotnet build Reelix.sln -c Release`; `dotnet test Reelix.sln -c Release` (Docker ativo, ≥ 202 testes + os novos); G1–G9.
- **Evidência esperada:** saídas no relatório do `implement-me` com tabela antes/depois e lista de testes novos por requisito.
- **Comportamento observável:** igual ao atual; novidade verificável: `GET /notifications/preferences`, `/gamification/statistics` e `/friends/ranking` não gravam.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| **Decidido em 2026-11-19: opção A (preservar o comportamento).** Pergunta **Q1**: o `GET /gamification/dashboard` (e `/users/{id}/badges`) deve parar de gravar? Remover a escrita exige um gatilho explícito para reconciliar (opção B: port chamado pelos fluxos de tracking na `0015`; opção C: worker ou endpoint de reconciliação) e muda o momento de `firstEarnedAt`/`lastRevokedAt` para quem atinge o limiar. Default (A): a escrita fica **fora do repositório e dentro de um passo explícito e nomeado** (`AchievementReconciler`), ainda acionado pelo caso de uso do dashboard, com contrato idêntico | Timestamps de conquista e coerência entre telas | Não (default A; B/C viram spec própria) | Tech lead/Produto |
| Premissa: ausência de linha em `notification_preferences` equivale a categorias desligadas (padrão do Domain e do `ProcessSocialEvent`/`ClaimPushDeliveries`) | `GET` sem escrita é neutro | Não | Tech lead |
| Premissa: limite transacional como port estreito (ex.: lote de eventos reservado e confirmado), formato no plano | Preserva atomicidade | Não | Plano |
| Premissa: `ExpoPushToken` permanece como campo do contrato HTTP (ADR 0006); só o port perde o nome do fornecedor | Contrato JSON | Não | Tech lead |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação em modo lote; linha de base do auditor e de `grep`; `INotificationsRepository` medido em 12 operações (o pedido dizia ~14) | Épico arquitetura-design-testes, spec 2 de 4 do backend |
