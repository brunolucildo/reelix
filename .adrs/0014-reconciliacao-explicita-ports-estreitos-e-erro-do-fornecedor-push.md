# 0014 — Reconciliação explícita de conquistas, ports estreitos com limite transacional e erro do fornecedor push no adaptador

- **Status:** proposto
- **Data:** 2026-11-24

> Registrado pela spec `0014` (o número definitivo é atribuído na integração). Aplica o ADR 0012 aos módulos Gamification e Notifications.

## Contexto

`GamificationRepository` e `NotificationsRepository` (422 e 500 linhas) decidiam conquistas, desfecho de evento social, backoff e cancelamento de entregas dentro de SQL e transações; as regras só rodavam com PostgreSQL, um `GET` gravava e a Application capturava `HttpRequestException`. A spec `0014` move a regra para o Domain e o fluxo para casos de uso, sem mudar rota, status, JSON, schema nem comportamento observável. Três decisões não estavam no ADR 0012.

## Decisão

1. **A escrita de conquistas é um passo explícito e nomeado, `AchievementReconciler`.** Ele (Application) lê estatísticas, deixa o `AchievementEvaluator` (Domain, puro) decidir conceder/reativar/revogar e grava dentro de uma transação `RepeatableRead` com lock da linha da conta. `GET /gamification/dashboard` e `GET /gamification/users/{id}/badges` continuam acionando-o (Q1, opção A, decidida em 2026-11-19): o contrato e o momento de `firstEarnedAt`/`lastRevokedAt` não mudam. `GET /gamification/statistics` e `/friends/ranking` e as leituras de Notifications deixam de gravar; conta ou preferência ausente vira a resposta de "conta nova" sem criar linha.
2. **O limite transacional é um port estreito, nunca `DbContext` no caso de uso.** A reconciliação usa `IAchievementReconciliationTransaction` (abre a transação e devolve um escopo que confirma ou desfaz ao ser descartado). Os workers de Notifications reservam um lote por port (`ISocialEventOutbox`, `IPushDeliveryQueue`): as linhas ficam travadas com `FOR UPDATE SKIP LOCKED` até `CommitAsync`, e a decisão sobre cada linha é do caso de uso e do Domain. Os demais ports (≤ 5 operações) devolvem entidades rastreadas e `INotificationsUnitOfWork` confirma.
3. **O fornecedor push fica atrás de `IPushSender` e o adaptador traduz o erro de transporte.** `HttpRequestException`, timeout e `JsonException` do envio viram um resultado retentável com os mesmos códigos persistidos (`expo_transport_error`, `expo_timeout`); só o cancelamento do pedido propaga. A falha de transporte ao ler um receipt continua propagando e abortando o ciclo (comportamento preservado, não melhorado). O jitter do backoff vem de um port (`IJitterSource`) e entra como parâmetro em `PushRetryPolicy`.

Complementos: os tipos de Domain `UserAchievementStatus`, `NotificationCategory` e `NotificationDevicePlatform` ganham espelhos no Application (`AchievementStatus`, `NotificationCategoryKind`, `DevicePlatform`) com os mesmos nomes e valores, guardados por teste; o `detail` de 400 de Gamification e Notifications deixa de vazar o sufixo ` (Parameter '…')` (ADR 0012, item 5).

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| ----------- | ---- | ------- | ----------- |
| Parar de gravar no `GET /dashboard` agora (opção B/C da Q1) | `GET` puro | Exige gatilho de reconciliação nos fluxos de tracking ou um worker e muda quando as conquistas são carimbadas | Decisão de produto de 2026-11-19: preservar; vira spec própria |
| `DbContext` ou `IDbContextTransaction` injetado no caso de uso | Menos código | A Application passa a conhecer EF; regra e persistência voltam a se misturar | Viola o limite do ADR 0001 e o objetivo da spec |
| Mapear o erro de transporte na Application | Um catch no caso de uso | Application conhece `HttpRequestException` | O adaptador é o dono do fornecedor (ADR 0012) |
| Port único `INotificationsRepository` menor | Menos arquivos | Volta a ser um repositório por módulo, sem ISP | A spec exige ports por necessidade e ≤ 5 operações |

## Consequências

- Positivas: a regra de conquistas, de eventos sociais, de backoff e de elegibilidade roda em teste unitário sem Docker; as leituras não gravam; a Application não referencia HTTP nem fornecedor; cada adaptador cabe em um arquivo curto.
- Negativas: mais tipos (14 casos de uso, 12 handlers, ~20 ports); o `GET /dashboard` continua gravando e, com dashboards simultâneos da mesma conta, o `RepeatableRead` pode responder 500 (40001), achado preservado do Batch 0; o ranking de quem o ligou responde 500 por defeito preexistente de tradução do EF Core, também preservado; as leituras de tabelas de outros módulos seguem nos adaptadores até a spec `0016`.
- Impacto: sem migration e sem pacote novo; `IExpoPushClient`, `IGamificationRepository`, `INotificationsRepository`, `GamificationService`, `NotificationsService` e os dois processors deixam de existir; os testes passam a usar o relógio fixo e fakes de port.
