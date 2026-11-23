# Plano — Backend: fundação transversal

- **Status:** concluído (implementado em 2026-11-23; ver relatório do `implement-me`)
- **Slug:** backend-fundacao-transversal
- **Spec:** `.specs/0013-backend-fundacao-transversal.md`
- **Data:** 2026-11-23

## Contexto

Os 105 achados do auditor mecânico incluem problemas que se repetem em todos os módulos (relógio global, leitura do `sub`, mapeamento de erro por endpoint, paginação e cursor duplicados, DI irregular, `Program.cs` de 244 linhas). As specs `0014` a `0016` só migram fluxos para use case + handler se esta base existir.

## Objetivo

Fundação comum entregue sem mudar rota, status, JSON, OpenAPI, schema nem pacotes. Gates `G1` a `G10` da spec no valor-meta e suíte verde com no mínimo 202 testes.

## Não-objetivos

Migrar fluxos, remover `try/catch` dos endpoints, regra de negócio em repositório, testes de arquitetura, cobertura, unificar limites de página ou títulos de erro, qualquer migration ou pacote NuGet.

## Usuários e permissões

Sem mudança. O dono continua vindo do `sub` validado; a leitura passa por um único componente. `GET` não ganha escrita (a conta continua nascendo na primeira chamada que a resolve, e `GET /auth/me` é o ponto documentado de provisionamento).

## Fluxos e estados

Refatoração sem UI. Contrato de erro de `§5` da spec preservado e fixado por teste (linhas 1 a 6, 8, 10 a 13 nas rotas reais; todas as linhas de erro no mapeador por rota sintética).

## Requisitos

R1 a R13 da spec (critérios de aceite lá).

## Contratos de dados

Nenhum dado novo. Sem migration (G8).

## Integrações

TMDB, OMDb e Expo sem mudança de contrato; só passam a receber `TimeProvider` (parâmetro opcional, com `TimeProvider.System` como padrão, resolvido pelo DI).

## Decisões e premissas

- Decisão: erros por exceções tipadas; mapeamento por **filtro de endpoint**, não `IExceptionHandler` (log em Error de 4xx esperado); título por metadado de rota (`ApplicationProblemOptions`) ou carregado pelo erro. ADR `.adrs/0012-use-case-por-fluxo-handler-por-endpoint-e-erros-de-aplicacao.md` (número provisório).
- Decisão: `ICurrentUser` (interface + `CurrentUser` sobre `IHttpContextAccessor`) e `CurrentUserExtensions` (`GetSubject`, `ReadSubjectId`, `GetEmail`); o rate limiter usa `GetSubject`.
- Decisão: `IUserAccountRepository` ganha `FindAsync` e `EnsureAsync`; `GetOrCreateAsync` fica como método de interface com implementação padrão que chama `EnsureAsync` (alias transitório até `0014` a `0016`).
- Decisão: cache de gêneros atrás do port `ICatalogGenreCache` (implementação `InMemoryCatalogGenreCache` em Infrastructure, singleton, relógio injetado).
- Decisão: classes de DI `<Modulo>ApplicationModule` e `<Modulo>InfrastructureModule` (e `ApplicationModule`/`InfrastructureModule` no topo) para que G5 não encontre nome repetido.
- Premissa (conservadora): o auditor ainda acusa `handler-without-usecase` no piloto (o use case `GetAuthenticatedAccount` não tem o sufixo `UseCase`; renomear é da `0016`).
- Premissa: a resposta 429 observada hoje tem `Content-Type: application/json` (o `WriteAsJsonAsync` substitui o `application/problem+json` definido antes); o contrato foi preservado como está e a spec cita `problem+json`.
- Dúvida em aberto: nenhuma bloqueante.

## Abordagem (fatias)

0. Caracterização no código antigo: `RouteSurfaceTests` (70 rotas), `ErrorContractCharacterizationTests`, `PageLimitBaselineTests`, strings douradas de cursor.
1. Relógio: `TimeProvider`, parâmetro `now` no Domain, injeção em services e repositórios, `FixedTimeProvider` nos testes.
2. Identidade e conta: `CurrentUserExtensions`/`ICurrentUser`, `FindAsync`/`EnsureAsync`, `TryGet*Id` delegando.
3. Erros: `ApplicationError`, filtro e mapeador, rota sintética de teste, ADR.
4. `PageRequest`/`PagePolicy` e `CursorCodec`, trocando as 3 cópias e os pontos de base64.
5. DTO e piloto `GET /auth/me` em `AuthenticationEndpoints`; helper `HostJson`.
6. DI por módulo, opções com `ValidateOnStart`, `Program.cs` enxuto, rate limit por tabela, cache de gêneros; `.docs`; gates.

## Arquivos previstos

Domain (assinaturas com `now`), Application (`Common/`, `Errors/`, módulos de DI, services), Infrastructure (módulos de DI, repositórios, cache de gêneros, provedores), Api (`Authentication/`, `Errors/`, `RateLimiting/`, `Hosting/`, `Program.cs`), testes em `backend/Reelix.UnitTests`, `.adrs/`, `.docs/ARCHITECTURE.md` §4.2 e §6.1, `.docs/DEVELOPMENT.md` §2.1.

## Testes

- Unitário: Domain com relógio fixo; `PagePolicy`; `CursorCodec` com goldens; cache de gêneros; `ReadSubjectId`.
- Integração: `RouteSurfaceTests`, `ErrorContractCharacterizationTests`, `ErrorMappingTests` e `RateLimitingTests` (host sintético), `StartupConfigurationTests`, `UserAccountRepositoryPostgresTests` (Testcontainers).
- E2E: não se aplica. Manual: ver relatório.

## Riscos e rollback

Contrato HTTP (mitigado pelos testes de caracterização); concorrência de `Ensure` (índice único mantido, teste com 8 chamadas); revert do commit desfaz tudo, pois não há migration nem pacote.

## Critério de pronto

- [x] G1 a G10 no valor-meta, build e testes verdes (339), auditor sem `direct-clock-access`.
- [x] ADR proposto, `.docs` atualizados, relatório entregue.

## Perguntas abertas

| Pergunta | Impacto | Bloqueante? | Quem decide |
| -------- | ------- | ----------- | ----------- |
| Renomear `GetAuthenticatedAccount` para `GetAuthenticatedAccountUseCase` | Some o achado do auditor | Não | Spec `0016` |
