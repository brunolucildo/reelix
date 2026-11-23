# Templates dos artefatos da execução — Reelix

> O run directory é `.claude/refactor/<run-id>/` (nunca commitado).
> O plano também é salvo em `.plans/refactor-slices-<run-id>-plan.md` (versionado); ADRs vão em `.adrs/`; a documentação canônica está em `.docs/`.
> Use estes templates literalmente; o orquestrador valida a presença das seções.

## `state.json`

O orquestrador cria e mantém este arquivo. Cada agente só atualiza `phase` e `status` da sua fase.

```json
{
  "runId": "YYYYMMDD-HHmm",
  "scope": "backend",
  "slice": "todos",
  "baseBranch": "main",
  "branch": "refactor/slices-YYYYMMDD-HHmm",
  "planPath": ".plans/refactor-slices-YYYYMMDD-HHmm-plan.md",
  "startedAt": "ISO-8601",
  "baseline": { "build": "pending", "test": "pending", "mobileLint": "n/a", "mobileTsc": "n/a" },
  "phases": {
    "audit": { "status": "pending", "artifact": "01-audit.md" },
    "plan": { "status": "pending", "artifact": "02-plan.md" },
    "implement": { "status": "pending", "artifact": "03-execution.md" },
    "qa": { "status": "pending", "artifact": "04-gates.md" },
    "commit": { "status": "pending", "artifact": "05-commit.md" },
    "pr": { "status": "pending", "artifact": "06-pr.md" }
  }
}
```

Status por fase: `pending`, `running`, `done`, `blocked`, `failed`.

## `01-audit.md` — Auditor

```markdown
# Auditoria de slices — <run-id>

- **Escopo:** `<scope>`
- **Data:** aaaa-mm-dd
- **Método:** script `audit-slices.mjs` + leitura semântica
- **Saída bruta:** `01-audit.raw.json`

## Resumo

| Módulo | Services (métodos públicos) | Use cases | Endpoints | Handlers | Classificação | Achados high/medium/low |
| --- | --- | --- | --- | --- | --- | --- |

**Total:** N módulos · N conformes · N não conformes · N exceções · N fora do escopo.

## Achados por regra (script)

| Regra | Id | Achados | Ocorrências | Confirmados | Descartados (falso positivo) |
| --- | --- | --- | --- | --- | --- |

## Achados por módulo

### <modulo> — `non-conforming`

| # | Severidade | Arquivo:linha | Regra | Evidência | Por que importa | Mudança-alvo | Risco p/ contrato HTTP |
| --- | --- | --- | --- | --- | --- | --- | --- |

#### Observações semânticas

- Regra de negócio em handler/endpoint/repositório:
- Fronteiras entre módulos (tabela de outro módulo, service concreto de outro):
- GET com escrita (criação preguiçosa de conta/perfil):
- `CancellationToken`, paginação, cursor:

### <modulo> — `conforming`

- Nenhum achado. Não mexer.

### <modulo> — `exception`

- **Motivo:** ...
- **Por que o padrão não se aplica:** ...

## Cobertura de contrato por fluxo

| Módulo | Fluxo/endpoint | Teste sucesso (status+JSON) | Teste erro 4xx | Lacuna → Batch 0? |
| --- | --- | --- | --- | --- |

## Mobile (se no escopo)

| Feature | Achado | Evidência | Mudança-alvo |
| --- | --- | --- | --- |

## Fora do escopo

- ...

## Dúvidas bloqueantes

| Pergunta | Impacto | Bloqueante? | Quem decide |
| --- | --- | --- | --- |
```

## `02-plan.md` / `.plans/refactor-slices-<run-id>-plan.md` — Planner

```markdown
# Plano — Refatoração de slices <run-id>

## Objetivo e escopo

(o quê, por quê, módulos incluídos/excluídos)

## Diagnóstico

(resumo da auditoria; link para 01-audit.md)

## Arquitetura-alvo

(camadas, use case por fluxo, handler 1:1, ports estreitos, erro centralizado, TimeProvider; link para a referência)

## Decisões

| Decisão | Escolha | Motivo |
| --- | --- | --- |
| Result vs exceções (por módulo) | ... | ... |
| Abstração do usuário autenticado | `ICurrentUser` / extensão do ClaimsPrincipal | ... |
| Mapeador único erro → ProblemDetails (pt-BR) | `IExceptionHandler` / filtro / Result→IResult | ... |

## Ordem de execução

| Ordem | Módulo | Motivo da ordem | Depende de | Risco |
| --- | --- | --- | --- | --- |

## Mapeamento 1:1

| Endpoint (método + rota) | Handler | Use case | Ports (estreitos) | Adapters | Testes |
| --- | --- | --- | --- | --- | --- |

## Batches

### Batch 0 — Caracterização (<modulo>)

- **Fluxos sem cobertura:**
- **Testes a criar (status + JSON, sucesso e erro):**
- **Critério:** passam verdes sobre o código antigo, sem alterar produção.

### Batch N — <modulo>

- **Objetivo:**
- **Arquivos a criar (UseCases/Ports/Errors/Handlers/Contracts/Module):**
- **Arquivos a mover:**
- **Arquivos a dividir (`*Service`, `*Contracts`, `*Endpoints`, repositório):**
- **Arquivos a apagar:**
- **Regras que saem do repositório (→ use case / domain service):**
- **`TimeProvider` (onde entra) e Domain recebendo o instante:**
- **DI (`<Modulo>Module.cs`):**
- **Testes (só namespace/construtor/DI):**
- **Riscos:**
- **Rollback:**
- **Critério de aceite:**

## Contrato preservado

- [ ] Rotas, métodos e prefixos
- [ ] Status codes (inclusive os produzidos inline hoje)
- [ ] JSON de request/response (nomes, casing, nulos, enums, datas)
- [ ] `ProblemDetails` (status, title, detail, extensões) em pt-BR
- [ ] Metadados OpenAPI (`WithName`, `Produces*`, tags, segurança)
- [ ] Auth e rate limit
- [ ] Esquema, `Migrations/` e snapshot do EF
- [ ] Chamadas a provedores (TMDB, OMDb, OpenAI, Expo) e payloads
- [ ] Dependências NuGet/npm

## Mobile (se no escopo)

(batches por feature; gates `npm run lint` e `npx tsc --noEmit`)

## Exceções

| Módulo | Motivo | Regra do script a tolerar (`--allow`) | Aprovado por |
| --- | --- | --- | --- |

## Documentação e ADR

- `.docs/ARCHITECTURE.md` (e `.docs/DEVELOPMENT.md`)
- `.adrs/NNNN-use-case-por-fluxo-e-handler-por-endpoint.md`

## Estratégia de testes

(foco por batch: `dotnet test --filter`; gates completos no Agente 4)

## Critério de pronto

- [ ] `audit-slices.mjs --strict` limpo (ou só exceções listadas)
- [ ] `dotnet build Reelix.sln -c Release` e `dotnet test Reelix.sln -c Release` verdes
- [ ] Contrato HTTP intacto
```

## `03-execution.md` — Implementer

```markdown
# Execução da refatoração — <run-id>

- **Plano:** `<plan-path>`
- **Branch:** `<branch>`
- **Status geral:** done | blocked

## Batches

| Batch | Módulo | Status | Arquivos | Verificação focada | Desvios |
| --- | --- | --- | --- | --- | --- |

### Batch N — <modulo>

- **Status:** done | blocked
- **Criados:**
- **Movidos:**
- **Divididos:**
- **Apagados:**
- **Regras movidas de repositório:**
- **DI atualizada:**
- **Testes atualizados (namespace/construtor/DI):**
- **Testes de caracterização novos:**
- **Comandos executados:**
  | Comando | Resultado |
  | --- | --- |
- **Desvios do plano:**
- **Bloqueios:**

## Docs e ADR

- ...

## `git status` esperado

- ...
```

## `04-gates.md` — QA

```markdown
# Gates de qualidade — <run-id>

- **Branch:** `<branch>`
- **Commit base:** `<sha>`
- **Status:** done | blocked | failed

## Gate de arquitetura

| Verificação | Comando | Resultado | Evidência |
| --- | --- | --- | --- |
| Auditoria mecânica | `node .../audit-slices.mjs --scope <scope> --strict` | Passou/Falhou | contagem por regra |
| Handler ↔ use case 1:1 | leitura + script | Passou/Falhou | ... |
| Sem catch/TryGetUserId em Endpoints/Handlers | leitura + script | Passou/Falhou | ... |

## Gate de contrato

| Contrato | Mudou? | Evidência |
| --- | --- | --- |
| Rotas/métodos/status | Não | `git diff` + testes de caracterização |
| JSON de request/response | Não | ... |
| ProblemDetails (pt-BR) | Não | ... |
| OpenAPI (`WithName`/`Produces*`) | Não | ... |
| Auth / rate limit | Não | ... |
| Esquema / `Migrations/` | Não | `git diff --stat -- backend/**/Migrations` vazio |
| Provedores externos | Não | ... |
| Pacotes (`*.csproj`/`package.json`) | Não | ... |

## Gates de QA

| # | Comando | Exit code | Resultado | Evidência |
| --- | --- | --- | --- | --- |
| 1 | `dotnet restore Reelix.sln --source https://api.nuget.org/v3/index.json -warnaserror:NU1903,NU1904` | 0 | Passou | ... |
| 2 | `dotnet build Reelix.sln -c Release --no-restore` | 0 | Passou | ... |
| 3 | `dotnet test Reelix.sln -c Release --no-restore` | 0 | Passou | total/aprovados/ignorados (Docker ativo) |
| 4 | `cd mobile && npm run lint` | 0 | Passou / n/a | ... |
| 5 | `cd mobile && npx tsc --noEmit` | 0 | Passou / n/a | ... |

## Não executado

| Gate | Motivo | Risco |
| --- | --- | --- |

## Falhas e correções

- ...
```

## `05-commit.md` — Commit & Push

````markdown
# Commit e push — <run-id>

- **Branch:** `<branch>`
- **Hash:** `<sha-curto>`
- **Header:** `<type>(<scope>): <descrição>`
- **Arquivos:** N
- **Push:** ok | pendente: <motivo>

## Mensagem

```text
<mensagem completa>
```

## Verificação

- [ ] `git log -1` confere o commit
- [ ] `git status` limpo (fora do run directory)
- [ ] Nenhum segredo/artefato gerado; `.claude/refactor/` fora do commit
- [ ] Push para `origin/<branch>` (`github.com/brunolucildo/reelix`)
````

## `06-pr.md` — PR

```markdown
# Pull Request — <run-id>

- **URL:** <url> | pendente
- **Base:** `<base-branch>`
- **Head:** `<branch>`
- **Título:** ...
- **Estado:** aberto | pendente

## Corpo do PR

(resumo ou link para `06-pr-body.md`)

## Pendências

- ...
```

## Relatório final do orquestrador

```markdown
## Refatoração de slices — <run-id>

- **Branch:** `<branch>`
- **Commit:** `<sha>` — `<header>`
- **Push:** ok | pendente
- **PR:** <url> | pendente
- **Plano:** `<plan-path>`
- **Run directory:** `<run-dir>`

### Módulos refatorados

| Módulo | Antes (services/métodos, endpoints) | Depois (use cases, handlers) | Ports | Erros tipados | Regras movidas |
| --- | --- | --- | --- | --- | --- |

### Auditoria mecânica (antes → depois)

| Regra | Antes | Depois |
| --- | --- | --- |

### Gates

| Gate | Resultado | Evidência |
| --- | --- | --- |

### Contrato HTTP

- Preservado? sim/não — evidência (testes de caracterização, `git diff`).

### Desvios, exceções e pendências

- ...

### Próximos passos

- ...
```
