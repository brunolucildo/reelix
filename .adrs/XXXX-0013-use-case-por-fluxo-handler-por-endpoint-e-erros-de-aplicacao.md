# XXXX-0013 — Use case por fluxo, handler por endpoint e erros de aplicação

- **Status:** proposto
- **Data:** 2026-11-23

> Prefixo `XXXX` provisório: o orquestrador renumera ao mesclar (a spec `0013` cita `0012`, mas esse número já pertence a outra decisão). Complementa o ADR 0001.

## Contexto

A auditoria de 2026-11-19 achou 105 problemas no backend, vários transversais: 57 usos de relógio global (41 no Domain), a leitura do `sub` em 13 pontos da Api, `try/catch` e mapeamento de erro por endpoint, `ValidatePage` em três cópias, cursor base64 em quatro pontos, DI com nomes repetidos e um `Program.cs` de 244 linhas. As specs `0014` a `0016` movem cada fluxo para o padrão-alvo (um use case por fluxo, um handler por endpoint) e só conseguem fazê-lo se a fundação comum existir. O contrato HTTP público (rotas, status e JSON) não pode mudar.

## Decisão

1. **Estrutura-alvo:** um use case por fluxo (`Application/<Modulo>/UseCases/<Fluxo>/`) e um handler por endpoint (`Api/<Modulo>/Handlers/`), 1:1, com DTOs de request/response em `Api/<Modulo>/Contracts/` e mapeamento explícito. O piloto é `GET /auth/me`.
2. **Erros por exceções tipadas**, não `Result<T>`: hierarquia `ApplicationError` (`ValidationError` 400, `NotFoundError` 404, `ConflictError` 409, `ServiceUnavailableError` 503, `UnauthenticatedError` 401, `ExternalDependencyError` com `Code` e `RetryAfter`). Os erros existentes herdam dela sem mudar mensagem. `ArgumentException` continua sendo 400.
3. **Um único mapeamento para `ProblemDetails`**, por **filtro de endpoint** (`ApplicationProblemFilter`), não por `IExceptionHandler`. O middleware de exceção do host registra em nível Error toda exceção que chega a ele, o que transformaria cada 400 ou 404 esperado em ruído de log; o filtro captura só os erros tipados (`when`) e não loga nada. Exceção inesperada segue para o `UseExceptionHandler` (500 sem stack).
4. **Origem do título:** metadado de rota (`ApplicationProblemOptions`: título de validação, título e detalhe de 404, estilo do erro de dependência `Detailed`/`TypeOnly`/`Opaque`) quando o título é da rota; o próprio erro carrega o título quando ele é do caso (conflitos, indisponibilidade). Assim cada módulo migra sem mudar status, título, detalhe, `type`, `code` nem `Retry-After`.
5. **`ArgumentException`** passa sempre por `ToUserMessage()` (sem o sufixo `(Parameter '…')`). Única mudança deliberada de contrato, em Social, Gamification e Notifications, quando cada módulo migrar.
6. **Tempo:** `TimeProvider` registrado (`TimeProvider.System`) e injetado em Application e Infrastructure; o Domain recebe o instante por parâmetro obrigatório (`DateTimeOffset now`).
7. **Identidade:** uma leitura única do `sub` (`CurrentUserExtensions`, `ICurrentUser`); a chave de partição do rate limiter usa o mesmo componente. Os `TryGet*Id` delegam a ele até as specs `0014` a `0016`.
8. **Conta:** port com passos explícitos `FindAsync` (não cria) e `EnsureAsync` (cria, tratando a violação do índice único). `GET /auth/me` é o ponto documentado de provisionamento; `GetOrCreateAsync` fica como alias transitório.
9. **Paginação e cursor:** `PageRequest` com políticas nomeadas que preservam limites e textos por fluxo (não unificados) e um `CursorCodec` (base64url sem padding, 200 caracteres).
10. **DI:** `Add<Modulo>Application()` e `Add<Modulo>Infrastructure()` por módulo, em classes `<Modulo>ApplicationModule`/`<Modulo>InfrastructureModule` sem nome repetido; `AddApplication`/`AddInfrastructure` só agregam. Opções TMDB/OMDb com `ValidateOnStart` que rejeita só valor presente malformado.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| ----------- | ---- | ------- | ----------- |
| `Result<T>` no lugar de exceções | Fluxo de erro explícito no tipo | Reescreve todos os fluxos e testes; o código atual e os adaptadores já lançam exceções | Custo alto sem ganho de contrato; fica registrado para reavaliar |
| `IExceptionHandler` global | Um ponto só, sem metadado por rota | Log em Error de 4xx esperado; título por rota exigiria metadado de qualquer forma | Ruído e vazamento de log; o filtro cobre o mesmo caso |
| Unificar limites de paginação | Contrato único | Muda `page`/`pageSize` aceitos | Decisão de produto de 2026-11-19: preservar por política |
| `TimeProvider.Testing` e NetArchTest | Menos código de teste | Pacote novo | A spec proíbe novo pacote; relógio fixo escrito à mão |

## Consequências

- Positivas: regras temporais determinísticas nos testes; erro HTTP em um só lugar; identidade lida uma vez; cada módulo migra com a tabela de erros da spec como teste.
- Negativas: `ExternalDependencyError` ainda carrega um `StatusCode` inteiro (dívida para `0015`); enquanto os módulos não migram, convivem o mapeador novo e os `try/catch` por endpoint.
- Impacto: assinaturas do Domain mudam (parâmetro `now`), serviços e repositórios recebem `TimeProvider`; testes passam a usar `FixedTimeProvider`. Sem migration, sem mudança de rota, status ou JSON.
