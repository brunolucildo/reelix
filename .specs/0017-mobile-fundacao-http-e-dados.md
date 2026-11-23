# 0017 — Mobile: fundação HTTP e dados

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** implementada (roteiro manual da seção 10 pendente)
- **Slug:** mobile-fundacao-http-e-dados
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-23
- **Owner:** Tech lead (a confirmar)
- **Relacionados:** mapa `.plans/arquitetura-design-testes-breakdown.md` (decisões D1, D6, D8, D10, D11; perguntas Q2 e Q6); ADR 0008 (`.adrs/0008-tanstack-query-e-zod-para-estado-de-servidor-no-mobile.md`, será atualizado); `.docs/ARCHITECTURE.md` §4.3 e §6.2; `.docs/DEVELOPMENT.md` §2.3 e §12; specs vizinhas: `0018` (features e telas finas, depende desta), `0019`/`0020` (design), `0022` (testes Jest/RNTL), `0023` (E2E)

## 1. Escopo

### Dentro do escopo

- Um único `http-client` em `mobile/src/lib/`: base da API (`api-url.ts`), `Authorization: Bearer` da sessão Supabase, parse de `ProblemDetails`, validação zod da resposta, `AbortSignal`.
- `ApiError` tipado (`status`, `title`, `detail`) e **um** tradutor de erro para mensagem de usuário (pt-BR).
- Tratamento **global** de 401 no `QueryClient` (`queryCache`/`mutationCache` `onError`): encerrar a sessão local, limpar o cache e mostrar uma única mensagem de sessão expirada.
- Remoção dos três clientes duplicados (`apiRequest`, `trackingRequest`, `request`).
- Divisão do `services/api-client.ts` (848 linhas) por domínio, em `src/features/<modulo>/api/`, **sem mudar os nomes nem as assinaturas** usadas pelas telas; migração em passos com um barrel temporário.
- Fábricas de query key por domínio (modelo: `trackingQueryKeys`), uma chave por recurso (inclui "detalhe do título"), helpers de paginação e constantes de `staleTime`.
- `lib/format` pt-BR (datas e números) e `getInitials`; `isCatalogProvider`/`CatalogProvider` únicos.

### Fora do escopo

- Organização por feature dos **hooks e componentes**, quebra das telas, `Redirect` duplicados, RHF nos formulários, primitivos e `ModalDialog` no lugar de `Alert.alert`, alias `@/assets`: spec `0018`.
- Qualquer mudança visual, tokens, primitivos novos, navegação: specs `0019` e `0020`.
- Testes automatizados do mobile (Jest/RNTL) e E2E: specs `0022` e `0023`. Não há testes mobile hoje (Q2); a verificação é `lint`, `tsc` e roteiro manual.
- Mudança de contrato HTTP, geração de cliente por OpenAPI, nova dependência npm, trocar TanStack Query ou zod.
- Backend (`0013`–`0016`).

## 2. Contexto

Estado medido em 2026-11-19 em `mobile/src` (12.496 linhas):

- **3 clientes HTTP copiados** (`apiRequest` em `services/api-client.ts:367`, `trackingRequest` em `tracking-api.ts:73`, `request` em `lists-api.ts:60`): cada um repete `supabase.auth.getSession()`, `await fetch(`, parse de `ProblemDetails` e `safeParse`, com mensagens de fallback diferentes e `status` anexado por `Object.assign`.
- `api-client.ts` tem 848 linhas e 43 funções exportadas de 8 domínios (conta, onboarding, catálogo, perfil, social, notificações, gamificação, avaliações/comentários) com os schemas zod no mesmo arquivo; `lists-api.ts` (249) e `tracking-api.ts` (240) à parte. `CatalogTitle` é exportado por `api-client.ts` e por `lists-api.ts` (mesmo schema).
- **Erro e sessão espalhados:** "Sua sessão expirou" aparece 11 vezes; o teste `'status' in error` aparece 6 vezes (`gamification.tsx`, `home.tsx`, `my-ratings.tsx`, `tracking.tsx` ×2, `hooks/use-tracking.ts`); `useErrorToast` é chamado 79 vezes; `home.tsx:42` mostra texto técnico ("HTTP n"). Não há tratamento global de 401: a tela mostra o toast e o usuário fica preso.
- **Cache fragmentado:** 93 ocorrências de `queryKey` (82 com array literal inline) e só `trackingQueryKeys` é fábrica. O mesmo título é buscado por `fetchCatalogTitleDetails` (9 chamadas em 8 arquivos) sob pelo menos 4 chaves (`['catalog','title',…]`, `['tracking','series-title',…]`, `['tracking','history-title',…]`, `['gamification','activity-title',…]`); há chaves de topo órfãs (`['catalog-seasons',…]`, `['catalog-season',…]`, `['reviews','mine']` convivendo com `['reviews', viewerId, …]`).
- **Paginação e frescor:** `getNextPageParam` reescrito 10 vezes (cursor `nextCursor` em 7, `page * pageSize < totalCount` em 2, `page < totalPages` em 1); `staleTime` com número solto 24 vezes (19× `300_000`, 4× `60_000`, 1× `30_000` no `_layout.tsx`).
- **Formatação:** 17 usos de `Intl.*`/`toLocaleDateString`/`toLocaleString` fora de qualquer módulo (`formatCatalogDate`, `formatActivityTime`, `watchDateFormatter`, `localDateKey` são locais de cada arquivo); `isCatalogProvider` existe duas vezes com tipos diferentes (`title/...:548` e `episode/...:128`).
- `QueryClient` é criado em `app/_layout.tsx` (`retry: 1`, `staleTime: 30_000`); `signOut` em `auth-provider.tsx` já limpa o cache e a sessão local (`queryClient.clear()`).

Por que agora: é a base de dados que a `0018` usa para mover hooks e afinar telas; fazer as duas ao mesmo tempo mistura mudança de contrato interno com mudança de tela, sem testes para proteger (Q2).

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Pessoa que usa o app (sessão e mensagens de erro consistentes); quem mantém o código (um lugar para cada decisão de dados). |
| Decisão ou ação que muda | Sessão expirada deixa de travar a tela: o app volta ao login com uma mensagem. Erros viram texto de usuário em um só lugar. Novos módulos nascem sobre `http-client`, fábricas de chave e `lib/format`. |
| Resultado esperado       | Um cliente, um erro tipado, um tratamento de 401, uma chave por recurso, formatação pt-BR única; comportamento visual idêntico ao de hoje. |
| Métrica de sucesso       | Metas numéricas da seção 10 (clientes 3→1, "Sua sessão expirou" 11→1, `'status' in` 6→0, `queryKey: [` inline 82→0, `getNextPageParam: (` inline 10→0, `staleTime: <número>` fora de `lib/` 24→0, `api-client.ts` 848→≤80). |

## 4. Requisitos

Decisões compartilhadas copiadas do mapa: D1 (sem mudar comportamento), D6 (estrutura por feature), D8 (copy pt-BR, erro em toast, validação inline), D10 (segurança invariante), D11 (um PR, checks `mobile`).

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | `lib/http-client` é o único ponto com `fetch` à API: base de `api-url.ts`, Bearer do Supabase, `Accept`/`Content-Type`, parse de `ProblemDetails`, `safeParse` zod, 204 sem corpo, `signal` repassado. | Must | Given o código final, When `grep -rn "await fetch(" mobile/src`, Then há 1 ocorrência, em `src/lib/http-client.ts` (hoje 3); When `grep -rn "supabase.auth.getSession" mobile/src`, Then só `lib/http-client.ts` e `providers/auth-provider.tsx` (hoje 4 arquivos). |
| R2  | `ApiError` (`status`, `title`, `detail`; `message` = `detail ?? title`) é lançado em toda resposta não-2xx. Falha de rede e resposta fora do contrato zod também viram erro tipado distinguível (`isApiError`, rede, contrato) com as mensagens de hoje. | Must | Given resposta 409 com `ProblemDetails`, When a função de serviço rejeita, Then `error.status === 409` e `error.message` é o `detail` do servidor; Given `fetch` que rejeita com `TypeError`, Then a mensagem é a de "não foi possível conectar à API" usada hoje em `home.tsx`; Given corpo fora do schema, Then a mensagem é "A API retornou dados em formato inesperado.". |
| R3  | `QueryClient` trata 401 em `queryCache.onError` e `mutationCache.onError`: encerra a sessão local (sem revogar push, que também daria 401), limpa o cache, mostra **um** toast de sessão expirada e **não** repete a requisição (`retry` não se aplica a 401). Idempotente com várias requisições simultâneas. | Must | Given sessão aceita pelo app e API que responde 401, When abro `home`, Then volto ao login (via `Stack.Protected`) com um único toast de sessão expirada e `GET` não é repetido; Given 5 consultas em voo respondendo 401, Then há um logout e um toast. |
| R4  | A mensagem "Sua sessão expirou. Entre novamente para continuar." existe em **uma** constante e `useErrorToast` não duplica o toast de 401 já tratado globalmente. | Must | `grep -rn "Sua sessão expirou" mobile/src \| wc -l` = 1 (hoje 11); `grep -rn "'status' in" mobile/src --exclude=auth-errors.ts \| wc -l` = 0 (hoje 6; `auth-errors.ts` trata erros do Supabase Auth e fica). |
| R5  | **Um** tradutor `toUserMessage(error, fallback)` converte `ApiError`/rede/contrato em texto de usuário; nenhum texto técnico ("HTTP n") chega à tela. | Must | `grep -rn "HTTP \${" mobile/src \| wc -l` = 0 (hoje 1, `home.tsx:42`); `home.tsx` não decide mensagem por `instanceof TypeError`/`status`. |
| R6  | Os 3 clientes duplicados são removidos; `tracking-api.ts` e `lists-api.ts` usam `http-client`. | Must | `grep -rnE "function (apiRequest\|trackingRequest\|request)\b" mobile/src \| wc -l` = 0 (hoje 3). |
| R7  | `api-client.ts` é dividido por domínio em `src/features/<modulo>/api/` (account/auth, onboarding, catalog, profile, social, notifications, gamification, ratings, mais lists e tracking movidos de `services/`), cada arquivo com schemas zod + funções. `services/api-client.ts` vira barrel temporário com **os mesmos nomes exportados**; `CatalogTitle` único. | Must | `wc -l mobile/src/services/api-client.ts` ≤ 80 (hoje 848); `wc -l` de cada arquivo em `features/*/api/*.ts` ≤ 250; `npx tsc --noEmit` passa sem editar assinaturas nas telas (diff das telas só troca imports opcionalmente); `ls mobile/src/services` não contém `lists-api.ts` nem `tracking-api.ts`. |
| R8  | Cada domínio tem uma fábrica de query key em `features/<modulo>/query-keys.ts` (modelo `trackingQueryKeys`, que migra de `hooks/use-tracking.ts` mantendo o export). Nenhum `queryKey` literal fora das fábricas; invalidação por prefixo da fábrica. | Must | `grep -rnE "queryKey: \[" mobile/src \| wc -l` = 0 (hoje 82). |
| R9  | O detalhe de título tem **uma** chave (`catalogQueryKeys.title(provider, contentType, externalId)`); as 9 chamadas de `fetchCatalogTitleDetails` usam essa chave. Chaves órfãs (`catalog-seasons`, `catalog-season`, `reviews` sem viewer) entram nas fábricas de catálogo e avaliações. | Must | Given o mesmo título aberto em `title/...`, `tracking` e `social`, When navego entre eles, Then a aba Network (Expo web) mostra uma requisição de detalhe por título dentro do `staleTime`; `grep -rn "'series-title'\|'history-title'\|'activity-title'" mobile/src \| wc -l` = 0. |
| R10 | Helpers de paginação em `lib/pagination` (cursor `nextCursor`; página numérica) substituem os 10 `getNextPageParam`; constantes de frescor em `lib/query/stale-time` (`short` 60 s, `catalog` 5 min; padrão global 30 s inalterado). | Must | `grep -rn "getNextPageParam: (" mobile/src \| wc -l` = 0 (hoje 10); `grep -rnE "staleTime: [0-9]" mobile/src --exclude-dir=lib \| wc -l` = 0 (hoje 24); valores efetivos iguais aos de hoje por consulta. |
| R11 | `lib/format` (pt-BR) concentra datas e números e `getInitials`; `formatCatalogDate`, `formatActivityTime`, `watchDateFormatter`, `localDateKey` deixam de ser locais. A saída é idêntica à de hoje. | Must | `grep -rnE "Intl\.\|toLocaleDateString\|toLocaleString\|toLocaleTimeString" mobile/src --exclude-dir=lib \| wc -l` = 0 (hoje 17); mesmas strings na tela (comparação manual no roteiro). |
| R12 | `isCatalogProvider` e o tipo `CatalogProvider` existem uma vez (módulo de catálogo). | Must | `grep -rn "function isCatalogProvider" mobile/src \| wc -l` = 1 (hoje 2). |
| R13 | Funções de leitura aceitam `signal?: AbortSignal` como **último parâmetro opcional**; TanStack Query repassa o `signal` nas consultas existentes. | Should | `tsc` passa; nenhuma chamada existente muda; as consultas do catálogo continuam passando `signal` (hoje já passam). |
| R14 | Invariantes preservados: validação zod de toda resposta, erro de API em toast e validação inline, `Stack.Protected`, tokens, nenhuma dependência nova, sem mudança visual, contrato HTTP intacto. | Must | `git diff mobile/package.json mobile/package-lock.json` vazio; `grep -rnE "#[0-9A-Fa-f]{6}" mobile/src/lib mobile/src/features` = 0; `npm run lint` e `npx tsc --noEmit` passam em cada passo. |
| R15 | Documentação atualizada na mesma mudança: ADR novo que atualiza o 0008 (cliente, 401 global, fábricas), `.docs/ARCHITECTURE.md` §4.3/§6.2, `.docs/DEVELOPMENT.md` §2.3 ("Hoje"), `.docs/DESIGN_SYSTEM.md` se citar `services/`. | Must | `.docs/ARCHITECTURE.md` §6.2 itens 2, 3, 4 e 8 marcados como resolvidos pela `0017`, e o ADR novo existe em `.adrs/`. |

## 5. Fluxos e estados

- **Caminho feliz:** tela chama função de serviço → `http-client` obtém a sessão, envia Bearer, valida com zod → dados entram no cache sob a chave da fábrica.
- **Vazio:** inalterado (204 → `undefined` onde o schema permite; listas vazias seguem os estados atuais).
- **Carregando:** inalterado; nenhum estado novo.
- **Erro:** 4xx/5xx → `ApiError` → toast com `detail` do servidor (pt-BR) ou a mensagem traduzida; rede → "Não foi possível conectar à API…"; contrato → "A API retornou dados em formato inesperado."; telas bloqueadas mantêm o estado "Tentar novamente".
- **Sucesso parcial:** não se aplica a esta spec (sem mudança de fluxo).
- **401 / sessão expirada:** um logout local, cache limpo, um toast, redirecionamento pelo `Stack.Protected`; nenhum retry; 401 subsequentes em voo são ignorados.
- **Cancelamento/retry:** `signal` aborta a requisição ao desmontar/trocar de chave; `retry: 1` do `QueryClient` segue para erros que não sejam 401; "Tentar novamente" das telas inalterado.

## 6. Acceptance Criteria

- [ ] Given `cd mobile`, When `npm run lint` e `npx tsc --noEmit`, Then ambos passam ao final de **cada** passo de migração.
- [ ] Given o código final, When rodo os greps das linhas R1, R4–R6, R8, R10–R12, Then todos retornam os valores-alvo da seção 10.
- [ ] Given a API em `Development` reiniciada com `Supabase__Audience` inválida, When abro `home` logado, Then o app volta ao login com um único toast de sessão expirada e sem laço de requisições (conferir a aba Network).
- [ ] Given a API desligada, When abro `home` e `search`, Then aparece o toast de conexão e o estado "Tentar novamente" funciona após religar a API.
- [ ] Given o roteiro manual da seção 10, When executo em Expo web e no Android, Then o comportamento visual e as strings são idênticos aos de antes da spec.
- [ ] Given `git diff --stat mobile/src/app mobile/src/components`, When reviso, Then só há mudança de import, chave/`staleTime`/paginação/formatação por helper e remoção de tratamento de 401 local; nenhuma mudança de JSX ou estilo.

## 7. O que fazer

Cada passo termina verde em `npm run lint` e `npx tsc --noEmit` e com smoke manual (login, `home`, `search`).

1. **Medir** a linha de base (seção 10) e registrá-la no plano. Verificar a limpeza do worktree do mobile.
2. **`lib/api-error` + `lib/http-client`** (R1, R2, R5, R13): `ApiError`, `toUserMessage`, cliente com `signal`; sem consumidores ainda.
3. **401 global** (R3, R4): `createQueryClient` com `onError` nos dois caches e `retry` que ignora 401; ligação com o `AuthProvider` para o logout local (sem revogar push); `useErrorToast` ignora 401; remover os 6 testes `'status' in` e as 11 strings. Ajustar `home.tsx` (R5).
4. **Migrar os três clientes**: `tracking-api` e `lists-api` primeiro (menores), depois `api-client` (R6); apagar `apiRequest`, `trackingRequest`, `request`.
5. **Dividir por domínio** (R7): um domínio por commit (account/onboarding, catalog, profile, social, notifications, gamification, ratings, lists, tracking); `services/api-client.ts` reexporta nomes idênticos; unificar `CatalogTitle`, `CatalogProvider`, `isCatalogProvider` (R12).
6. **Fábricas de chave** (R8, R9): `query-keys.ts` por domínio; trocar chaves inline por domínio, começando por catálogo (título único) e depois lists, social, notifications, gamification, ratings, profile, onboarding.
7. **`lib/pagination` e `lib/query/stale-time`** (R10): substituir os 10 `getNextPageParam` e os 24 `staleTime`, preservando o valor efetivo por consulta.
8. **`lib/format`** (R11): extrair os 17 usos e `getInitials` (a troca das 3 cópias de iniciais fica na `0018`).
9. **Documentação e ADR** (R15); rodar os greps da seção 10 e o roteiro manual; `commit-and-push` via `implement-me`.

## 8. O que não fazer

- Não renomear nem mudar assinatura de função de serviço usada pelas telas; não mover telas nem hooks (isso é a `0018`).
- Não mexer em JSX, estilos, tokens ou textos além de remover mensagens técnicas e o 401 local.
- Não adicionar dependência (axios, openapi codegen, zustand) nem segundo `QueryClient`.
- Não tentar `refreshSession` em laço nem retry de 401; não logar token, `Authorization` ou corpo de resposta.
- Não mudar o `staleTime`/`retry` efetivo de nenhuma consulta nem o contrato HTTP; não editar `package.json`/lockfile.
- Não escrever testes nem afirmar que "testes passaram" para o mobile: só `lint`, `tsc` e o roteiro manual.

## 9. Riscos e dependências

| Tipo        | Descrição | Mitigação |
| ----------- | --------- | --------- |
| Dependência | Nenhuma spec anterior; a `0018` depende desta. Trilha mobile independente do backend (mapa §4). | Entregar o barrel `services/api-client.ts` estável para a `0018` consumir. |
| Risco       | Regressão sem testes (Q2): trocar chave de cache ou `staleTime` muda quando o dado é refeito. | Um domínio por commit, valor efetivo preservado, roteiro manual por domínio e comparação de requisições no Network (Expo web). |
| Risco       | Invalidação: ao unificar chaves, `invalidateQueries` por prefixo antigo (`['lists']`, `['reviews']`, `['notification-inbox']`) deixa de atingir chaves novas, ou atinge demais. | Mapear antes cada `invalidateQueries` (incluindo `_layout.tsx` para notificações) para o prefixo da fábrica; roteiro: mutação → lista/Home atualizam. |
| Risco       | 401 global em laço: `signOut` chama `revokeRegisteredPushDevice`, que usa a API e daria novo 401. | Logout forçado local, sem revogar push, com guarda de idempotência. |
| Risco       | Deep links e callback: `auth/callback` e `auth/reset-password` não podem acionar o 401 global antes da sessão existir. | Roteiro manual de deep link (`reelix://auth/callback`) em `QA.md` §5.5; o tratamento só age em consultas/mutações do `QueryClient`. |
| Risco       | Ciclo de import `lists-api → api-client` e colisão de nomes ao dividir (`CatalogTitle`). | Catálogo é a base dos domínios; barrel com exportação nominal. |

## 10. Resultado

Como validar que a spec foi atendida:

- **Comando/verificação:** em `mobile/`: `npm run lint`; `npx tsc --noEmit`; greps abaixo (Git Bash); `wc -l`. Registrar saída real no relatório (`RULES.md` §6).

| Medida (comando, em `mobile/`)                                                                  | Hoje | Meta |
| ----------------------------------------------------------------------------------------------- | ---- | ---- |
| `grep -rn "await fetch(" src \| wc -l`                                                          | 3    | 1    |
| `wc -l src/services/api-client.ts`                                                              | 848  | ≤ 80 |
| `grep -rn "Sua sessão expirou" src \| wc -l`                                                    | 11   | 1    |
| `grep -rn "'status' in" src --exclude=auth-errors.ts \| wc -l`                                  | 6    | 0    |
| `grep -rn "HTTP \${" src \| wc -l`                                                              | 1    | 0    |
| `grep -rnE "queryKey: \[" src \| wc -l`                                                         | 82   | 0    |
| `grep -rn "getNextPageParam: (" src \| wc -l`                                                   | 10   | 0    |
| `grep -rnE "staleTime: [0-9]" src --exclude-dir=lib \| wc -l`                                   | 24   | 0    |
| `grep -rnE "Intl\.\|toLocaleDateString\|toLocaleString\|toLocaleTimeString" src --exclude-dir=lib \| wc -l` | 17 | 0 |
| `grep -rn "function isCatalogProvider" src \| wc -l`                                            | 2    | 1    |

- **Evidência esperada:** lint e tipos sem erro; tabela acima nos valores-alvo; `package.json` sem diff; ADR criado.
- **Roteiro manual mínimo (Expo web 375 px e Android):** (1) login, `home` com as seções, logout; (2) `search` por título e abrir detalhe; (3) detalhe de título: temporada e marcar episódio; (4) `tracking`: histórico e séries; (5) `lists`: favoritar, adicionar à lista; (6) `social` e `notifications` (feed e inbox); (7) `gamification` e `profile`; (8) 401 forçado (API com audiência inválida) e API desligada; (9) deep link `reelix://auth/callback` com erro simulado.
- **Comportamento observável:** telas e textos idênticos; sessão expirada devolve ao login com um toast.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| Premissa (Q2): sem testes mobile antes desta spec; proteção = `lint`, `tsc`, passos pequenos e roteiro manual. | Risco de regressão | Não | Usuário |
| Premissa: 401 em requisição autenticada encerra a sessão local direto, sem tentar `refreshSession` (o Supabase já renova antes do vencimento). Reversível. | Fluxo de sessão expirada | Não | Tech lead |
| Premissa: `staleTime` global 30 s, `retry: 1` e os valores 60 s/5 min por consulta permanecem; só viram constantes. | Frescor de cache | Não | Tech lead |
| Premissa: módulos de `features/<modulo>/api` e `query-keys.ts` nascem aqui (local final de D6); `hooks/` e `components/` são da `0018`. Divide o corte de forma diferente da descrição do mapa. | Corte entre `0017` e `0018` | Não | Tech lead |
| Premissa (Q6): Expo web só ambiente de desenvolvimento/teste; usado aqui apenas para inspecionar rede. | Verificação manual | Não | Usuário |
| Número do ADR novo: próximo livre em `.adrs/` no momento da implementação (o backend também gera ADR na `0013`). | Numeração | Não | Tech lead |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação da spec em modo lote (breakdown arquitetura/design/testes); linha de base medida no código. | Fundação de dados do mobile antes da `0018`. |
| 2026-11-23 | Implementada em fatias por domínio (plano `.plans/mobile-fundacao-http-e-dados-plan.md`, ADR `0013`). Premissas registradas: cliente chamado `httpRequest`; fallback genérico único para erro HTTP/contrato; 204 validado com `safeParse(undefined)`; chaves órfãs de temporada normalizadas sob `catalog`. Roteiro manual (Expo web e Android) não executado por agente. | Execução da spec via implement-me (modo orquestrado). |
