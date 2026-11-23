# Briefs dos 6 agentes — Reelix

> O orquestrador lê este arquivo e envia **um brief por vez** ao subagent, em sequência.
> Todos os agentes trabalham no **mesmo checkout e na mesma branch**. Não crie worktree.
> Substitua os placeholders antes de enviar. O subagent começa sem contexto: o brief precisa ser autossuficiente.

## Placeholders

| Placeholder | Valor |
| --- | --- |
| `<RUN_ID>` | `YYYYMMDD-HHmm` da execução |
| `<RUN_DIR>` | Caminho absoluto de `.claude/refactor/<RUN_ID>` |
| `<SCOPE>` | Escopo da auditoria; padrão `backend` (pode incluir `mobile/src`) |
| `<SLICE>` | Módulo-alvo (`--slice`) ou `todos` |
| `<BASE_BRANCH>` | Branch de destino do PR; padrão `main` |
| `<BRANCH>` | `refactor/slices-<RUN_ID>` |
| `<PLAN_PATH>` | `.plans/refactor-slices-<RUN_ID>-plan.md` |
| `<AUDIT_PATH>` | `<RUN_DIR>/01-audit.md` |
| `<TARGET_ARCH_PATH>` | `.claude/skills/refactor-slices/references/target-architecture.md` |
| `<AUDIT_CHECKLIST_PATH>` | `.claude/skills/refactor-slices/references/audit-checklist.md` |
| `<REPORT_TEMPLATES_PATH>` | `.claude/skills/refactor-slices/references/report-templates.md` |
| `<AUDIT_SCRIPT>` | `.claude/skills/refactor-slices/scripts/audit-slices.mjs` |

## Contexto comum (inclua em todos os briefs)

```text
Você é um subagent da skill refactor-slices do projeto Reelix (backend .NET 10 em backend/,
Reelix.sln; app mobile Expo em mobile/). O objetivo é refatorar as fatias verticais para a
arquitetura-alvo: Api -> Application -> Domain, Infrastructure implementando ports, UM use case
por fluxo (Application/<Modulo>/UseCases/<Fluxo>/<Fluxo>UseCase.cs, método ExecuteAsync) e UM
request handler por endpoint (Api/<Modulo>/Handlers/<Fluxo>Handler.cs, 1:1 handler <-> use case).

Regras que valem para todos os agentes:
- Trabalhe no checkout atual, na branch <BRANCH>. Não crie worktree, não troque de branch,
  não faça merge, rebase, reset, amend, force push ou commit (exceto o Agente 5).
- Leia antes de agir: AGENTS.md, .docs/AGENTS.md, .docs/ARCHITECTURE.md, .docs/DEVELOPMENT.md, .docs/QA.md, .docs/TESTING.md, .docs/RULES.md
  (§4 plano, §5 ADR), .adrs/0001-*.md e, para mobile, mobile/AGENTS.md.
- A fonte de verdade da arquitetura-alvo é <TARGET_ARCH_PATH>.
- O CONTRATO HTTP PÚBLICO É INTOCÁVEL: rotas, status, JSON de request/response, ProblemDetails
  (pt-BR), metadados OpenAPI, auth, rate limit, esquema/migrations. Se faltar teste que fixe o
  contrato de um fluxo, ele precisa de teste de caracterização ANTES da mudança estrutural.
- Não invente comandos, dependências, rotas ou comportamento. Não adicione pacote NuGet/npm.
- Não mascare falha com #pragma warning disable, teste relaxado, [Fact(Skip=...)] ou gate pulado.
- Não edite artefatos gerados (bin/, obj/, TestResults/, artifacts/, node_modules/) nem Migrations/.
- Não leia nem imprima segredos (appsettings com chaves, .env, user-secrets).
- Registre o que realmente executou e viu. Relato sem evidência não conta.
- Ao terminar, escreva seu artefato no run directory e atualize state.json.
```

---

## Agente 1 — Auditor

**Missão:** encontrar, com evidência, todos os módulos que não seguem o padrão e classificar cada um. É somente leitura: não altera código.

**Leia:**
- Contexto comum.
- `<AUDIT_CHECKLIST_PATH>`.
- `<TARGET_ARCH_PATH>`.
- Código de `<SCOPE>` (Domain, Application, Infrastructure, Api e os testes em `backend/Reelix.UnitTests`).

**Faça:**
1. Rode o script e salve a saída bruta:
   ```bash
   node <AUDIT_SCRIPT> --scope <SCOPE> --json > <RUN_DIR>/01-audit.raw.json
   node <AUDIT_SCRIPT> --scope <SCOPE>
   ```
2. Leia cada módulo listado. Confirme ou descarte cada achado do script com leitura do código; confirme ou descarte cada **suspeita** (`repository-business-rule-suspect`).
3. Procure o que o script não vê (checklist §1): regra de negócio em handler/endpoint, use case que orquestra dois fluxos, ports gordos, módulo lendo tabela de outro ou injetando service concreto de outro, GET que escreve (criação preguiçosa de conta/perfil), falta de `CancellationToken`, paginação/cursor duplicados, DTOs de Application servindo de contrato HTTP.
4. Levante a **cobertura de contrato** por fluxo (checklist §3): para cada endpoint, existe teste que fixa status + JSON e o erro 4xx? Liste as lacunas.
5. Classifique cada módulo: `conforming`, `non-conforming`, `exception` ou `out-of-scope`.
6. Para cada violação, registre: arquivo:linha, evidência, regra, por que importa, mudança-alvo e risco para o contrato HTTP.
7. Liste os módulos já conformes (o planner não mexe neles) e as dúvidas bloqueantes. Não invente resposta.
8. Se `<SCOPE>` incluir `mobile/`, audite também a seção 2.6 do checklist e rode (leitura) `npm run lint` e `npx tsc --noEmit` em `mobile/` para registrar o baseline.

**Saída:** `<RUN_DIR>/01-audit.md` no template de `<REPORT_TEMPLATES_PATH>` (seção 01). Inclua tabela de resumo, achados por módulo, lacunas de contrato e exceções justificadas.

**Critério de saída:**
- [ ] Script executado e saída bruta salva.
- [ ] Todo módulo de `<SCOPE>` classificado; suspeitas confirmadas/descartadas.
- [ ] Toda violação com evidência, mudança-alvo e risco de contrato.
- [ ] Lacunas de teste de caracterização listadas por fluxo.
- [ ] Nenhum arquivo de código modificado (`git status` só mostra o que já era ignorado/untracked fora do run directory).
- [ ] `state.json` com `phase: "audit"`, `status: "done"`.

**Pare se:** o escopo não existir, o repositório estiver sujo antes de começar ou houver dúvida bloqueante. Escreva o motivo no artefato e marque `status: "blocked"`.

---

## Agente 2 — Planner

**Missão:** transformar a auditoria em um plano de refatoração executável, ordenado, com mudanças por arquivo, testes de caracterização e critérios de aceite. Não altera código.

**Leia:**
- Contexto comum.
- `<AUDIT_PATH>` e `<RUN_DIR>/01-audit.raw.json`.
- `<TARGET_ARCH_PATH>`.
- Código e testes dos módulos `non-conforming`; `.specs/` do módulo, se existirem.

**Faça:**
1. Confirme cada achado no código; se a auditoria estiver errada, registre a correção no plano (não altere o artefato do auditor).
2. Ordene os módulos por risco/dependência. Recomendação: comece por um módulo pequeno que prove o padrão (ex.: Authentication ou Gamification); deixe os grandes (Lists, Tracking, Social) para depois; **Catalog** vem cedo se outros módulos precisarem do port "título existe" (`ICatalogTitleLookup`). Respeite dependências entre módulos (port antes de consumidores).
3. Para cada módulo, escreva **Batch 0 (caracterização)** quando houver lacuna: testes que fixam status + JSON (sucesso e erro) rodando **verdes sobre o código antigo** (WebApplicationFactory/testes existentes em `Reelix.UnitTests/Api`). Só depois, os batches estruturais.
4. Para cada batch estrutural, escreva:
   - objetivo;
   - arquivos a criar/mover/dividir/apagar;
   - mapeamento endpoint → handler → use case → ports → adapters → testes;
   - ports a criar (estreitos, leitura/escrita separadas) e erros de aplicação a tipar;
   - decisão **Result vs exceções** para o módulo (uma só) e como o mapeador central preserva o `ProblemDetails` atual;
   - `TimeProvider` (onde entra) e regras que saem do repositório para use case/domain service;
   - DI do módulo (`<Modulo>Module.cs`);
   - mudanças em testes (só namespace/construtor/DI; asserção não muda);
   - riscos, rollback e critério de aceite verificável.
5. Inclua a seção de **contrato preservado** (rotas, status, JSON, ProblemDetails, OpenAPI, auth, rate limit, migrations, provedores, dependências).
6. Inclua **exceções** (módulos `exception` e por quê) e a estratégia de testes por batch: teste focado durante a implementação, gates completos no Agente 4.
7. Inclua a atualização de docs/ADR: `.docs/ARCHITECTURE.md` (e `.docs/DEVELOPMENT.md`) e `.adrs/NNNN-use-case-por-fluxo-e-handler-por-endpoint.md`.
8. Se incluir mobile, um batch por feature seguindo a seção 12 da arquitetura-alvo; só `npm run lint` e `npx tsc --noEmit` como gates.
9. Defina a ordem de execução e o que fazer se um batch falhar.

**Saída:** `<PLAN_PATH>` no template da seção 02 de `<REPORT_TEMPLATES_PATH>`. Salve uma cópia em `<RUN_DIR>/02-plan.md`.

**Critério de saída:**
- [ ] Todo achado `high`/`medium` tem batch ou justificativa.
- [ ] Todo fluxo sem cobertura de contrato tem Batch 0.
- [ ] Todo batch tem arquivos, mapeamento 1:1 e critério de aceite.
- [ ] Nenhuma mudança de contrato HTTP no plano.
- [ ] Ordem e rollback definidos.
- [ ] Nenhum código alterado.
- [ ] `state.json` com `phase: "plan"`, `status: "done"`.

**Pare se:** a auditoria não permitir um plano seguro (achado ambíguo, decisão de produto como remover escrita de um GET, dependência externa). Escreva as perguntas bloqueantes e marque `status: "blocked"`.

---

## Agente 3 — Implementer

**Missão:** executar o plano batch a batch, preservando o contrato, e deixar a árvore pronta para QA. Não commita.

**Leia:**
- Contexto comum.
- `<PLAN_PATH>` e `<RUN_DIR>/02-plan.md`.
- `<TARGET_ARCH_PATH>`.
- Código, testes e DI dos módulos do batch.

**Faça:**
1. Antes de cada batch, releia o batch e os testes que o cobrem. Execute o **Batch 0** primeiro e confirme que os testes de caracterização passam sobre o código antigo.
2. Execute na ordem do plano. Para cada fluxo:
   - crie `Application/<Modulo>/UseCases/<Fluxo>/<Fluxo>UseCase.cs` (uma classe, um `ExecuteAsync`) com `<Fluxo>Command|Query` e `<Fluxo>Result` ao lado;
   - crie ports estreitos em `Ports/` e erros em `Errors/`; desmembre `*Contracts.cs`;
   - crie `Api/<Modulo>/Handlers/<Fluxo>Handler.cs` com **um** use case injetado, `ICurrentUser` para o usuário, request/response DTOs da Api (nunca tipos de Domain);
   - reduza `<Modulo>Endpoints.cs` a `MapGroup` + rotas + metadados idênticos;
   - mova regra de repositório para use case/domain service; relógio via `TimeProvider` (o Domain recebe o instante);
   - adapters de provedor traduzem erro de HTTP para erro de aplicação;
   - registre tudo em `<Modulo>Module.cs`; apague o service/contracts antigo só quando nada mais o usar;
   - atualize namespaces/construtores/DI dos testes; **não mude asserções**.
3. Crie `ICurrentUser` e o mapeador único de erro (`Api/Common/`) no primeiro batch que precisar; reutilize nos seguintes. O mapeador preserva status, `title`, `detail` e extensões atuais.
4. Após cada fluxo, rode a verificação mais barata:
   ```bash
   dotnet build Reelix.sln -c Release --no-restore
   dotnet test Reelix.sln -c Release --no-build --filter "FullyQualifiedName~<Modulo>"
   ```
   (se `--no-restore` falhar por falta de restore, rode `dotnet restore Reelix.sln` uma vez).
5. Atualize docs/ADR quando o padrão for aplicado pela primeira vez.
6. Se um teste falhar por **comportamento** (não por namespace/DI), **pare o batch**, registre a divergência e não force o teste a passar.
7. Se um batch não puder ser concluído, registre `blocked` e não avance para outro que dependa dele.
8. Não toque em `Migrations/`, `appsettings*.json` com segredos nem em pacotes. Não commite, não faça push, não abra PR.

**Saída:** `<RUN_DIR>/03-execution.md` no template de `<REPORT_TEMPLATES_PATH>` (seção 03).

**Critério de saída:**
- [ ] Todos os batches `done` ou `blocked` com motivo.
- [ ] Nenhum endpoint sem handler 1:1; nenhum use case compartilhado por dois handlers.
- [ ] Nenhum use case com mais de um fluxo/`ExecuteAsync`.
- [ ] Testes alterados só em namespace/construtor/DI (e testes novos de caracterização).
- [ ] Docs/ADR atualizados.
- [ ] `git status` lista apenas mudanças esperadas.
- [ ] `state.json` com `phase: "implement"`, `status: "done"` ou `"blocked"`.

**Pare se:** o plano estiver errado, o contrato HTTP precisar mudar, faltar decisão de produto ou a mudança exigir pacote novo. Não improvise arquitetura fora do plano.

---

## Agente 4 — QA / Gatekeeper

**Missão:** provar que a refatoração preservou o contrato e que a arquitetura-alvo foi atingida. Pode corrigir falhas mecânicas causadas pela refatoração; não pode relaxar gates.

**Leia:**
- Contexto comum.
- `<RUN_DIR>/03-execution.md`, `<PLAN_PATH>`.
- `<AUDIT_CHECKLIST_PATH>` (gate de arquitetura).
- `.github/workflows/ci.yml` (o CI real: restore, test, migration script).

**Faça:**
1. **Gate de arquitetura:**
   ```bash
   node <AUDIT_SCRIPT> --scope <SCOPE> --strict [--allow <regras documentadas no plano>]
   ```
   Zero achado é o esperado. Exceções do plano entram em `--allow` e são registradas uma a uma. Complemente com leitura: handlers 1:1 (um use case por handler, um handler por rota) e nenhum `catch`/`TryGetUserId` em `Endpoints`/`Handlers`.
2. **Gate de contrato:** revise `git diff` procurando mudança em rota, método, status, DTO/JSON, `ProblemDetails`, `WithName/Produces*`, auth, rate limit, esquema, `Migrations/`, provedores ou pacotes. Confirme `git diff --stat -- backend/**/Migrations` vazio e `*.csproj` sem `PackageReference` novo. Os testes de caracterização devem ter passado antes **e** depois sem edição de asserção. Se houver, é falha.
3. **Gates de QA (backend)**, nesta ordem, da raiz do repositório:
   ```bash
   dotnet restore Reelix.sln --source https://api.nuget.org/v3/index.json -warnaserror:NU1903,NU1904
   dotnet build Reelix.sln -c Release --no-restore
   dotnet test Reelix.sln -c Release --no-restore
   ```
   `dotnet test` inclui os testes de integração com Postgres via Testcontainers (`PostgresAuthorizationIntegrationTests`): exigem Docker (`docker info`). Sem Docker, **não declare sucesso**: `blocked` e sem commit.
4. **Gates de QA (mobile)**, se o escopo incluiu `mobile/`:
   ```bash
   cd mobile && npm run lint && npx tsc --noEmit
   ```
5. Falha mecânica (namespace, DI, construtor em teste) causada pela refatoração: corrija e rode de novo. Falha de **comportamento/asserção/contrato**: **pare** e reporte; não altere asserção.
6. Verifique que `git status` não contém segredo, `.env`, `bin/`, `obj/`, `TestResults/` nem `.claude/refactor/`.

**Saída:** `<RUN_DIR>/04-gates.md` no template de `<REPORT_TEMPLATES_PATH>` (seção 04).

**Critério de saída:**
- [ ] Gate de arquitetura sem achado (ou só exceções documentadas).
- [ ] `dotnet build` e `dotnet test` em Release passando (e mobile, se aplicável).
- [ ] Nenhuma mudança de contrato; migrations intactas.
- [ ] `state.json` com `phase: "qa"`, `status: "done"` ou `"blocked"`.

**Pare se:** qualquer gate obrigatório falhar por comportamento ou o Docker/infra necessário não estiver disponível. O Agente 5 não pode rodar com `status` diferente de `done`.

---

## Agente 5 — Commit & Push

**Missão:** commitar e publicar a refatoração **somente** depois de o QA aprovar, usando a skill `commit-and-push`.

**Leia:**
- Contexto comum.
- `<RUN_DIR>/04-gates.md` (precisa estar `done` com gates verdes).
- `.claude/skills/commit-and-push/SKILL.md`.

**Faça:**
1. Confirme que `<RUN_DIR>/04-gates.md` existe e não tem gate obrigatório falhando. Se houver, pare.
2. Carregue a skill `commit-and-push` e siga o contrato dela (status → análise → segurança → mensagem → add → commit → push).
3. A branch deve ser `<BRANCH>`; não crie outra.
4. **Nunca** inclua `.claude/refactor/` no commit (run directory). Confira com `git status` antes do `add`; como `.claude/skills/` pode estar untracked, adicione apenas os caminhos do refactor (`backend/`, `mobile/`, `.docs/`, `.adrs/`, `.plans/`) em vez de `git add -A`.
5. Conventional Commits: `refactor(api): ...` (ou o tipo/escopo que o diff justificar: `refactor(mobile): ...`, `test(api): ...` para o Batch 0 se for commit separado), descrição em pt-BR, corpo com o porquê e os módulos refatorados, footer `Refs: <PLAN_PATH>` e a linha `Co-Authored-By` exigida pela skill/ambiente.
6. Push explícito para `origin` na branch `<BRANCH>`. Nunca `--force`, `--amend`, `--no-verify`.
7. Não abra PR; isso é o Agente 6.

**Saída:** `<RUN_DIR>/05-commit.md` no template de `<REPORT_TEMPLATES_PATH>` (seção 05).

**Critério de saída:**
- [ ] Commit existe (`git log -1`).
- [ ] Push feito para `origin/<BRANCH>` (ou pendência explícita).
- [ ] Nenhum segredo/artefato/run directory no commit.
- [ ] `state.json` com `phase: "commit"`, `status: "done"`.

**Pare se:** QA não estiver `done`, a árvore estiver em conflito, o remoto não for `github.com/brunolucildo/reelix` ou o push for rejeitado. Registre o motivo; não contorne.

---

## Agente 6 — PR

**Missão:** abrir o Pull Request da branch `<BRANCH>` para `<BASE_BRANCH>` com o resumo e as evidências de QA. Não faz merge.

**Leia:**
- Contexto comum.
- `<RUN_DIR>/05-commit.md`, `<RUN_DIR>/04-gates.md`, `<PLAN_PATH>`.
- `.claude/skills/commit-and-push/SKILL.md` (destino e branch).

**Faça:**
1. Confirme que o commit e o push existem. Se não, pare.
2. Verifique `gh auth status` e o remoto (`github.com/brunolucildo/reelix`). Se `gh` não estiver autenticado, escreva o corpo em `<RUN_DIR>/06-pr-body.md` e registre a pendência com o comando exato.
3. Verifique se já existe PR para a branch:
   ```bash
   gh pr view <BRANCH> --json url,state,title
   ```
   Se existir, reutilize e reporte a URL; não crie duplicado.
4. Monte o corpo do PR com:
   - resumo do problema e da solução;
   - módulos refatorados e o padrão aplicado (camadas, use case por fluxo, handler 1:1, ports estreitos, erro centralizado, `TimeProvider`);
   - tabela de gates com resultado real (`dotnet build`, `dotnet test`, gate de arquitetura, mobile se houver);
   - confirmação de **contrato HTTP preservado** (rotas, status, JSON, ProblemDetails) e testes de caracterização adicionados;
   - link do plano `<PLAN_PATH>` e do ADR;
   - riscos, exceções e o que não rodou.
5. Abra o PR:
   ```bash
   gh pr create --base <BASE_BRANCH> --head <BRANCH> --title "refactor(api): ..." --body-file <RUN_DIR>/06-pr-body.md
   ```
6. Não faça merge, não altere a base, não force push, não feche PR.

**Saída:** `<RUN_DIR>/06-pr.md` no template de `<REPORT_TEMPLATES_PATH>` (seção 06).

**Critério de saída:**
- [ ] PR aberto ou pendência explícita com comando.
- [ ] Corpo do PR contém gates reais e link do plano.
- [ ] Nenhum merge/force.
- [ ] `state.json` com `phase: "pr"`, `status: "done"`.

**Pare se:** não houver commit/push, `gh` não estiver autenticado ou a branch base não existir. Registre a pendência; não invente URL.
