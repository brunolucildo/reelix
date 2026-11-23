# 0001 — Monólito modular com Clean Architecture em quatro projetos

- **Status:** aceito
- **Data:** 2026-10-26 (aproximada, pelo histórico git)
- **Origem:** registro retroativo de decisão já implementada

## Contexto

O Reelix precisa de uma API única para catálogo, tracking, listas, avaliações, social, notificações e gamificação (specs `.specs/0001` a `0012`). `.docs/ARCHITECTURE.md` §1, §2 e §5 fixa os princípios: Clean Architecture, SOLID, DDD pragmático e KISS, "modular monolith first", sem microsserviços até que escala, times ou deploy independente justifiquem. O mesmo documento diz que o layout é conceitual e que não é obrigatório criar um projeto por módulo. Era preciso decidir quantos projetos existem, quem depende de quem e onde ficam as portas de saída.

## Decisão

- A solução `Reelix.sln` tem quatro projetos de produção em `backend/`: `Reelix.Domain`, `Reelix.Application`, `Reelix.Infrastructure` e `Reelix.Api`, mais `Reelix.UnitTests`.
- **Regra de dependência (comprovada nos `.csproj`):** `Domain` não referencia nada (nem pacote); `Application` referencia só `Domain` (e as abstrações de DI e de logging); `Infrastructure` referencia `Domain` e `Application`; `Api` referencia `Application` e `Infrastructure` e é a raiz de composição (`AddApplication()` e `AddInfrastructure()` em `Program.cs`).
- **Portas em `Application`:** interfaces como `ICatalogProvider`, `IUserAccountRepository`, `INotificationsRepository`, `IExpoPushClient` e `ICommentSpoilerClassifier` ficam em `Application`; `Infrastructure` as implementa (EF Core/Npgsql, clientes HTTP de TMDB, Expo e OpenAI).
- Os módulos de produto são pastas espelhadas nos quatro projetos (`Catalog/`, `Tracking/`, `Lists/`, `Ratings/`, `Social/`, `Notifications/`, `Gamification/`, `Users/`), com um `ReelixDbContext` único. `Api` expõe Minimal APIs por módulo e hospeda os `BackgroundService`.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| Microsserviços por módulo | Deploy e escala independentes | Autenticação, deploy e operação duplicados | `architecture.md` §2 e §16: sem demanda de escala ou de times separados |
| Um projeto por módulo de produto | Fronteira imposta pelo compilador | Proliferação de projetos | `architecture.md` §5 não exige; `.docs/QA.md` registra que a ausência não é falha |
| Projeto único sem camadas (inferido) | Menos cerimônia | Domínio acoplado a EF e HTTP; testes mais difíceis | Inferido: contraria os princípios de `architecture.md` §1 |
| Segunda API em NestJS | Reuso de padrões de outro projeto | Duplica deploy, autenticação e operação | `technology-stack-evaluation.md`: preservar a API ASP.NET Core |

## Consequências

- Positivas: `Domain` testável sem framework (`Reelix.UnitTests/Domain`); provedores externos trocáveis por portas; um único artefato de deploy; módulos localizáveis por pasta.
- Negativas: as fronteiras entre módulos são convenção (um `ReelixDbContext`, nenhum teste de arquitetura; `Reelix.ArchitectureTests` aparece em `architecture.md` §5 só como layout conceitual). Tensões conhecidas, verificadas por auditoria em 2026-11-19:
  - Regra de negócio em repositórios: `GamificationRepository.GetDashboardAsync` concede e revoga conquistas por limiar; `NotificationsRepository` decide o resultado de eventos sociais (`created`/`disabled`/`obsolete`), monta título e corpo da notificação, cancela entregas ao desligar preferências e calcula o backoff do push. `SocialNotificationProcessor` só repassa a chamada ao repositório.
  - Serviços de `Application` com muitos métodos públicos: `UserListsService` (15), `TrackingService` (10), `RatingsCommentsService` e `SocialService` (9 cada).
  - DTOs de `Application` servem como contrato HTTP: os endpoints devolvem os `record`s de `Reelix.Application.*` direto em `Results.Ok` (verificado em Catalog e Social), sem tipos de resposta próprios em `Api`.
  - A correção dessas tensões será tratada em spec futura; este ADR não prescreve solução.
- Impacto em código, operação e time: toda feature nova passa por Domain, Application, Infrastructure e Api, com registro de DI por módulo (`AddTrackingApplication`, `AddListsInfrastructure` etc.). Os workers rodam no processo da API, então o hosting precisa ficar continuamente ativo (ADR 0007). Referências: `.docs/ARCHITECTURE.md` §1, §2, §5; `.docs/QA.md` (visão geral).
