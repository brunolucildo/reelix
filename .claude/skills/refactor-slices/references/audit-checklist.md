# Checklist de auditoria de slices — Reelix (.NET)

> Usado pelo **Agente 1 (Auditor)** e pelo **Agente 4 (QA)**.
> O script pega o que é mecânico; o auditor precisa ler o código e julgar o que é semântico.

## 1. Rodar o script

Na raiz do repositório:

```bash
node .claude/skills/refactor-slices/scripts/audit-slices.mjs --scope backend
```

Variações:

```bash
# JSON para anexar ao run directory
node .claude/skills/refactor-slices/scripts/audit-slices.mjs --json > .claude/refactor/<run-id>/01-audit.raw.json

# Um módulo só
node .claude/skills/refactor-slices/scripts/audit-slices.mjs --module Lists

# Gate final: exit 1 se sobrar achado (exceções documentadas entram em --allow)
node .claude/skills/refactor-slices/scripts/audit-slices.mjs --strict --allow <regra1,regra2>
```

O script varre `backend/**/*.cs` (ignora `bin/`, `obj/`, `Migrations/` e, por padrão, `Reelix.UnitTests`) e agrupa por módulo. Regras mecânicas:

| Id | Regra | O que acusa | Sev. |
| --- | --- | --- | --- |
| (a) | `service-too-many-methods` | `*Service.cs` em Application com mais de 5 métodos públicos | medium/high (>10) |
| (b) | `contracts-mixed` | `*Contracts.cs` com `interface I...` (port) junto de `record` (DTO) e, se houver, exceções | high |
| (c) | `endpoints-too-long` | `*Endpoints.cs` com mais de ~150 linhas (>300 = high) | medium/high |
| (c) | `endpoint-repeated-auth-check` | `TryGet*Id`/`Results.Unauthorized` repetidos (≥3) | medium |
| (c) | `duplicated-user-id-helper` | `TryGet*Id` definido em mais de um arquivo da Api | medium |
| (c) | `endpoint-inline-error-mapping` | `catch (` / `ArgumentException` → `ProblemDetails` inline | medium |
| (d) | `endpoint-multiple-dependencies` | endpoint (método ou lambda) injetando mais de um service/use case | high |
| (e) | `direct-clock-access` | `DateTime(Offset).UtcNow\|Now` em Domain/Application/Infrastructure | medium |
| (f) | `api-imports-domain` | referência a `Reelix.Domain` em arquivo da Api | medium |
| (g) | `layer-purity-framework` | `Microsoft.EntityFrameworkCore`/`Microsoft.AspNetCore` em Application/Domain | high |
| (g) | `application-http-leak` | `HttpStatusCode`/`HttpRequestException` em Application | high |
| (h) | `repository-business-rule-suspect` | `Earn(`, `Revoke(`, `switch` em `*Repository.cs` — **suspeita**, confirmar por leitura | medium |
| (i) | `file-too-long` | arquivo com mais de 400 linhas (>600 = high) | medium/high |
| (j) | `module-without-usecases-folder` | módulo da Application sem `UseCases/` | high |
| extra | `module-without-handlers-folder` | módulo da Api com rotas e sem `Handlers/` | medium |
| extra | `layer-direction` | `using Reelix.X` contra a direção (ex.: Application → Infrastructure; Api → Infrastructure fora de `Program.cs`/DI) | high |
| extra | `handler-multiple-usecases` / `handler-without-usecase` / `handler-injects-non-usecase` | handler fora do 1:1 | high/medium |
| extra | `handler-route-count-mismatch` | nº de handlers ≠ nº de rotas do módulo | medium |
| extra | `usecase-execute-count` | use case sem exatamente um `ExecuteAsync` | high |
| extra | `usecase-shared-by-handlers` / `usecase-without-handler` | use case consumido por 2+ handlers / por nenhum | high/low |

O script **não** encontra (o auditor precisa ler):

- regra de negócio dentro de handler, endpoint ou DTO;
- use case que só repassa chamada ou que orquestra dois fluxos;
- port grande demais (violação de ISP) ou nomeado pela tecnologia;
- adapter com regra de negócio que não use as palavras da heurística (h);
- entidade anêmica com regra espalhada em service;
- módulo lendo tabela de outro (`DbContext.Set<...>` de entidade alheia) ou injetando service concreto de outro módulo;
- GET com efeito colateral de escrita (ex.: criação preguiçosa de conta/perfil);
- ausência de `CancellationToken` em cadeia assíncrona;
- paginação/cursor com validação duplicada;
- falta de teste de caracterização para o fluxo;
- teste que mudou de asserção para acomodar comportamento novo.

## 2. Auditoria semântica por camada

### 2.1 Domain

- [ ] Nenhuma referência a EF Core, ASP.NET, HTTP ou outro projeto.
- [ ] Entidades protegem invariantes (setters privados, fábricas/métodos que validam); sem entidade anêmica com regra em service.
- [ ] Nenhum `DateTime(Offset).UtcNow|Now`: o instante entra por parâmetro.
- [ ] Regras como avaliação de conquistas e processamento de evento social vivem em domain service puro.
- [ ] Erros de domínio são tipados, não strings/`Exception` genérica.

### 2.2 Application

- [ ] Cada fluxo tem **uma** classe `<Fluxo>UseCase` com **um** `ExecuteAsync(<Fluxo>Command|Query, CancellationToken)`.
- [ ] `<Fluxo>Command|Query` e `<Fluxo>Result` ao lado do use case; nenhum `*Contracts.cs` misturando DTO, port e exceção.
- [ ] Ports em `Ports/`, pequenos (leitura/escrita separadas), nomeados pela necessidade.
- [ ] Erros tipados em `Errors/`; **uma** estratégia (Result ou exceções) por módulo, mapeada num único ponto.
- [ ] Sem `HttpStatusCode`, `HttpRequestException`, EF Core, `HttpContext`, `IResult`, DTO HTTP.
- [ ] Relógio por `TimeProvider`; efeitos colaterais (notificação, evento) **depois** de persistir.
- [ ] `Services/` só com capacidade reutilizável (cursor, emissão), nunca um fluxo disfarçado.
- [ ] Nenhum use case injeta service/use case concreto de outro módulo; só port estreito do módulo dono.
- [ ] `CancellationToken` propagado.
- [ ] GET sem escrita: criação preguiçosa de conta/perfil é passo explícito.
- [ ] Paginação validada em um lugar; cursor opaco em um helper.

### 2.3 Infrastructure

- [ ] Repositório só persiste/consulta; nenhuma regra (avaliação de conquista, decisão por tipo de evento, limite, normalização).
- [ ] Cada adapter implementa port(s) estreito(s); EF Configurations em `Persistence/<Modulo>/`.
- [ ] Adapter de provedor (TMDB, OMDb, OpenAI, Expo) traduz erro HTTP para erro de aplicação tipado, com timeout/cancelamento.
- [ ] Nenhum adapter depende de service/use case concreto de outro módulo (usar port).
- [ ] DI do módulo em um `<Modulo>Module.cs`/`AddXxx` consistente.
- [ ] Logs/telemetria sem token, e-mail, corpo de comentário ou PII.

### 2.4 Api

- [ ] `<Modulo>Endpoints.cs` só faz `MapGroup`, rotas e metadados OpenAPI.
- [ ] **Um** `Handlers/<Fluxo>Handler.cs` por endpoint; cada um injeta **exatamente um** use case (além de `ICurrentUser`).
- [ ] Leitura do usuário autenticado em **uma** abstração; sem `TryGetUserId` por arquivo.
- [ ] Request/response DTOs na Api; nenhum tipo de Domain na assinatura pública.
- [ ] Sem `catch`/`Results.Problem` inline: erros vão ao mapeador central (pt-BR).
- [ ] Parse de enum e paginação via helper único.
- [ ] `WithName`, `WithSummary`, `Produces*`, `RequireAuthorization`, tags e rate limits idênticos aos originais.
- [ ] Workers chamam um use case por ciclo e não têm regra.
- [ ] Api referencia Infrastructure só em `Program.cs`/DI.

### 2.5 Módulo e composição

- [ ] `<Modulo>Module.cs` (Application) registra use cases e services; (Infrastructure) registra adapters e ports.
- [ ] `Program.cs` só compõe módulos e mapeia endpoints.
- [ ] Nenhum módulo lê tabela de outro nem injeta service concreto de outro.

### 2.6 Mobile (somente se o escopo incluir `mobile/`)

- [ ] `features/<modulo>/{api,hooks,components,screens-support}`; telas em `src/app/` finas.
- [ ] Um http-client; query keys por fábrica; hooks por feature.
- [ ] Formulários com zod + react-hook-form; formatação em `lib/`; primitivos de UI compartilhados.
- [ ] `npm run lint` e `npx tsc --noEmit` limpos.

## 3. Cobertura de contrato (obrigatório por fluxo)

Para cada fluxo candidato, registre no auditor:

| Fluxo | Teste que fixa status + JSON (sucesso) | Teste de erro (4xx) | Lacuna? |
| --- | --- | --- | --- |

Onde houver lacuna, o planner abre **Batch 0** com testes de caracterização. Fonte de testes existentes: `backend/Reelix.UnitTests/` (`Api/`, `<Modulo>/`, `Domain/`).

## 4. Classificação de cada módulo

Para cada módulo, produza **uma** classificação:

| Classificação | Critério |
| --- | --- |
| `conforming` | Use cases por fluxo, handlers 1:1, ports estreitos, erros centralizados, sem achado mecânico ou semântico. |
| `non-conforming` | Ao menos uma violação que será corrigida nesta execução. |
| `exception` | Transversal (ex.: `Program.cs`, `Common/`, `ReelixDbContext`) em que o padrão não se aplica; justificativa registrada no plano e no ADR. |
| `out-of-scope` | Fora do escopo pedido (`--slice`, `--scope`); registre o motivo. |

`exception` não é sinônimo de "difícil": módulo pequeno (Authentication) com um fluxo ainda é `non-conforming` se faltar `UseCases/` ou handler.

## 5. Severidade

| Severidade | Uso |
| --- | --- |
| `high` | Quebra a direção das dependências, mistura fluxos (service gordo, Contracts misto), endpoint com vários services, regra de negócio em handler/repositório, vazamento de HTTP/EF. Corrigir nesta execução. |
| `medium` | Relógio global, TryGet/erro inline repetidos, Api importando Domain, arquivo grande, suspeita (h) confirmada. Corrigir ou justificar. |
| `low` | Nome/local de arquivo, use case órfão, código morto aparente. Corrigir se barato; registrar se não. |

## 6. Evidência obrigatória

Toda violação no relatório do auditor precisa de:

- `arquivo:linha`;
- trecho ou símbolo que prova a violação;
- regra violada (da tabela acima ou do script);
- por que importa;
- mudança-alvo (extrair use case, criar handler, criar port, mover regra para domain service etc.);
- risco da mudança **para o contrato HTTP**.

Sem evidência, não é achado: é opinião. O planner não planeja em cima de opinião. Suspeitas (h) só viram achado depois de leitura que confirme regra de negócio.

## 7. Falsos positivos comuns

- `switch` de **mapeamento puro** em repositório (ex.: ordenação/projeção EF) não é regra de negócio; só vira achado se decidir comportamento de negócio (tipo de evento, tipo de regra de conquista).
- `*Service.cs` com ≤ 5 métodos e capacidade reutilizável (cursor, cálculo) é permitido.
- `UtcNow` em adapter para `Retry-After`/expiração de cache ainda deve usar `TimeProvider`, mas é `medium` de baixo risco; não mude semântica.
- `HttpRequestException` em **Infrastructure** é esperado (adapter); só é violação em Application.
- `Reelix.Domain` em Api dentro de testes não conta (testes ignorados por padrão).
- `Program.cs`/arquivos `*DependencyInjection.cs`/`*Module.cs` da Api podem referenciar Infrastructure.
- Endpoint com um único service **mas** com regra de negócio é violação **semântica**, mesmo que o script não aponte.

## 8. Critério de saída do auditor

- [ ] Rodou o script e anexou a saída bruta.
- [ ] Leu cada módulo e classificou todos.
- [ ] Toda violação tem evidência e mudança-alvo; suspeitas confirmadas ou descartadas.
- [ ] Lacunas de teste de caracterização listadas por fluxo.
- [ ] Exceções têm justificativa.
- [ ] Nenhum arquivo de código foi modificado.
- [ ] `01-audit.md` está no run directory e `state.json` foi atualizado.
