# 0007 — Outbox de eventos sociais para notificações

- **Status:** aceito
- **Data:** 2026-11-01 (aproximada, pelo histórico git)
- **Origem:** registro retroativo de decisão já implementada

## Contexto

Pedidos e aceites de amizade (módulo Social) devem gerar notificações (módulo Notificações) sem acoplar os dois nem fazer a ação do usuário depender do serviço de push. A spec `09-social.md` manda gravar eventos na mesma transação da mudança de relação e não entregar notificações; a `10-notifications.md` proíbe mensageria complexa sem necessidade, pede controle de duplicação e retentativa, e diz que não há fila selecionada. `technology-stack-evaluation.md` adia SQS/LocalStack até que haja requisito de entrega durável fora do processo da API.

## Decisão

- **Outbox em PostgreSQL:** a tabela `social_events` (`friend.requested` e `friend.accepted`, com versão, ator, destinatário e `occurred_at`; `CHECK` limita tipos e exige contas distintas) é gravada no **mesmo `SaveChanges`** da mudança em `friend_relationships` (`SocialRepository`).
- **Consumo idempotente:** `SocialNotificationProcessor` chama `ProcessSocialEventsAsync(100)`, que seleciona em uma transação os eventos sem linha em `notification_processed_social_events`, com `FOR UPDATE SKIP LOCKED`. Cada evento vira `created`, `disabled` (preferência social desligada) ou `obsolete` (relação já mudou), e o recibo é gravado na mesma transação. `UX_user_notifications_account_source` (chave `social:{eventId}`) impede notificação duplicada.
- **Entregas push com lease:** cada notificação gera uma `notification_deliveries` por dispositivo ativo. `ClaimPushDeliveriesAsync` (lote de 50, lease de 2 min, também `FOR UPDATE SKIP LOCKED`) reserva entregas `Pending` e grava o `lease_id`; a resposta só é registrada se o lease ainda for o mesmo. Ticket Expo vira `Ticketed`, e os recibos são conferidos depois (`ClaimReceiptChecksAsync`, expira em 24 h). Falha transitória faz retry com backoff exponencial com jitter (ou `Retry-After`), limitado por `ExpoPush:MaxAttempts`; `DeviceNotRegistered` desativa o dispositivo.
- **Workers finos:** `SocialNotificationWorker` e `NotificationDeliveryWorker` são `BackgroundService` em `Reelix.Api` que só abrem um escopo, chamam o processor e dormem (social: 1 s se houve trabalho, senão 15 s; entrega: 10 s ligado, 1 min desligado, ADR 0006). Exceção no lote é logada e o lote é repetido depois de 30 s; os eventos permanecem na fila.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| Notificar direto no `SocialService` | Menos peças | Ação do usuário passa a depender da notificação e do push | Specs 09 e 10: eventos desacoplados; sem dependência síncrona de serviço externo |
| SQS + LocalStack | Entrega durável, DLQ, workers independentes | Infraestrutura e operação novas | `technology-stack-evaluation.md`: adiar até haver requisito de entrega durável fora do processo |
| Eventos de domínio em memória (inferido) | Simples | Perdem-se no restart; sem retry | Inferido: `architecture.md` §1 só admite eventos de domínio com necessidade concreta |
| n8n para o fluxo | Pouco código | Dependência obrigatória fora do núcleo | `architecture.md` §11: n8n não é dependência síncrona de ações centrais |
| Biblioteca de jobs, como Hangfire (inferido) | Painel e retry prontos | Dependência e tabelas próprias | Inferido: não há menção nas fontes |

## Consequências

- Positivas: o evento é gravado de forma atômica com a mudança de amizade; sobrevive a restart; reprocessar é seguro (recibo e chave única); os `SKIP LOCKED` permitem mais de uma instância sem processar o mesmo item duas vezes (propriedade do SQL, não testada com concorrência); a lógica fica fora dos workers.
- Negativas: latência de até ~15 s até a notificação entrar; só dois tipos de evento (alertas de episódios e estreias não têm fonte nem agendamento); o worker roda no processo da API, então exige hosting contínuo; sem DLQ (esgotadas as tentativas, a entrega fica com status `Failed`); nenhuma rotina de limpeza de `social_events` e dos recibos foi encontrada no código; o único cenário concorrente com teste de integração é o de marcações de tracking (`ConcurrentMovieTrackingMarks_...`), então outbox e entregas não têm teste com várias instâncias; boa parte da regra de decisão está no `NotificationsRepository` (tensão do ADR 0001).
- Impacto em código, operação e time: `Reelix.Domain/Social/SocialEvent`, `Reelix.Domain/Notifications/*`, `NotificationsRepository`, processors em `Application/Notifications` e workers em `Reelix.Api/Notifications`. Migrations `AddSocial` e `AddNotifications`. Se a entrega precisar de durabilidade fora do processo, a evolução prevista é um broker (SQS), com novo ADR.
