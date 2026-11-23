# XXXX — Cliente HTTP único, 401 global e fábricas de chave no mobile

- **Status:** aceito
- **Data:** 2026-11-23
- **Origem:** spec `0017` (`.specs/0017-mobile-fundacao-http-e-dados.md`); atualiza o ADR 0008
- **Nota de numeração:** o prefixo `XXXX` é provisório (execução orquestrada); o número definitivo é atribuído na integração.

## Contexto

O ADR 0008 fixou TanStack Query e Zod no mobile, mas deixou três cópias do helper de requisição (`apiRequest`, `trackingRequest`, `request`), um `api-client.ts` de 848 linhas, 82 `queryKey` literais (o mesmo título sob pelo menos quatro chaves), `getNextPageParam` reescrito 10 vezes, 24 `staleTime` numéricos, 17 usos soltos de `Intl`/`toLocale*` e a mensagem de sessão expirada em 11 lugares, sem tratamento global de 401: a tela mostrava o toast e a pessoa ficava presa. Não há testes mobile antes desta mudança (Q2), então o corte precisava ser pequeno, por domínio e sem alterar o comportamento visual.

## Decisão

1. **Um cliente HTTP** em `mobile/src/lib/http-client.ts` (`httpRequest`): base de `services/api-url.ts`, `Authorization: Bearer` da sessão Supabase, parse de `ProblemDetails`, validação zod, 204 sem corpo e `AbortSignal`. É o único `fetch` à API.
2. **Erros tipados** em `lib/api-error.ts`: `ApiError` (`status`, `title`, `detail`; `message` = `detail ?? title`), `ApiNetworkError` e `ApiContractError`, e **um** tradutor `toUserMessage(error, fallback)` em pt-BR. Texto técnico ("HTTP n") não chega à tela; a mensagem de sessão expirada é uma constante.
3. **401 global** em `lib/query/create-query-client.ts`: `QueryCache.onError` e `MutationCache.onError` notificam um único listener; o `AuthProvider.expireSession()` encerra a sessão **local** (sem revogar o dispositivo de push, que daria novo 401), limpa o cache e é idempotente (guarda por ref); um toast; `retry` não se aplica a 401 e `useErrorToast` ignora 401. Sem `refreshSession` em laço.
4. **Divisão por domínio** de `services/api-client.ts` em `features/<modulo>/api/*-api.ts` (account, onboarding, catalog, profile, social, notifications, gamification, ratings, lists, tracking), com schemas zod junto das funções. `services/api-client.ts` vira barrel temporário (`export *`) com os mesmos nomes até a spec `0018`; `CatalogTitle`, `CatalogProvider` e `isCatalogProvider` existem uma vez, em `features/catalog`.
5. **Fábricas de query key** em `features/<modulo>/query-keys.ts` (modelo `trackingQueryKeys`): uma chave por recurso (detalhe de título em `catalogQueryKeys.title`, usada por todas as telas) e invalidação por prefixo da fábrica; chaves órfãs de temporada entram no catálogo.
6. **Utilitários de `lib/`:** `lib/pagination` (cursor `nextCursor`; página numérica por contagem e por total de páginas), `lib/query/stale-time` (`default` 30 s, `short` 60 s, `catalog` 5 min; valores efetivos preservados) e `lib/format` (datas e números pt-BR, `getInitials`).

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| Manter três helpers e só centralizar mensagens | Menos mudança | A duplicação continua; 401 segue por tela | Não resolve a causa; a spec exige um ponto único |
| Interceptor de biblioteca (axios) | Interceptors prontos | Dependência nova; `fetch` já basta | R14 proíbe dependência nova |
| Cliente gerado do OpenAPI | Contrato único | Etapa de build; fora do escopo | Já descartado no ADR 0008 |
| Tentar `refreshSession` e repetir a requisição no 401 | Sessão "se cura" | Risco de laço; o Supabase já renova antes do vencimento | Premissa da spec: 401 encerra a sessão local; reversível |
| Chamar `signOut()` completo no 401 | Reaproveita código | Revoga push pela API e recebe outro 401 | Logout local dedicado (`expireSession`) |
| `export { ... }` nominal no barrel | Mais explícito | Passaria de 80 linhas e duplicaria a lista de nomes | `export *` por módulo; o `tsc` acusa colisão de nomes |

## Consequências

- Positivas: um lugar para cada decisão de dados; sessão expirada devolve ao login com um toast, sem laço; cache compartilhado entre telas para o mesmo título; mensagens de erro e formatação consistentes; novos módulos nascem sobre `httpRequest`, fábricas e `lib/`.
- Negativas: `export *` no barrel mascara de onde vem cada nome até a `0018`; sem testes mobile (a `0022` cobre), a proteção foi `lint`, `tsc`, passos por domínio e roteiro manual; unificar o detalhe de título compartilha cache entre telas que antes buscavam separado (esperado, dentro do mesmo `staleTime`).
- Impacto em código, operação e time: função de serviço nova usa `httpRequest` e fica em `features/<modulo>/api`; `queryKey` literal, `staleTime` numérico, `fetch` direto e `Intl` solto em tela passam a ser anti-padrões; a `0018` migra telas e hooks para `features/` e remove o barrel.

## Relação com o ADR 0008

Este ADR atualiza o 0008: o "cliente HTTP mínimo" passa a ser `lib/http-client`, o `QueryClient` ganha tratamento global de 401 e as chaves passam a ser fábricas por domínio. TanStack Query, Zod, toast para erro de API e validação inline continuam como decididos lá.
