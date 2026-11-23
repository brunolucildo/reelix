# AGENTS.md — Guia operacional do repositório

> **Público:** agentes de código e pessoas que vão atuar no projeto.
> **Fonte de verdade:** este diretório (`.docs/`). O `README.md` da raiz é um resumo curto de entrada; o `AGENTS.md` da raiz só aponta para cá.
> **Verificado contra o código em:** 2026-11-19.

## 1. O que é o projeto

**Reelix** é um aplicativo para descobrir e acompanhar filmes e séries, registrar o que se assistiu, organizar títulos em listas e compartilhar atividades autorizadas com amigos. O repositório reúne o app mobile (React Native + Expo, em `mobile/`) e uma API ASP.NET Core (.NET 10, em `backend/`) em monólito modular com Clean Architecture; a identidade é do Supabase Auth e os dados da conta Reelix ficam em PostgreSQL.

**Estado atual (2026-11-19):** autenticação, onboarding, perfil, catálogo (TMDB/OMDb), Home de descoberta, tracking, favoritos/watchlist/listas, avaliações e comentários com proteção contra spoilers, social, notificações (inbox) e gamificação estão implementados no mobile e na API; o único ambiente verificado é `Development`. O APK EAS de development foi validado no Samsung (login, navegação, logout e recuperação de senha). **Ainda não existem:** widgets Android, push remoto habilitado, alertas de catálogo, recomendação por IA, posts e reações sociais, moderação administrativa, upload de avatar, exclusão/exportação de conta, observabilidade, **testes automatizados do mobile e testes E2E**. O backend tem 339 testes xUnit que passam (2026-11-23). A estrutura interna do código ainda não segue integralmente a arquitetura-alvo (`ARCHITECTURE.md` §4 e §6): refatorar a arquitetura, o design visual e criar a camada de testes são os próximos passos, conduzidos por specs em `.specs/`. Não trate a arquitetura-alvo como se já estivesse implementada.

## 2. Leia antes de trabalhar

| Se você vai...                                          | Leia                             |
| ------------------------------------------------------- | -------------------------------- |
| entender o produto, o vocabulário, o escopo e o roadmap | `BUSINESS.md`                    |
| tocar em estrutura, módulos, contratos ou stack         | `ARCHITECTURE.md`                |
| criar ou alterar UI                                     | `DESIGN.md` + `DESIGN_SYSTEM.md` |
| escrever qualquer código                                | `DEVELOPMENT.md`                 |
| criar rota, endpoint, erro ou texto de interface        | `DEVELOPMENT.md` §13             |
| escrever ou rodar testes                                | `TESTING.md` + `QA.md`           |
| validar uma entrega                                     | `QA.md`                          |
| iniciar uma tarefa ou finalizar uma entrega             | `RULES.md`                       |
| ver uma decisão estrutural já tomada                    | `.adrs/`                         |
| ver o histórico e o escopo de cada módulo               | `.specs/`                        |

## 3. Comandos reais

Requer **.NET 10 SDK** e **Node.js + npm** (a CI usa Node 22). **Docker** só é necessário para os testes que usam Testcontainers (`PostgresAuthorizationIntegrationTests`). Detalhes de ambiente em `QA.md` §2.

**Backend** (na raiz do repositório, PowerShell):

| Objetivo                       | Comando                                                                                                                                                                       |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| restaurar                      | `dotnet restore Reelix.sln --source https://api.nuget.org/v3/index.json`                                                                                                      |
| compilar                       | `dotnet build Reelix.sln --configuration Release --no-restore`                                                                                                                |
| testes                         | `dotnet test Reelix.sln --configuration Release --no-restore`                                                                                                                 |
| subir a API (perfil `http`)    | `dotnet run --project backend\Reelix.Api\Reelix.Api.csproj --launch-profile http`                                                                                             |
| segredos locais                | `dotnet user-secrets set "<chave>" "<valor>" --project backend\Reelix.Api\Reelix.Api.csproj`                                                                                  |
| restaurar ferramentas          | `dotnet tool restore`                                                                                                                                                         |
| script idempotente de migrations | `dotnet tool run dotnet-ef migrations script --idempotent --project backend\Reelix.Infrastructure\Reelix.Infrastructure.csproj --startup-project backend\Reelix.Api\Reelix.Api.csproj --output artifacts\reelix-migrations.sql` |
| aplicar migrations (dev)       | `dotnet tool run dotnet-ef database update --project backend\Reelix.Infrastructure\Reelix.Infrastructure.csproj --startup-project backend\Reelix.Api\Reelix.Api.csproj`        |

**Mobile** (dentro de `mobile/`):

| Objetivo                        | Comando                                 |
| ------------------------------- | --------------------------------------- |
| instalar (reprodutível)         | `npm ci`                                |
| subir o Metro                   | `npx expo start`                        |
| Android / iOS / web             | `npm run android` / `npm run ios` / `npm run web` |
| lint                            | `npm run lint`                          |
| checar tipos                    | `npx tsc --noEmit`                      |
| diagnosticar dependências Expo  | `npx expo-doctor`                       |
| instalar módulo Expo            | `npx expo install <pacote>`             |
| gerar PNGs de plataforma do brand kit | `npm run brand:generate`          |
| auditar dependências            | `npm audit --audit-level=high`          |

Portas e endereços em `ARCHITECTURE.md` §2.1 (API em `http://localhost:5035`).

**Não existem** (não invente): `npm test`, `npm run test:e2e`, `npm run typecheck`, scripts de Bun/Turborepo, `docker compose` do projeto, hooks de pre-commit, `dotnet format` configurado, testes de arquitetura. Eles são alvo das specs de testes (`ARCHITECTURE.md` §3.2).

**Baseline em 2026-11-23** (branch de integração das specs `0013` e `0017`): `dotnet test Reelix.sln --configuration Release` passa (339 testes, 0 falhas, com Docker); `npm run lint` e `npx tsc --noEmit` passam em `mobile/`. Evidências e limites em `QA.md` §1.

## 4. Regras não negociáveis

1. **Não invente comandos, scripts, dependências ou skills.** Confira `package.json`, os `.csproj` e o catálogo de skills antes de afirmar que algo existe.
2. **Não afirme que executou** build, lint, tipos ou testes sem ter executado e visto o resultado.
3. **UI:** siga `DESIGN.md` e `DESIGN_SYSTEM.md` (tokens de `mobile/src/theme/tokens.ts`, toast para erro, inline para validação, alvos de 44 px, copy em pt-BR, dados reais). Atualize o wireframe quando uma tela mudar.
4. **Toda tarefa de desenvolvimento** tem spec em `./.specs/` e plano em `./.plans/`; decisões estruturais também geram ADR em `./.adrs/` (ver `RULES.md`). Tarefas triviais usam plano curto, mas não pulam o registro.
5. **Autorização no servidor:** nunca confie em ID de usuário enviado pelo cliente nem em tela protegida; o dono vem do `sub` do JWT validado.
6. **Segredos** nunca entram no repositório: `ConnectionStrings__Reelix`, tokens de TMDB/OMDb/OpenAI/Expo, `Supabase__JwtSecret` e a `service_role` ficam só no servidor (User Secrets ou gerenciador de segredos). `EXPO_PUBLIC_*` vai para o bundle e é público; jamais coloque `service_role` ou chave secreta no app.
7. **Migrations:** não edite migration já aplicada em algum banco; não aplique em ambiente compartilhado sem autorização e revisão do SQL; o CI só gera o script, não aplica.
8. **Se o código divergir destes docs, o código vence.** Atualize o `.docs` na mesma mudança.
9. **Não edite artefatos gerados** (`bin/`, `obj/`, `node_modules/`, `.expo/`, `dist/`, `coverage/`, `TestResults/`, `artifacts/` gerado pelo CI) nem lockfiles à mão.
10. **Contrato público preservado:** refatorações não mudam rotas, status nem JSON da API sem atualizar o cliente mobile e a spec.

## 5. Skills

Consulte o catálogo disponível antes de começar. O projeto tem nove skills de processo próprias, mais skills de apoio:

| Skill             | Quando usar                                                         | O que produz                                             |
| ----------------- | ------------------------------------------------------------------- | -------------------------------------------------------- |
| `breakdown-specs` | prompt grande que junta várias capacidades                          | mapa em `.plans/<epic>-breakdown.md` + N specs           |
| `create-spec`     | prompt, ideia ou ticket que precisa virar spec                      | `.specs/<NNNN>-<slug>.md` executável                     |
| `refine-spec`     | spec existente vaga, incompleta ou contraditória                    | a mesma spec refinada + histórico de mudanças            |
| `feature-concept` | ideia que exige interrogatório profundo **e** plano de execução     | `.specs/<slug>.md` + `.plans/<slug>-plan.md`             |
| `implement-me`    | spec/plano pronto para virar código (`implement-me <slug>`)         | implementação + QA + ADR + commit/push + relatório       |
| `orchestrate-me`  | várias specs de uma vez (`orchestrate-me 0013-0016`)                | 1 subagent por spec em worktree + integração + relatório |
| `refactor-slices` | módulos fora da arquitetura em camadas (`refactor-slices`)          | auditoria + plano + refactor + gates + commit/push + PR  |
| `commit-and-push` | fim de cada spec (chamada pelo `implement-me`) ou pedido do usuário | commit Conventional Commits com co-author + push         |
| `code-review`     | revisar um PR do GitHub (`/code-review <url-do-pr>`)                | revisão publicada no PR: comentários inline + geral      |

Fluxo padrão: `create-spec` (ou `feature-concept`) → `refine-spec` quando precisar de auditoria → `implement-me <slug>`, que **termina sempre com `commit-and-push`**. Quando o pedido junta mais de uma capacidade, comece por `breakdown-specs`; depois rode `implement-me` em cada spec, uma por vez, ou `orchestrate-me <range>` para várias de uma vez (um subagent por spec, em worktrees separados, respeitando dependências).

`commit-and-push` analisa todas as mudanças pendentes, escreve a mensagem em Conventional Commits (descrição em pt-BR; `type` e `scope` em inglês), adiciona o co-author do Claude Code, faz `git add -A`, commita e dá push. Só roda a pedido do usuário ou no último passo do `implement-me`; nunca faz force push, `--amend`, `--no-verify` nem `git init`. **Destino fixo:** o push só vai para `https://github.com/brunolucildo/<repositorio>` via `origin` (hoje `https://github.com/brunolucildo/reelix.git`). A branch `main` é protegida: exige PR e os checks `backend` e `mobile`; a skill cria `<type>/<slug>` e nunca commita direto em `main`. Não há hooks de pre-commit.

`code-review` recebe o link de um PR (`https://github.com/<dono>/<repo>/pull/<n>`) e lança o agente `code-reviewer`, que revisa o diff em modo somente leitura segundo `.claude/skills/code-review/references/review-checklist.md`. Os comentários seguem **Conventional Comments**; qualquer `issue` ou `(blocking)` resulta em `REQUEST_CHANGES`, caso contrário `APPROVE`. Artefatos de cada execução ficam em `.claude/review/` (ignorado pelo git).

**Onde as skills ficam:** `.claude/skills/<nome>/SKILL.md`, carregadas pelo Claude Code (invocação por `/<nome>`; a lista é lida ao iniciar a sessão). O agente próprio `code-reviewer` vive em `.claude/agents/code-reviewer.md`. As skills de terceiros estão registradas em `skills-lock.json`. Ao alterar uma skill, mantenha este guia e `RULES.md` coerentes.

Skills de apoio (valem conforme o contexto):

- `frontend-design` e `web-design-guidelines` — obrigatórias para trabalho de UI **se estiverem instaladas**. `web-design-guidelines` é escrita para web: aplique o que vale para React Native/Expo web (acessibilidade, estados, tipografia, contraste, foco) e registre o que não se aplica.
- **Mobile (Expo/React Native):** `expo-router` (rotas, `Stack`, abas, modais), `expo-data-fetching` (fetch, React Query, erros, offline), `expo-animation` (Reanimated, gestos, haptics), `vercel-react-native-skills` e `react-native-best-practices` (desempenho: listas, re-renders, JS thread). Antes de usar uma API do Expo, a regra de `mobile/AGENTS.md` continua valendo: confira a versão do SDK e a doc versionada.
- **Backend (.NET):** `dotnet-webapi` (endpoints, OpenAPI, tratamento de erro), `dotnet-best-practices`, `csharp-async` (async e `CancellationToken`), `csharp-refactoring` (refatorar sem mudar comportamento), `ef-core` e `optimizing-ef-core-queries` (EF Core), `csharp-xunit` (testes xUnit).
- **Testes:** `tdd` (orientado a comportamento), `react-native-testing` (RNTL v13/v14, para a spec `0022`), `playwright` (CLI) e `playwright-best-practices` (`@playwright/test`: seletores, flakiness, CI, axe; para a spec `0023`).
- **Dados e segurança:** `supabase-postgres-best-practices` (schema, índices, RLS, migrations), `supabase` (Auth, RLS, Storage, CLI), `security-review` (injeção, autenticação, autorização/IDOR).
- **Acessibilidade:** `accessibility` (WCAG 2.2; escrita para web, aplique os princípios ao React Native e registre o que não se aplica).
- `skill-creator` e `find-skills` (manutenção e descoberta de skills).

**Skills de terceiros são instruções que o agente segue:** só instale depois de ler o `SKILL.md`, registre em `skills-lock.json` e instale para o Claude Code (`npx skills add <fonte> -a claude-code`), que grava em `.claude/skills/`. As skills `expo-*` trazem um passo opcional para enviar feedback à Expo com dados do projeto (`@expo/agent-cli feedback`): **não envie sem autorização explícita do usuário**.

Não existem aqui (foram removidas por serem de tecnologias que o projeto não usa ou por risco): `next-best-practices`, `shadcn`, `vercel-react-best-practices`, `expo-dev-client` (avaliada como risco crítico pela CLI de skills; instrui `npx eas-cli@latest` sem versão fixa) e `github-actions` da Callstack (cobre builds de simulador, não o nosso CI).

Se uma skill de terceiro esperada não estiver disponível, **não bloqueie a tarefa**: siga os documentos deste diretório e registre a ausência no relatório final.

## 6. Mapa deste diretório

- `BUSINESS.md` — problema, proposta de valor, conceitos, escopo, roadmap e decisões abertas.
- `ARCHITECTURE.md` — estado atual, stack, limites, camadas (hoje e alvo), contratos por módulo, decisões e dívidas.
- `DESIGN.md` — direção visual, paleta, tipografia, acessibilidade e padrões de UX.
- `DESIGN_SYSTEM.md` — tokens, componentes e regras de UI do mobile.
- `DEVELOPMENT.md` — princípios, camadas, padrões de código backend/mobile, convenções de API e definição de pronto.
- `TESTING.md` — como os testes funcionam e a estratégia-alvo.
- `QA.md` — gates, ambiente, baseline, checklist manual e evidências.
- `RULES.md` — fluxo obrigatório, templates de plano/ADR/relatório e limites de atuação.
- `design/` — wireframes HTML e o mood board `visual_identity.png`.

## 7. Estrutura do repositório

```text
backend/Reelix.Api              # HTTP (minimal APIs), Program.cs, workers, wwwroot/dev-auth.html
backend/Reelix.Application      # serviços/casos de uso, contratos e ports
backend/Reelix.Domain           # entidades e regras
backend/Reelix.Infrastructure   # EF Core, repositórios, provedores, migrations
backend/Reelix.UnitTests        # xUnit (unidade, modelo EF, HTTP e Testcontainers)
mobile/                         # app Expo/React Native (src/app, components, services, theme, assets/brand)
Reelix.sln                      # solução .NET
artifacts/                      # script de migrations gerado (revisão manual)
.github/workflows/ci.yml        # CI (backend + mobile)
.claude/skills/                 # skills carregadas pelo Claude Code
.claude/agents/                 # agentes próprios (code-reviewer)
.docs/                          # documentação canônica do projeto
.specs/                         # specs (_template.md + NNNN-<slug>.md)
.plans/                         # planos de implementação (<slug>-plan.md)
.adrs/                          # Architecture Decision Records
```
