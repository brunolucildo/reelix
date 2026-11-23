# Breakdown — Fundação de engenharia: arquitetura, design e testes

- **Data:** 2026-11-19
- **Origem:** pedido do usuário (Thales): "primeiro refatorar a arquitetura do projeto, sempre usando Clean Architecture e SOLID; depois refatorar o design visualmente usando `/web-design-guidelines` e `/frontend-design`; e por último adicionar a camada de testes, agora com Playwright e demais ferramentas."
- **Specs:** `0013` a `0023` em `.specs/`
- **Base factual:** auditorias de 2026-11-19 registradas em `.docs/ARCHITECTURE.md` §6.1 e §6.2, `.docs/DESIGN.md` §13, `.docs/DESIGN_SYSTEM.md` e `.docs/TESTING.md`; auditor mecânico `.claude/skills/refactor-slices/scripts/audit-slices.mjs` (105 achados no backend, 29 high).

## 1. Objetivo

Antes de ampliar funcionalidades (fases 3 a 5 de `.docs/BUSINESS.md` §6), levar o Reelix a um estado em que: (1) o backend e o mobile seguem a arquitetura-alvo de `.docs/ARCHITECTURE.md` §4.2 e §4.3, sem mudar comportamento nem o contrato HTTP; (2) o app tem um design system coeso e uma interface refeita com intenção, preservando a identidade da marca; (3) existe uma camada de testes confiável (xUnit com testes de arquitetura, Jest/RNTL e Playwright), com gates no CI.

## 2. Decisões compartilhadas

| #   | Decisão                                                                                                                                                                                                                       | Vale para |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------- |
| D1  | **Refatoração não muda comportamento.** Rotas, status, JSON, `ProblemDetails`, OpenAPI, autenticação e schema do banco permanecem; sem migration nova nas specs de arquitetura. Onde faltar cobertura, a spec exige teste de caracterização **antes** de mover código. | 0013–0018 |
| D2  | **Quatro projetos .NET continuam** (`Api`, `Application`, `Domain`, `Infrastructure`); um projeto de testes de arquitetura entra só na `0021`. Estrutura-alvo: `.docs/DEVELOPMENT.md` §2.1.                                    | 0013–0016, 0021 |
| D3  | **Um caso de uso por fluxo e um handler por endpoint (1:1)**; DTOs de request/response na Api; nenhum tipo de Domain em contrato HTTP; nenhuma regra de negócio em repositório, endpoint ou adaptador.                      | 0014–0016 |
| D4  | **Erros:** hierarquia de erros de aplicação tipados e **um** mapeamento central para `ProblemDetails` em pt-BR. Premissa: manter exceções tipadas (menor mudança que `Result<T>`); a escolha é registrada em ADR na `0013`. | 0013–0016 |
| D5  | **Tempo e identidade:** `TimeProvider` injetado (sem `UtcNow` em Domain/Application) e `ICurrentUser` único na Api; resolver a conta (`GetOrCreate`) é um passo explícito e `GET` não escreve.                              | 0013–0016 |
| D6  | **Mobile por feature:** `features/<modulo>/{api,hooks,components}`, um `http-client` com `ApiError`, query keys por fábrica, telas finas, primitivos em `components/ui`, formulários com RHF + zod.                          | 0017–0020 |
| D7  | **Identidade da marca preservada:** superfícies quase pretas, vermelho/rosa (`#FF2D55`), Sora e o brand kit seguem como ponto de partida; o design evolui a partir de `tokens.ts` (fonte única). shadcn/Tailwind **não** se aplicam. | 0019–0020 |
| D8  | **Copy em pt-BR**, erros de API em toast e validação inline, alvos de 44 px, dados reais, wireframe atualizado a cada tela que muda.                                                                                          | 0017–0023 |
| D9  | **Stack de testes:** xUnit + Testcontainers (existente) e testes de arquitetura; Jest (`jest-expo`) + React Native Testing Library; Playwright sobre o Expo web. Sem inventar scripts: cada spec cria o script real e o gate de CI. | 0021–0023 |
| D10 | **Segurança e privacidade invariantes:** autorização por recurso no servidor, dono vindo do `sub`, `service_role` fora do app, segredos fora do repositório, nada de dado real em testes.                                    | todas     |
| D11 | **Execução:** cada spec termina com `commit-and-push` pelo `implement-me` (branch `<type>/<slug>`, PR, checks `backend` e `mobile`); `main` é protegida.                                                                      | todas     |

## 3. Specs (ordem de implementação)

| #    | Arquivo                                                        | Capacidade                                                                                                                                                              | Camada    |
| ---- | -------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------- |
| 0013 | `0013-backend-fundacao-transversal.md`                         | `TimeProvider`, `ICurrentUser`, mapeamento único de erros, paginação/cursor únicos, DTOs na Api, padrão de DI por módulo, limpeza do `Program.cs`; ADR do padrão          | Arquitetura |
| 0014 | `0014-backend-gamification-notifications-casos-de-uso.md`      | Tirar a regra de negócio dos repositórios de Gamificação e Notificações; casos de uso e handlers 1:1; `GET` do dashboard sem escrita; testes de caracterização           | Arquitetura |
| 0015 | `0015-backend-catalog-tracking-lists-casos-de-uso.md`          | Casos de uso e handlers de Catalog, Tracking e Lists; port único "título existe no catálogo"; Application sem detalhes de HTTP; fim do adaptador que depende de service concreto | Arquitetura |
| 0016 | `0016-backend-ratings-social-users-auth-casos-de-uso.md`       | Casos de uso e handlers de Ratings/Comments, Social, Users (onboarding e perfil) e Auth; fronteiras de módulo finais; Domain fora dos contratos; `DbContext` organizado    | Arquitetura |
| 0017 | `0017-mobile-fundacao-http-e-dados.md`                         | `http-client` único com `ApiError`, 401 global, query keys por fábrica, `lib/` de formatação pt-BR, paginação e erros; remoção dos três clientes duplicados             | Arquitetura |
| 0018 | `0018-mobile-features-e-telas-finas.md`                        | `features/*`, hooks por feature, quebra das telas gigantes, guardas de rota únicas, formulários com RHF + zod                                                           | Arquitetura |
| 0019 | `0019-design-tokens-e-primitivos-ui.md`                        | Evolução dos tokens (cor semântica de erro, lineHeight, sombra, movimento, zIndex), sincronia com o brand kit e primitivos de UI                                         | Design    |
| 0020 | `0020-design-refatoracao-visual-das-telas.md`                  | Plano de design e refatoração visual das telas sobre os primitivos, com estados, navegação, responsividade e acessibilidade                                              | Design    |
| 0021 | `0021-testes-backend-arquitetura-e-cobertura.md`               | Projeto de testes de arquitetura, separação unitário/integração, fixtures e builders compartilhados, cobertura com piso no CI, host de teste para E2E                    | Testes    |
| 0022 | `0022-testes-mobile-jest-rntl.md`                              | Jest (`jest-expo`) + RNTL: http-client, hooks, primitivos e fluxos críticos; script `npm test` e gate no CI                                                              | Testes    |
| 0023 | `0023-testes-e2e-playwright-expo-web.md`                       | Playwright sobre o Expo web contra API e PostgreSQL de teste: entrada, busca, tracking, listas; acessibilidade com axe; job de CI                                        | Testes    |

## 4. Dependências

```text
0013 ─┬─> 0014 ─┐
      ├─> 0015 ─┼─> 0021 ─┐
      └─> 0016 ─┘         │
0017 ───> 0018 ─> 0019 ─> 0020 ─┬─> 0022
                                └─> 0023 (também depende de 0021)
```

- `0014`, `0015` e `0016` dependem só da `0013`, mas **tocam `Program.cs` e os arquivos de DI**: implemente em série (ou em worktrees separados com merge cuidadoso via `orchestrate-me`). A trilha mobile (`0017`→`0018`) é independente do backend e pode correr em paralelo.
- `0019` e `0020` só começam depois da `0018`, para não redesenhar telas que ainda serão reestruturadas.
- `0021` depende da `0016` (as regras de arquitetura só passam quando as camadas estiverem em ordem). `0022` depende da `0020` (testar os primitivos finais). `0023` depende da `0020` e da `0021` (host de teste da API).

## 5. Rastreabilidade do prompt

| Trecho do pedido                                                          | Specs                  |
| ------------------------------------------------------------------------- | ---------------------- |
| "primeiro refatorar a arquitetura do projeto, sempre usando Clean Architecture e SOLID" | 0013–0018 (backend 0013–0016; mobile 0017–0018) |
| "depois refatorar o design visualmente utilizando /web-design-guidelines e /frontend-design" | 0019–0020 |
| "e por último adicionar a camada de teste agora usando playwright e etc." | 0021 (xUnit e arquitetura), 0022 (Jest/RNTL), 0023 (Playwright) |

## 6. Backlog (candidatas não criadas)

- Contrato OpenAPI como teste (geração/validação de cliente) — depende de decisão sobre codegen (`.docs/ARCHITECTURE.md` §6.2).
- Observabilidade (REELIX-016), hosting e métricas — decisão pendente.
- Tema claro e layouts de tablet/desktop — fora desta fase (ver pergunta Q3).
- CI de Playwright com matriz de dispositivos reais (Android) — depende de REELIX-005.
- Widgets Android (REELIX-013) — spec `0012`; só após a fundação.

## 7. Perguntas abertas

| #  | Pergunta                                                                                                                                              | Impacto                                                        | Bloqueante? | Quem decide |
| -- | ----------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------- | ----------- | ----------- |
| Q1 | Erros de aplicação: manter exceções tipadas (premissa) ou adotar `Result<T>`?                                                                         | Forma dos casos de uso (0013–0016)                              | Não (ADR na 0013) | Tech lead |
| Q2 | Escrever testes **antes** de refatorar o mobile? A ordem pedida põe os testes por último; a trilha mobile fica protegida só por `tsc`, `lint` e roteiro manual. | Risco de regressão em 0017–0018                                 | Não         | Usuário     |
| Q3 | O design inclui tema claro e layouts de tablet/web? Premissa: **não** (só escuro, retrato; o Expo web é ferramenta de desenvolvimento e E2E, não alvo de produto). | Escopo de 0019–0020                                             | Não         | Usuário     |
| Q4 | Modelo de navegação: manter menu lateral + `AppHeader` ou adotar abas inferiores? O mood board mostra abas.                                          | Estrutura de 0020                                               | **Sim** para 0020 | Usuário |
| Q5 | E2E: como apontar a API para stubs de catálogo/Expo/OpenAI? As URLs estão fixas em `DependencyInjection.cs`; premissa: torná-las configuráveis (sem mudar defaults). | Viabilidade de 0023                                             | Não (resolvido em 0021/0023) | Tech lead |
| Q6 | O Expo web passa a ser plataforma suportada do produto ou fica só como ambiente de testes?                                                           | Escopo de design e E2E                                          | Não (premissa: só testes) | Usuário |
| Q7 | `GET /gamification/dashboard` deve parar de gravar? A 0014 isola a escrita num passo explícito, mas o dashboard ainda a aciona; eliminar a escrita muda `firstEarnedAt`/`lastRevokedAt` e exige um gatilho de reconciliação. `GET /gamification/users/{id}/badges` hoje grava na conta de outra pessoa. | Contrato observável de Gamificação (0014) | Não (default: preservar) | Produto/Tech lead |
| Q8 | Remover o sufixo ` (Parameter '…')` do `detail` em Social, Gamification e Notifications? É a única mudança deliberada de contrato (0013); default: sim. | Contrato de erro (0013) | Não | Tech lead |

## 8. Resultado da criação (2026-11-19)

As 11 specs foram criadas com as 12 seções do template; 10 estão `pronta` e a `0020` está `rascunho`, bloqueada pela Q4. Observações registradas pelos autores:

- A `0017` já cria `features/<modulo>/api/` e `query-keys.ts` no destino final; a `0018` acrescenta `hooks/` e `components/`.
- O `TimeProvider` falso da `0021` depende da `0013`, não da `0016`; as partes de fixtures e do host de teste podem começar antes.
- O host de teste da `0021` nasce em `backend/Reelix.E2EHost` e torna configuráveis as URLs de TMDB/OMDb/Expo/OpenAI (defaults inalterados, recusadas em `Production`).
- O auditor `audit-slices.mjs` tem um falso negativo conhecido: `stripNoise` trata mal strings `$"""` e pode esconder relógio global e `switch` em `NotificationsRepository.cs`; as specs usam `grep` além de `--strict`.
- A skill `playwright` é CLI-first; a `0023` registra que o pedido explícito do usuário autoriza `@playwright/test`.

## 9. Decisões do usuário (quiz de 2026-11-19)

| #  | Decisão                                                                                       | Efeito                                                                                          |
| -- | --------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| Q4 | **Cinco abas inferiores** (Início, Buscar, Minha Lista, Amigos, Perfil)                       | `0020` passa a `pronta`; reestrutura `_layout`, rotas, deep links e destinos de notificação     |
| Q2 | **Testes do mobile por último** (ordem original mantida)                                      | `0017`–`0018` protegidas por lint, tsc e roteiro manual por feature                              |
| Q7 | **Preservar** o `GET /gamification/dashboard` (opção A)                                        | `0014` isola a escrita em `AchievementReconciler`; mudar o gatilho vira spec própria            |
| Q8 | **Remover** o sufixo ` (Parameter '…')` das mensagens de Social, Gamification e Notifications | Única mudança de contrato (`0013`)                                                              |
| Q3/Q6 | **Só escuro, retrato, Expo web só para testes**                                            | Escopo de `0019`–`0020` e `0023` confirmado                                                     |
| —  | **Não unificar** os limites de paginação (preservar por política)                              | `0013`                                                                                          |
| —  | **Constante nomeada** para idioma/provedor padrão do catálogo                                  | `0015`                                                                                          |

Q1 (exceções tipadas no lugar de `Result<T>`) e Q5 (URLs de provedores configuráveis para E2E) seguem como premissas do mapa; não foram levadas ao quiz.

## 10. Execução

| Passo | Specs | Estado | Branch / PR | Observações |
| ----- | ----- | ------ | ----------- | ----------- |
| 0 | Fundação do processo (docs, skills, specs) | Concluído | PR #13 mesclado na `main` | — |
| 1 | `0013` ‖ `0017` | **Integrado** em `orchestrate/0013-0017` (2026-11-23); aguardando PR e merge | `feat/0013-backend-fundacao-transversal`, `feat/0017-mobile-fundacao-http-e-dados` | Backend: 339 testes verdes (eram 202). Mobile: lint, tsc e audit passam. **Roteiro manual da `0017` ainda não executado** (`.claude/orchestrate/20261123-1316-0013-0017/0017.report.md`). ADRs: `0012` (backend) e `0013` (mobile). |
| 2 | `0014` ‖ `0018` | Pendente | — | — |
| 3 | `0015` ‖ `0019` | Pendente | — | — |
| 4 | `0016`, depois `0020` | Pendente | — | A `0020` começa pelo plano de design (aprovação do usuário). |
| 5 | `0021` ‖ `0022` | Pendente | — | — |
| 6 | `0023` | Pendente | — | — |

Achados do passo 1 que afetam as próximas specs: (a) o `429` do rate limiter responde `application/json` (e não `problem+json`), preservado e fixado por teste; (b) o auditor ainda acusa `handler-without-usecase` no piloto `GET /auth/me`, a corrigir na `0016`; (c) `0017` criou `features/<modulo>/api` e `query-keys.ts`, então a `0018` só acrescenta `hooks/` e `components/`; (d) `getInitials` existe em `lib/format` sem consumidores (a troca das 3 cópias é da `0018`).
