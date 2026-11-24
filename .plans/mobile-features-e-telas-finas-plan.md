# Plano — Mobile: features e telas finas

- **Status:** implementado; roteiro manual da seção 10 da spec **não executado** (execução orquestrada sem dispositivo nem `mobile/.env`)
- **Slug:** mobile-features-e-telas-finas
- **Spec:** `.specs/0018-mobile-features-e-telas-finas.md`
- **Data:** 2026-11-24

## Contexto

Depois da `0017`, o mobile ainda tinha organização por tipo, telas de 500 a 1.000 linhas com 80 chamadas de `useQuery`/`useMutation` em rotas e componentes, 20 `useQueryClient` fora de hooks, 9 `Redirect` (7 de sessão duplicados), primitivos copiados por tela, formulários não-auth sem zod/RHF e 65 imports relativos de assets. Não há testes mobile (Q2).

## Objetivo

Organização por feature (`features/<modulo>/{api,hooks,components}`), telas finas que só compõem, guarda de sessão única, formulários com RHF + zod, primitivos em `components/ui` e `ConfirmDialog` no lugar de `Alert.alert`, **sem mudança visual** (exceto as 2 confirmações da R9).

## Não-objetivos

Evolução visual (`0019`/`0020`), testes (`0022`/`0023`), contrato HTTP, backend, novas telas, tema claro/tablet, dependência nova, edição de `mobile/ios` e `mobile/android`.

## Usuários e permissões

Sem mudança. O dono dos dados vem do `sub` do JWT no servidor; hooks usam `enabled`/`useViewerId()` para o frame com sessão nula.

## Fluxos e estados

Inalterados: carregando, vazio, erro em toast + "Tentar novamente", validação inline, seções da Home com falha isolada. Sessão que some: `Stack.Protected` desmonta as rotas privadas. Confirmação destrutiva: `ConfirmDialog` (cancelar fecha; confirmar executa e fecha).

## Requisitos

R1 a R13 da spec (estrutura, zero acesso a dados em rotas/componentes, invalidação por prefixo, telas finas, `Redirect`, formulários, primitivos, `LoadingState`/`RetryState`, `ConfirmDialog`, alias `@/assets`, sem regressão de UI, gate por fatia, docs e ADR).

## Decisões e premissas

- Um commit por fatia, `lint` e `tsc` verdes em cada um; "mover antes de reescrever".
- Cópias de primitivos com aparências diferentes viram `variant`/props (`ToggleRow`: `divided`/`plain`/`privacy`; `Pager`: `pages`/`count`); unificar é da `0019`.
- `SocialAvatar` fica em `features/social/components` (inicial em `typography.body`, diferente do `UserAvatar`) e passa a usar `getInitials`.
- `isCatalogProvider`/`isCatalogContentType` chegam às rotas por `features/catalog/route-params` (a regra proíbe importar valores de `api/` em `.tsx`).
- `AuthScreen` fica em `features/auth/components` e é importado pelo tracking.
- Buscas de campo único sem envio ficam fora do RHF (premissa da spec); o RHF cobre lists, profile, tracking (data), ratings (comentário) e onboarding (seleções).
- `runMutation` de listas continua mostrando `error.message` (comportamento anterior); não foi trocado por `toUserMessage` para não mudar textos.

## Abordagem (fatias)

0. `components/ui` (primitivos movidos e novos) e alias `@/assets` (validado com `expo export -p web`).
1. catalog (busca, título, episódio) e `use-tracking` movido para `features/tracking/hooks`.
2. tracking (`tracking.tsx` em 5 seções; data assistida com RHF; `ConfirmDialog`).
3. lists (hooks de consulta, busca, detalhes e mutations; componentes e diálogo; RHF).
4. home/discovery (hooks e componentes em `features/home`; `useTitleCollectionActions`).
5. ratings (`RatingReviewSection` dividido; comentário com RHF; `ConfirmDialog`; my-ratings).
6. social (tela, perfil de amigo, cartão de atividade).
7. notifications (hooks; `useOpenNotification` sai de `_layout.tsx`; `AppHeader`/`UserDrawer` por hooks).
8. gamification.
9. profile (RHF + zod; `ToggleRow` privacy).
10. onboarding (`useOnboardingFlow`; seleções com RHF + zod).
11. auth (`components/auth` e `schemas/auth-forms` para `features/auth`; `Redirect` de reset-password).
12. Limpeza (barrel `services/api-client.ts`, `src/hooks`), docs (ARCHITECTURE §4.3/§6.2, DEVELOPMENT §2.3, DESIGN_SYSTEM), ADR.

## Arquivos previstos

`mobile/src/**` (movimentação e novos hooks/componentes), `.docs/ARCHITECTURE.md` (§4.3 e §6.2), `.docs/DEVELOPMENT.md` (§2.3), `.docs/DESIGN_SYSTEM.md`, `.adrs/0015-organizacao-por-feature-e-telas-finas-no-mobile.md`, `.specs/0018-...md`, este plano.

## Testes

Sem runner no mobile. Por fatia: `npm run lint` e `npx tsc --noEmit`. No fim: `npm audit --audit-level=high`, `npx expo export -p web` (valores fictícios) e os greps da seção 10 da spec. Roteiro manual por feature (seção 10) a executar pela pessoa em Expo web 375 px.

## Riscos e rollback

Regressão visual/comportamental sem testes (mitigação: fatias pequenas e roteiro). Cache/invalidação: cada mutation foi mapeada para o mesmo prefixo de antes. Frame com sessão nula: `enabled` e `useViewerId()`. Rollback: reverter os commits da fatia (cada feature é um commit).

## Critério de pronto

Tabela da seção 10 nos valores-alvo; `lint`, `tsc` e `audit` verdes; `package.json`/lockfile sem diff; docs e ADR atualizados; roteiro manual executado pela pessoa (pendente).

## Perguntas abertas

Nenhuma bloqueante.
