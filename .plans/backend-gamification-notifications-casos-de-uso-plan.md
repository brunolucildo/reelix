# Plano — Backend: Gamification e Notifications em casos de uso

- **Status:** concluído (implementado em 2026-11-24; ver relatório do `implement-me`)
- **Slug:** backend-gamification-notifications-casos-de-uso
- **Spec:** `.specs/0014-backend-gamification-notifications-casos-de-uso.md`
- **Data:** 2026-11-24

## Contexto

`GamificationRepository` (422 linhas) e `NotificationsRepository` (500) concentravam a regra de conquistas, a decisão de evento social, o backoff e o cancelamento de entregas dentro de SQL e transações; um `GET` gravava e a Application capturava `HttpRequestException`. A `0013` entregou a fundação (relógio, identidade, `Find`/`Ensure`, erros e mapeador, cursor, DTO, DI por módulo).

## Objetivo

Regra de negócio fora dos repositórios e testável sem banco; 14 casos de uso, 12 handlers 1:1; ports de até 5 operações; `GET`s sem escrita implícita; contrato HTTP intacto. Gates `G1` a `G9` da spec no valor-meta.

## Não-objetivos

Mudar limiar, texto, backoff, lease, intervalo, lote, formato de cursor; migrations; mobile; tirar a escrita do dashboard (Q1, opção A); corrigir defeitos preexistentes que o Batch 0 expuser; trocar as leituras de tabelas de outros módulos (spec `0016`).

## Usuários e permissões

Sem mudança. O dono vem do `sub` validado por `ICurrentUser`; `GET /gamification/users/{id}/badges` lê a conta alvo sem exigir relação.

## Fluxos e estados

Contrato de erro da `0013` §5: linhas 1/2 (400, sem sufixo `(Parameter …)` em Gamification e Notifications), 8 (404 sem corpo próprio: `Results.NotFound()`), 9 (409 de dispositivo), 10 (503 de push), 12 (429). `KeyNotFoundException` deixa de existir (resultado nulo no caso de uso); `InvalidOperationException` deixa de virar 503.

## Requisitos

R1 a R14 da spec.

## Contratos de dados

Nenhum dado novo; sem migration (G4 vazio).

## Integrações

Expo Push atrás de `IPushSender`; o adaptador traduz erro de transporte nos mesmos códigos (`expo_transport_error`, `expo_timeout`).

## Decisões e premissas

- Q1 opção A: o dashboard e os badges continuam gravando, pelo passo `AchievementReconciler`; ADR `.adrs/XXXX-0014-reconciliacao-explicita-ports-estreitos-e-erro-do-fornecedor-push.md` (número provisório).
- Limite transacional como port: `IAchievementReconciliationTransaction`, `ISocialEventOutbox`, `IPushDeliveryQueue` (lote reservado e confirmado); demais ports devolvem entidades rastreadas e `INotificationsUnitOfWork` confirma.
- Espelhos de enum no Application (`AchievementStatus`, `NotificationCategoryKind`, `DevicePlatform`), guardados por teste.
- Casos de uso de worker em `Application/Notifications/Workers/`.
- Achados do Batch 0 preservados (F1 ranking 500, F2 dashboard concorrente 500, F3 binding de enum em Development): ver spec §11.
- Dúvida em aberto: nenhuma bloqueante.

## Abordagem (fatias)

0. Batch 0: caracterização HTTP + PostgreSQL (conquistas, eventos sociais, entrega push, contrato das 12 rotas) verde no código antigo.
1. Domain: `AchievementEvaluator`, `SocialEventNotificationPlanner`, `PushRetryPolicy`, `PushDeliveryEligibility`, tipo do evento social e testes unitários.
2. Gamification: ports, `AchievementReconciler`, 5 casos de uso, adaptadores, DTOs, 5 handlers, endpoints finos.
3. Notifications: ports, 7 casos de uso + 2 de worker, adaptadores, `IPushSender`, DTOs, 7 handlers, workers finos.
4. Testes novos (use cases com ports falsos, arquitetura, sender, leituras sem escrita) e ajuste dos testes antigos que citavam os tipos removidos.
5. Docs (`ARCHITECTURE.md` §4.9, §4.10 e §6.1), ADR, gates e commit.

## Arquivos previstos

`backend/Reelix.Domain/{Gamification,Notifications,Social}`, `backend/Reelix.Application/{Gamification,Notifications}`, `backend/Reelix.Infrastructure/{Persistence/Gamification,Persistence/Notifications,Notifications}` e os dois módulos de DI, `backend/Reelix.Api/{Gamification,Notifications}`, `Program.cs` (só o `using` dos workers), testes em `backend/Reelix.UnitTests`, `.docs/ARCHITECTURE.md`, `.adrs/`.

## Testes

- Unitário (sem Docker): Domain (`AchievementEvaluatorTests`, `SocialEventNotificationPlannerTests`, `PushPolicyTests`), casos de uso com ports falsos (`GamificationUseCaseTests`, `NotificationUseCaseTests`, `PushWorkerUseCaseTests`), `ExpoPushSenderTests`, `GamificationNotificationsArchitectureTests`.
- Integração (PostgreSQL, Docker): `Characterization/*` (Batch 0 e `ReadsDoNotWriteTests`).
- E2E: não se aplica.

## Riscos e rollback

Contrato HTTP (mitigado pelo Batch 0), concorrência (mesmas transações e `FOR UPDATE`/`SKIP LOCKED`, G5), defeitos preexistentes preservados. Revert do commit desfaz tudo (sem migration nem pacote).

## Critério de pronto

- [x] G1 a G9 no valor-meta, build e testes verdes.
- [x] ADR proposto, `.docs` atualizados, relatório entregue.

## Perguntas abertas

| Pergunta | Impacto | Bloqueante? | Quem decide |
| -------- | ------- | ----------- | ----------- |
| Corrigir F1 (ranking 500) e F2 (dashboard concorrente 500) | Contrato real de Gamification | Não | Tech lead (spec própria) |
