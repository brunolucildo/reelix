---
name: refactor-slices
description: Audita e refatora o backend .NET do Reelix (Clean Architecture por fatia vertical — Api → Application → Domain, Infrastructure implementando ports) para um use case por fluxo e um request handler por endpoint (1:1 handler ↔ use case), sem quebrar o contrato HTTP público. Executa um pipeline de seis subagents em sequência — auditoria (com script mecânico), plano, implementação, gates (dotnet build/test), commit-and-push e abertura de PR; inclui uma seção para o mobile Expo. Use esta skill sempre que o usuário disser "refatorar slices", "refatorar vertical slices", "slices fora do padrão", "aplicar Clean Architecture", "arquitetura em camadas", "arquitetura hexagonal", "request handler pattern", "handler por endpoint", "use case por fluxo", "quebrar o service gordo", "auditar slices", "corrigir arquitetura dos módulos", "refactor slices", "refactor vertical slices", "layered architecture", ou pedir para encontrar e corrigir módulos que não seguem a arquitetura do projeto — mesmo que não mencione a skill pelo nome.
argument-hint: "[--scope <path>] [--slice <modulo>] [--base <branch>] [--dry-run]"
---

# Refactor Slices (Reelix)

Refatorar as fatias verticais do backend .NET (e, opcionalmente, do mobile) para a arquitetura-alvo, sem mudar comportamento: **auditoria → plano → implementação → gates → commit/push → PR**, cada etapa em um **subagent separado**.

A skill transforma uma arquitetura inconsistente em um padrão repetível: `Domain` puro, `Application` com **um use case por fluxo** (`ExecuteAsync`), `Infrastructure` implementando ports estreitos e `Api` com **um request handler por endpoint**. O vínculo é **1:1**: cada endpoint tem um handler; cada handler injeta exatamente um use case.

Hoje o código tem: um `*Service` por módulo com 8–15 métodos, `*Contracts.cs` misturando DTOs/ports/exceções, `*Endpoints.cs` com métodos estáticos injetando o service direto (e repetindo `TryGetUserId`, parse de enum e exceção→`ProblemDetails`), repositórios gordos com regra de negócio (`GamificationRepository`, `NotificationsRepository`), uso de `DateTime.UtcNow` sem `TimeProvider`, DTOs de Application como contrato HTTP e Api importando tipos de Domain. É isso que a skill corrige.

## Contrato da skill

- **Entrada:** nenhuma obrigatória. Opções:
  - `--scope <path>` — diretório auditado; padrão `backend`. Para incluir o mobile, passe `backend` e `mobile/src` (o script mecânico cobre só `.cs`).
  - `--slice <modulo>` — limita plano/implementação a um módulo (Catalog, Tracking, Lists, Ratings, Social, Notifications, Gamification, Users, Authentication). A auditoria continua lendo o escopo inteiro para contexto.
  - `--base <branch>` — branch de destino do PR; padrão `main`.
  - `--dry-run` — para depois do plano (Agentes 1 e 2); não altera código, não commita, não abre PR.
- **Saída:** código refatorado, testes de caracterização, plano em `.plans/refactor-slices-<run-id>-plan.md`, docs/ADR atualizados, commit, push, PR aberto e relatório consolidado. Artefatos de execução em `.claude/refactor/<run-id>/`.
- **Documentação:** canônica em `.docs/`; planos em `.plans/`; ADRs em `.adrs/` (template em `.docs/RULES.md` §4–5).
- **Pipeline:** seis subagents, **em sequência**, no mesmo checkout e na mesma branch. O orquestrador não codifica: ele lança, verifica artefatos e decide se avança.
- **Não faz:** mudar o contrato HTTP, inventar requisitos, adicionar pacote, relaxar gate, commitar na `main`, fazer force push/amend/rebase, mesclar PR, criar worktree, editar `Migrations/` ou artefatos gerados.
- **Quando parar:** árvore suja, baseline quebrado, dúvida bloqueante, gate falhando por comportamento, Docker indisponível para os testes de integração, `gh` sem autenticação. Pare e explique; não contorne.

## Princípios

1. **Refatoração é estrutural.** Rotas, status, JSON, `ProblemDetails` (pt-BR), metadados OpenAPI, auth, rate limit, esquema do banco e chamadas a provedores continuam idênticos. Se um teste precisar mudar de asserção, o comportamento mudou: pare.
2. **Contrato HTTP nunca quebra.** Sem teste que fixe status + JSON de um fluxo, **escreva teste de caracterização antes** (Batch 0, verde sobre o código antigo). Nenhum fluxo é refatorado sem essa rede.
3. **Um fluxo, uma classe.** Use case não agrega fluxos. `Services/` só para capacidade reutilizável (cursor, emissão), nunca para disfarçar um fluxo.
4. **Um endpoint, um handler.** Endpoints só mapeiam rotas; cada handler injeta um único use case, lê o usuário por uma abstração única e deixa o erro ao mapeador central.
5. **Regra de negócio fica no Domain/Application.** Nada de regra em repositório, handler ou DTO; relógio por `TimeProvider`.
6. **Agentes sequenciais, mesma árvore.** Cada agente depende do artefato e das mudanças do anterior. Sem worktree.
7. **Evidência vence relato.** O orquestrador lê o artefato, o `git status`, o `git log` e a saída real dos gates. "O subagent disse que passou" não é prova.
8. **Gate não se negocia.** Falha de build, teste, arquitetura ou contrato bloqueia commit e PR. Exceção pré-existente só vale com evidência e registro.
9. **Documentação acompanha o código.** Ao aplicar o padrão pela primeira vez, atualize `.docs/ARCHITECTURE.md` (e `.docs/DEVELOPMENT.md`) e crie o ADR do padrão.

## 0. Pré-voo (orquestrador)

Leia, nesta ordem: `AGENTS.md` da raiz, `.docs/AGENTS.md`, `.docs/ARCHITECTURE.md`, `.docs/DEVELOPMENT.md`, `.docs/QA.md` §3, `.docs/TESTING.md`, `.docs/RULES.md` §4–5, `.adrs/0001-*`e `.github/workflows/ci.yml`. Se o escopo incluir `mobile/`, leia `mobile/AGENTS.md`. Leia também [`references/target-architecture.md`](references/target-architecture.md) e [`references/agent-briefs.md`](references/agent-briefs.md).

1. **Resolva as opções.** `--scope` existe e é diretório? `--slice` corresponde a um módulo (pasta em `Reelix.Application/` ou `Reelix.Api/`)? `--base` existe no remoto? Em dúvida, pare e pergunte.
2. **Estado do repositório:**
   ```bash
   git rev-parse --is-inside-work-tree
   git status --porcelain=v1 -b
   git branch --show-current
   git remote get-url --push origin
   gh auth status
   ```
   - Árvore suja (fora do que já é untracked por design, como `.claude/`), conflito ou operação git em andamento: **pare**. O pipeline parte de um HEAD limpo.
   - `origin` precisa ser `github.com/brunolucildo/reelix`. Outro destino: pare.
   - `gh` sem autenticação: o pipeline pode seguir até o commit, mas o PR fica pendente; avise no pré-voo.
3. **Baseline antes de tocar em nada.** Da raiz do repositório, rode e registre:
   ```bash
   dotnet restore Reelix.sln --source https://api.nuget.org/v3/index.json -warnaserror:NU1903,NU1904
   dotnet build Reelix.sln -c Release --no-restore
   dotnet test Reelix.sln -c Release --no-restore
   ```
   Se o baseline já falhar, pare: não atribua a falha à refatoração. `dotnet test` inclui testes de integração com Postgres via Testcontainers; verifique `docker info`. Sem Docker, o pipeline para no Agente 4 e não commita; avise no pré-voo.
   Se o escopo incluir `mobile/`, registre também (dentro de `mobile/`, com `node_modules` instalado):
   ```bash
   npm run lint
   npx tsc --noEmit
   ```
4. **Rode o auditor mecânico (leitura)** para dimensionar o trabalho e mostrá-lo na confirmação:
   ```bash
   node .claude/skills/refactor-slices/scripts/audit-slices.mjs --scope backend
   ```
5. **Crie a branch e o run directory:**
   ```bash
   git switch -c refactor/slices-<run-id>
   mkdir -p .claude/refactor/<run-id>
   ```
   `<run-id>` = `YYYYMMDD-HHmm`. A branch parte do `HEAD` atual. Se já existir, pare e pergunte. O run directory **nunca** entra em commit.
6. **Crie `state.json`** no formato de [`references/report-templates.md`](references/report-templates.md).
7. **Confirme o escopo.** Mostre em 5–10 linhas: branch, escopo, módulos que o script aponta, baseline, Docker disponível, se o PR será aberto. Só peça confirmação se houver decisão irreversível, pacote novo, exceção de módulo ou infra ausente.

## 1. Arquitetura-alvo (resumo)

| Projeto | Contém | Não contém |
| --- | --- | --- |
| `Reelix.Domain` | Entidades com invariantes, value objects, domain services puros | EF Core, ASP.NET, HTTP, relógio global, outros projetos |
| `Reelix.Application` | `UseCases/<Fluxo>/<Fluxo>UseCase.cs` (um `ExecuteAsync`) com Command/Query/Result ao lado, `Ports/` estreitos, `Errors/` tipados, `Services/` de capacidade reutilizável | HTTP, EF Core, `Infrastructure`, `Api`, `HttpStatusCode`/`HttpRequestException` |
| `Reelix.Infrastructure` | Repositórios por port, EF Configurations em `Persistence/<Modulo>/`, adapters de provedores (TMDB, OMDb, OpenAI, Expo), DI por módulo | Regra de negócio, `Api`, dependência em service concreto de outro módulo |
| `Reelix.Api` | `<Modulo>Endpoints.cs` só com rotas/metadados, `Handlers/<Fluxo>Handler.cs` 1:1, DTOs de request/response, `ICurrentUser`, mapeador único de erro | Regra de negócio, tipos de Domain, `catch` inline, referência a Infrastructure fora do composition root |

Layout, exemplos de use case/handler, fronteiras entre módulos (ports estreitos), regras de qualidade (`CancellationToken`, GET sem escrita, paginação e cursor únicos), contrato HTTP intocável e a seção do mobile estão em [`references/target-architecture.md`](references/target-architecture.md). O auditor, o planner e o implementer leem esse arquivo.

## 2. Pipeline de 6 agentes

| # | Agente | Missão | Artefato | Escreve código? |
| --- | --- | --- | --- | --- |
| 1 | Auditor | Encontrar módulos fora do padrão, com evidência e lacunas de contrato | `01-audit.md` + `01-audit.raw.json` | Não |
| 2 | Planner | Transformar achados em plano executável, com Batch 0 de caracterização | `02-plan.md` + `.plans/refactor-slices-<run-id>-plan.md` | Não |
| 3 | Implementer | Executar o plano batch a batch, preservando o contrato | `03-execution.md` | Sim |
| 4 | QA | Rodar gate de arquitetura, contrato e gates de QA | `04-gates.md` | Só correção mecânica |
| 5 | Commit & Push | Commitar e publicar pela skill `commit-and-push` | `05-commit.md` | Não |
| 6 | PR | Abrir o Pull Request com resumo e evidências | `06-pr.md` + `06-pr-body.md` | Não |

Os briefs completos, com placeholders e critérios de saída, estão em [`references/agent-briefs.md`](references/agent-briefs.md). **Use-os literalmente**, preenchendo os placeholders; não improvise um brief mais curto.

### 2.1 Como lançar

Use a ferramenta de subagent do harness (`Agent`/`Task` no Claude Code) com:

- `subagent_type`: `general-purpose` — precisa de leitura, edição e shell.
- `description`: `refactor-slices — Agente N (<fase>)`.
- `prompt`: o brief do agente, preenchido com `<RUN_ID>`, `<RUN_DIR>`, `<SCOPE>`, `<SLICE>`, `<BASE_BRANCH>`, `<BRANCH>`, `<PLAN_PATH>`, `<AUDIT_PATH>` e caminhos das referências e do script.
- Foreground (`run_in_background` falso). O próximo agente depende do anterior; não há paralelismo.
- **Sem** `isolation: worktree`. Todos trabalham no mesmo checkout e na mesma branch.

Depois de cada agente:

1. Leia o artefato e confirme que as seções obrigatórias existem e não estão vazias.
2. Leia `state.json`; a fase deve estar `done` (ou `blocked`/`failed`, que interrompem o pipeline).
3. Verifique o efeito real: `git status` para Agente 3, `git log -1` para Agente 5, `gh pr view` para Agente 6.
4. Atualize `state.json` e emita uma linha de progresso:
   ```text
   [refactor-slices] audit done · plan done · implement running · qa pending · commit pending · pr pending
   ```
5. Só então lance o próximo agente. Se o artefato estiver incompleto, retome o **mesmo** subagent com a correção; não pule etapa.

### 2.2 Sequência

1. **Auditor** → `01-audit.md`. Se todos os módulos forem `conforming`/`exception`, pare e reporte: não há o que refatorar.
2. **Planner** → `02-plan.md` + plano versionado. Se o plano tiver dúvida bloqueante (ex.: remover a escrita de um GET muda o contrato), pare e pergunte antes de implementar.
3. **Implementer** → `03-execution.md`. Se um batch ficar `blocked`, pare; não rode QA em cima de implementação incompleta.
4. **QA** → `04-gates.md`. Se `blocked`/`failed`, pare; não commite. Correção mecânica é permitida; mudança de comportamento não.
5. **Commit & Push** → `05-commit.md`. Só com QA `done`. Se o push falhar, pare e reporte.
6. **PR** → `06-pr.md`. Se `gh` não estiver autenticado, deixe o corpo em `06-pr-body.md` e reporte a pendência com o comando exato.

`--dry-run`: rode apenas 1 e 2, entregue o relatório e pare.

## 3. Run directory e estado

```text
.claude/refactor/<run-id>/
  state.json
  01-audit.md
  01-audit.raw.json
  02-plan.md
  03-execution.md
  04-gates.md
  05-commit.md
  06-pr.md
  06-pr-body.md
  final-report.md
```

- `.claude/refactor/` não entra em commit (o `.gitignore` já o cobre). Como `.claude/skills/` pode estar não rastreado, o Agente 5 adiciona caminhos explícitos (`backend/`, `mobile/`, `.docs/`, `.adrs/`, `.plans/`) em vez de `git add -A`.
- O plano **também** vive em `.plans/refactor-slices-<run-id>-plan.md` e é commitado.
- O ADR e as atualizações de docs são commitados pelo Agente 5.
- O orquestrador é o único que escreve `state.json`; cada agente pode atualizar a própria fase, mas o orquestrador reconcilia.

## 4. Gates

O Agente 4 roda, nesta ordem:

1. **Arquitetura:**
   ```bash
   node .claude/skills/refactor-slices/scripts/audit-slices.mjs --scope <scope> --strict [--allow <regras>]
   ```
   Zero achado (ou só exceções documentadas no plano, passadas em `--allow`). Mais leitura: 1:1 handler ↔ use case ↔ rota; nenhum `catch`/`TryGetUserId` em `Endpoints`/`Handlers`.
2. **Contrato:** `git diff` revisado para rotas, status, JSON, `ProblemDetails`, `WithName/Produces*`, auth, rate limit, esquema, `Migrations/` (deve estar intacto), provedores e pacotes. Os testes de caracterização passam **sem edição de asserção**. Nada pode mudar.
3. **QA do backend**, da raiz:
   ```bash
   dotnet build Reelix.sln -c Release --no-restore
   dotnet test Reelix.sln -c Release --no-restore
   ```
   Os testes de integração (Testcontainers/Postgres, `PostgresAuthorizationIntegrationTests`) exigem Docker. Sem Docker, `blocked`; sem commit.
4. **QA do mobile** (se no escopo), dentro de `mobile/`:
   ```bash
   npm run lint
   npx tsc --noEmit
   ```
5. **Higiene:** nenhum segredo, `.env`, `bin/`, `obj/`, `TestResults/` ou `.claude/refactor/` em `git status`.

Regras:
- Falha mecânica (namespace, DI, construtor em teste) pode ser corrigida pelo Agente 4 e o gate rodado de novo.
- Falha de comportamento, asserção ou contrato: **pare** e reporte. Não relaxe teste, não use `[Fact(Skip)]`/`#pragma warning disable`.
- Falha pré-existente fora do escopo: registre com evidência; não mascare.

## 5. Commit, push e PR

- **Agente 5** carrega a skill `commit-and-push` e segue o contrato dela. Branch `<branch>`, Conventional Commits (`refactor(api): ...`; `refactor(mobile): ...` para o mobile), descrição em pt-BR, corpo com o porquê e os módulos, `Refs: <plan-path>` e a linha de coautoria exigida pela skill/ambiente. Push explícito para `origin` (`github.com/brunolucildo/reelix`). Nunca `--force`, `--amend`, `--no-verify`.
- **Agente 6** usa `gh pr create --base <base> --head <branch> --title ... --body-file <run-dir>/06-pr-body.md`. O corpo inclui resumo, módulos, padrão aplicado, tabela de gates real, confirmação de contrato preservado, link do plano/ADR e riscos/exceções. Se o PR já existir, reutilize. Não faça merge.
- Se `gh` não estiver autenticado, o PR fica pendente com o comando exato; o commit/push continua válido.

## 6. Relatório final

Preencha o template de [`references/report-templates.md`](references/report-templates.md) (seção "Relatório final do orquestrador") com evidência real:

- branch, commit, push e PR;
- módulos antes/depois (services/métodos → use cases/handlers), ports e erros tipados;
- achados do script antes → depois, por regra;
- tabela de gates com resultado e evidência; contrato HTTP preservado e prova;
- exceções, desvios e o que não rodou;
- riscos residuais e próximo passo.

Se o pipeline parou, diga exatamente em qual agente, com qual artefato e qual decisão é necessária. Não declare sucesso sem ter lido o artefato, o `git log` e a saída dos gates.

## Guardrails

- Não mude o contrato HTTP: rotas, status, JSON, `ProblemDetails`, OpenAPI, auth, rate limit, esquema e provedores são intocáveis. Sem teste de caracterização, escreva-o antes.
- Não crie, compartilhe ou reutilize um use case entre dois handlers; 1:1 é regra, não sugestão.
- Não deixe `Endpoints.cs` com lógica nem handler com mais de um use case injetado.
- Não deixe regra de negócio em repositório/Infrastructure nem `DateTime.UtcNow` em Domain/Application/Infrastructure.
- Não adicione pacote NuGet/npm, `#pragma warning disable`, `[Fact(Skip)]` ou teste relaxado.
- Não edite `Migrations/`, `bin/`, `obj/`, `TestResults/`, `artifacts/`, `node_modules/`.
- Não commite na `main`, não faça force push/amend/rebase/reset, não mescle PR.
- Não crie worktree: os agentes são sequenciais e compartilham a árvore.
- Não declare gate, commit, push ou PR sem ter visto a saída real.
- Sem `.git`, árvore suja, baseline quebrado, Docker ausente ou gate falhando: **pare e explique**.
