# Plano — Mobile: fundação HTTP e dados

- **Status:** implementado; roteiro manual da seção 10 da spec pendente (não executável por agente)
- **Slug:** mobile-fundacao-http-e-dados
- **Spec:** `.specs/0017-mobile-fundacao-http-e-dados.md`
- **Data:** 2026-11-23

## Contexto

O mobile tinha três clientes HTTP copiados, `api-client.ts` de 848 linhas, 82 `queryKey` literais, 10 `getNextPageParam`, 24 `staleTime` numéricos, 17 usos soltos de `Intl`/`toLocale*` e "Sua sessão expirou" em 11 lugares, sem tratamento global de 401. Não há testes mobile (Q2): a proteção é `lint`, `tsc`, passos por domínio e roteiro manual.

## Objetivo

Um cliente, um erro tipado, um tratamento de 401, uma chave por recurso e formatação pt-BR única, com comportamento visual idêntico ao de antes.

## Não-objetivos

Mover telas e hooks (spec `0018`), mudança visual (`0019`/`0020`), testes (`0022`/`0023`), dependência nova, mudança de contrato HTTP, backend.

## Usuários e permissões

Sem mudança de permissão. O dono dos dados continua vindo do `sub` do JWT no servidor; o app só envia o Bearer da sessão Supabase.

## Fluxos e estados

Caminho feliz, vazio e carregando inalterados. Erro: 4xx/5xx viram `ApiError`; rede e contrato viram erros tipados com mensagens pt-BR. 401: um logout local, cache limpo, um toast, sem retry. Cancelamento: `signal` repassado ao `fetch`.

## Requisitos

R1 a R15 da spec (ver `.specs/0017-mobile-fundacao-http-e-dados.md` seção 4).

## Linha de base (medida em 2026-11-23, em `mobile/`)

| Medida | Antes | Depois | Meta |
| --- | --- | --- | --- |
| `grep -rn "await fetch(" src \| wc -l` | 3 | 1 | 1 |
| `wc -l src/services/api-client.ts` | 848 | 10 | ≤ 80 |
| `grep -rn "Sua sessão expirou" src \| wc -l` | 11 | 1 | 1 |
| `grep -rn "'status' in" src --exclude=auth-errors.ts \| wc -l` | 6 | 0 | 0 |
| `grep -rn "HTTP \${" src \| wc -l` | 1 | 0 | 0 |
| `grep -rnE "queryKey: \[" src \| wc -l` | 82 | 0 | 0 |
| `grep -rn "getNextPageParam: (" src \| wc -l` | 10 | 0 | 0 |
| `grep -rnE "staleTime: [0-9]" src --exclude-dir=lib \| wc -l` | 24 | 0 | 0 |
| `grep -rnE "Intl\.\|toLocaleDateString\|toLocaleString\|toLocaleTimeString" src --exclude-dir=lib \| wc -l` | 17 | 0 | 0 |
| `grep -rn "function isCatalogProvider" src \| wc -l` | 2 | 1 | 1 |

## Decisões e premissas

- Decisão: o cliente se chama `httpRequest` (não `apiRequest`) para que o grep de R6 por `function apiRequest` encontre 0 e não haja um quarto "cliente" com nome igual aos removidos.
- Decisão: 401 por sessão ausente no cliente (antes de chamar a API) lança `ApiError(401)`; o tratamento global é idempotente por ref e não mostra toast se a sessão já terminou.
- Decisão: `toUserMessage` devolve a mensagem de sessão para 401, a mensagem de rede/contrato para esses erros e o `fallback` do chamador para os demais `ApiError`; o texto técnico "HTTP n" some.
- Decisão: a unificação do detalhe de título muda o formato de chaves órfãs (`catalog-seasons`/`catalog-season` viram `['catalog','seasons'|'season',…]`) porque nenhuma invalidação usava o prefixo antigo; as demais chaves mantêm os valores.
- Premissa: a mensagem genérica de fallback dos três clientes antigos ("atualizar seu acompanhamento", "dados de acompanhamento em formato inesperado") passa a ser a genérica única; os hooks de tracking já exibem textos próprios por consulta e mutação.
- Premissa: 204 é validado com `schema.safeParse(undefined)` (todos os endpoints 204 usam `z.void()`/`z.undefined()`).
- Premissa: ADR da spec: `0013` (renumerado na integração).

## Abordagem (fatias; um commit por fatia/domínio, `lint` e `tsc` verdes em cada)

1. Linha de base medida (acima).
2. `lib/api-error` e `lib/http-client` (R1, R2, R5, R13).
3. 401 global: `createQueryClient`, `AuthProvider.expireSession`, `useErrorToast` ignora 401, remoção de `'status' in` e das mensagens duplicadas, `home.tsx` (R3 a R5).
4. Migração dos três clientes para `httpRequest` (R6).
5. Divisão por domínio: account, onboarding, catalog, profile, social, notifications, gamification, ratings, lists, tracking (R7, R12).
6. Fábricas de query key por domínio e troca dos literais (R8, R9).
7. `lib/pagination` e `lib/query/stale-time` (R10).
8. `lib/format` (R11).
9. Documentação, ADR (R15) e greps finais.

## Arquivos previstos

`mobile/src/lib/**`, `mobile/src/features/<modulo>/{api,query-keys}`, `mobile/src/services/api-client.ts` (barrel), `mobile/src/providers/{auth,toast}-provider.tsx`, `mobile/src/app/_layout.tsx`, telas e componentes (imports, chaves, `staleTime`, paginação, formatação), `.docs/{ARCHITECTURE,DEVELOPMENT,DESIGN_SYSTEM}.md`, `.adrs/0013-*.md`, `.adrs/0008-*.md` (nota de atualização).

## Testes

- Unitário/integração/E2E: não existem no mobile (Q2); a `0022` cria o runner.
- Estático: `npm run lint`, `npx tsc --noEmit`, `npm audit --audit-level=high` e os greps acima.
- Manual: roteiro da seção 10 da spec (Expo web 375 px e Android), pendente.

## Riscos e rollback

- Risco: regressão sem testes (chave de cache, `staleTime`, invalidação). Mitigação: um domínio por commit, valores efetivos preservados (19 × `catalog`, 4 × `short`), roteiro manual por domínio.
- Risco: 401 em laço. Mitigação: logout local sem revogar push, guarda de idempotência, sem retry de 401.
- Rollback: reverter os commits da branch (cada fatia é independente); `package.json` e lockfile não mudaram.

## Critério de pronto

- [x] `npm run lint` e `npx tsc --noEmit` passam ao final de cada fatia.
- [x] Greps da tabela nos valores-alvo; `git diff` de `package.json` e `package-lock.json` vazio; `npm audit --audit-level=high` sem high/critical.
- [x] ADR criado; ARCHITECTURE §4.3/§6.2, DEVELOPMENT §2.3 e DESIGN_SYSTEM atualizados.
- [ ] Roteiro manual da seção 10 executado em Expo web e Android (pendente; pessoa responsável).

## Perguntas abertas

| Pergunta | Impacto | Bloqueante? | Quem decide |
| -------- | ------- | ----------- | ----------- |
| Número definitivo do ADR | Numeração | Não | Orquestrador |
