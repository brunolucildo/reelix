# 0015 — Organização por feature, hooks de dados e telas finas no mobile

- **Status:** aceito
- **Data:** 2026-11-24
- **Origem:** spec `0018` (`.specs/0018-mobile-features-e-telas-finas.md`); complementa o ADR 0013
- **Nota de numeração:** número definitivo atribuído pelo orquestrador na integração (era provisório `XXXX`).

## Contexto

Depois da fundação de dados (ADR 0013), o mobile ainda tinha organização por tipo (`components/<dominio>`), telas "god component" de 500 a 1.000 linhas com acesso direto a dados (80 chamadas de `useQuery`/`useMutation` em `src/app` e `src/components`, 20 `useQueryClient` fora de hooks), 7 `Redirect` de sessão duplicados apesar do `Stack.Protected`, primitivos copiados por tela (toggles, chips, pager, botões) e formulários não-auth com validação manual. Não há testes mobile (Q2), então a segurança da refatoração vem de `lint`, `tsc`, passos pequenos por feature e roteiro manual.

## Decisão

1. **Organização por feature:** `mobile/src/features/<modulo>/{api,hooks,components}` (mais `query-keys.ts` e `schemas.ts`) para account, auth, catalog, gamification, home, lists, notifications, onboarding, profile, ratings, social e tracking. `src/components` guarda só `ui/` (primitivos) e `navigation/` (cabeçalho, gaveta e botão de voltar, compartilhados). `services/api-client.ts` e `src/hooks` foram removidos.
2. **Telas finas:** rotas em `src/app` só leem parâmetros, chamam hooks e compõem componentes da feature (todas com no máximo 250 linhas). Dados e mutations vivem em hooks da feature, e cada mutation invalida o cache por prefixo da fábrica de chaves. Rota ou componente nunca chama `useQuery`/`useMutation`/`useQueryClient` nem importa valores de `api/` (só `import type`).
3. **Dependências entre features:** `catalog` é a base e não importa de nenhuma outra feature; composições que juntam catálogo com tracking, listas ou avaliações ficam na feature que as consome, ou na rota. Os parâmetros de rota do catálogo são validados em `features/catalog/route-params`.
4. **Guardas:** `Stack.Protected` em `_layout.tsx` é a única guarda de sessão; os `Redirect` de sessão saíram das telas (um frame com sessão nula é tratado por `useViewerId()` e `enabled`). Os dois `Redirect` de onboarding concluído continuam nas telas `home` e `onboarding`.
5. **Primitivos em `components/ui`:** `PrimaryButton`, `IconAction`, `ModalDialog`, `UserAvatar`, `ToggleRow`, `Chip`/`SegmentButton`/`TabButton`, `SmallButton`, `Pager`, `LoadingState`/`RetryState` e `ConfirmDialog`. Onde as cópias antigas tinham aparências diferentes, o primitivo as preserva por `variant`/props; unificar o visual é da spec `0019`. `ConfirmDialog` substitui `Alert.alert` (no-op no RN Web) e é a única mudança visual aceita.
6. **Formulários não-auth** com React Hook Form + zod (`features/<modulo>/schemas.ts`): listas, perfil, data assistida do tracking, comentário e seleções do onboarding, com as mesmas mensagens inline; erro de API segue em toast. Buscas de campo único sem envio ficam fora do RHF.
7. **Assets** importados por `@/assets/...` (alias já existente no `tsconfig.json`, validado com `expo export -p web`).

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| Manter `components/<dominio>` e só extrair hooks | Menos movimentação de arquivos | A tela continua conhecendo o tipo de pasta, não a feature; dependências cruzadas invisíveis | O alvo do mapa (D6) é organização por feature |
| Um PR único com os 11 módulos | Uma revisão | Sem testes, regressão difícil de localizar | Um commit verde por feature (R12) |
| Unificar a aparência dos primitivos duplicados na mesma mudança | Menos `variant` | Mudaria pixels sem a camada de design (`0019`/`0020`) | D1: sem mudança visual |
| Biblioteca de estado global para dados dos formulários | Sem prop drilling | Dependência nova e duplicação do estado do servidor | TanStack Query + RHF bastam; sem dependência nova |
| Manter o `Redirect` de sessão em cada tela | Robustez extra | Duplicação; o `Stack.Protected` já cobre | Guarda única em `_layout.tsx` |

## Consequências

- Positivas: nova funcionalidade vira "hook da feature + componente + tela fina"; mutations com invalidação por prefixo num lugar só; confirmações destrutivas funcionam também no web; formulários padronizados; sem dependência nova.
- Negativas: sem testes mobile, a regressão visual só é apanhada pelo roteiro manual; algumas combinações de estilo ficaram como `variant` (`ToggleRow`, `Pager`) até a `0019`; telas e hooks grandes foram divididos por responsabilidade e não por redesenho, então parte da duplicação de estilos entre seções continua.
- Impacto em código, operação e time: imports passam a `@/features/...`; novos módulos seguem `features/<modulo>/{api,hooks,components}`; `0019`/`0020` redesenham sobre os primitivos de `components/ui`; `0022`/`0023` testam hooks e fluxos sobre esta estrutura.
