# 0018 — Mobile: features e telas finas

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** implementada (roteiro manual da seção 10 pendente: não executado na execução orquestrada)
- **Slug:** mobile-features-e-telas-finas
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-24
- **Owner:** Tech lead (a confirmar)
- **Relacionados:** mapa `.plans/arquitetura-design-testes-breakdown.md` (D1, D6, D8, D10, D11; Q2, Q3, Q6); ADR 0008 (`.adrs/0008-tanstack-query-e-zod-para-estado-de-servidor-no-mobile.md`); `.docs/ARCHITECTURE.md` §4.3 e §6.2; `.docs/DEVELOPMENT.md` §2.3, §4 e §12; `.docs/DESIGN.md` e `.docs/DESIGN_SYSTEM.md`; depende de `0017`; specs vizinhas: `0019`/`0020` (design), `0022` (Jest/RNTL), `0023` (Playwright)

## 1. Escopo

### Dentro do escopo

- Organização por feature: `src/features/<modulo>/{api,hooks,components}` para catalog, home (discovery), tracking, lists, ratings, social, notifications, gamification, profile, onboarding e auth. O `api/` e o `query-keys.ts` já nascem na `0017`; aqui entram `hooks/` e `components/`.
- Hooks de query/mutation por feature; telas só compõem e tratam estados (carregando, vazio, erro).
- Quebra das telas "god component" com **checklist por tela** (seção 7) e meta de tamanho verificável.
- Remoção dos `Redirect` de sessão duplicados em favor do `Stack.Protected` (`_layout.tsx`).
- Formulários não-auth com React Hook Form + zod: lists, profile, tracking (data assistida), reviews (comentário), onboarding.
- Primitivos duplicados **movidos** para `components/ui` (só mover e deduplicar lógica): `PrimaryButton`, `PreferenceToggle`/`PrivacyToggle`, `SelectionChip`/`SegmentButton`/`SectionButton`/`SmallButton`, `Pager`/`Pagination`, avatar (iniciais em 3 cópias), `IconAction`, `ModalDialog`, `UserAvatar`.
- `Alert.alert` de confirmação substituído por `ModalDialog` (`tracking.tsx:190`, `rating-review-section.tsx:185`); imports de assets com alias `@/assets` (65 relativos); remoção do barrel `services/api-client.ts` e de `src/hooks`.

### Fora do escopo

- Evolução visual de qualquer primitivo, tokens, navegação, estados redesenhados: specs `0019` e `0020` (aqui, **sem mudança visual**, exceto as 2 confirmações da R9).
- `http-client`, `ApiError`, 401 global, fábricas de chave, paginação/`staleTime`/`lib/format`: spec `0017` (pré-requisito).
- Testes automatizados (Jest/RNTL, Playwright): specs `0022` e `0023`. Não há testes mobile (Q2): a segurança vem de `lint`, `tsc`, slices pequenos e do roteiro manual por feature (R12).
- Contrato HTTP, backend (`0013`–`0016`), novas telas ou funcionalidades, tema claro/tablet (Q3), Expo web como produto (Q6).

## 2. Contexto

Medido em 2026-11-19 em `mobile/src` (12.496 linhas; `src/app` 6.858):

- **Telas gigantes** (cada uma com seu `StyleSheet` no fim do arquivo): `lists.tsx` 995, `tracking.tsx` 903, `onboarding.tsx` 748, `gamification.tsx` 638, `components/reviews/rating-review-section.tsx` 771, `social.tsx` 573, `title/[provider]/[contentType]/[externalId].tsx` 559, `profile.tsx` 521; `tracking.tsx` tem 19 ocorrências de `useState` e `lists.tsx` 17.
- **Dados na tela:** 80 chamadas de `useQuery`/`useInfiniteQuery`/`useQueries`/`useMutation` em `src/app` (52) e `src/components` (28); 20 `useQueryClient` fora de hooks; 25 imports de `@/services/(api-client|lists-api|tracking-api)` em `.tsx`. `lists.tsx` nem usa `useMutation`: chama 17 funções de serviço à mão num `runMutation` próprio. Só existe `hooks/use-tracking.ts` (170 linhas).
- **Guardas duplicadas:** 9 `<Redirect` em 8 arquivos. 7 são guardas de sessão que o `Stack.Protected` já cobre (`auth/reset-password`, `episode/...`, `home:51`, `lists`, `search`, `title/...`, `tracking`); 2 são de **onboarding**, não de sessão (`home.tsx:75` → `/onboarding`; `onboarding.tsx:56` → `/home`).
- **Primitivos duplicados:** `PreferenceToggle` em `gamification.tsx:393` e `notifications.tsx:203`, `PrivacyToggle` em `profile.tsx:296` (mesma estrutura com `Switch`, estilos e props diferentes); `SelectionChip` e `SegmentButton` (`onboarding`), `SectionButton`/`SmallButton`/`Pager` (`lists`), `Pagination` (`tracking`, comportamento e aparência diferentes do `Pager`); `PrimaryButton` mora em `components/auth/primary-button.tsx` e tem 21 usos fora de auth; iniciais do avatar em `profile.tsx:101`, `social.tsx:430` e `user-avatar.tsx:31`.
- **Formulários:** `useForm` só em 4 arquivos de auth; 8 telas/componentes não-auth têm `TextInput` com validação manual em `useState`.
- **Confirmação:** `Alert.alert` é no-op no RN Web (`.docs/DEVELOPMENT.md` §4; `QA.md` §5.2); `ModalDialog` já tem 5 usos.
- **Assets:** 65 imports relativos `../../assets/...` (24 em `app`, 41 em `components`); o alias `@/assets/*` já está no `tsconfig.json` e tem 0 usos.

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Quem mantém o app; indiretamente o usuário, que ganha confirmações que funcionam também no web e menos regressões. |
| Decisão ou ação que muda | Nova funcionalidade passa a ser "hook da feature + componente + tela fina", sem tocar em arquivo de 1.000 linhas; a `0019`/`0020` redesenham sobre primitivos únicos. |
| Resultado esperado       | Telas ≤ 250 linhas, sem acesso direto a dados, guardas únicas, formulários padronizados, primitivos em um lugar, sem mudança visual. |
| Métrica de sucesso       | Metas da seção 10 (hooks de dados na tela 80→0, maior tela 995→≤250, `Redirect` 9→2, `Alert.alert` 2→0, imports relativos de assets 65→0). |

## 4. Requisitos

Decisões do mapa copiadas: D1 (sem mudar comportamento), D6 (features, telas finas, primitivos em `components/ui`, RHF + zod), D8 (pt-BR, toast para erro de API, validação inline, 44 px), D10, D11.

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | Estrutura `src/features/<modulo>/{api,hooks,components}` para os 11 módulos; `src/hooks` e `services/api-client.ts` removidos; `src/components` fica só com `ui/` e `navigation/`. Dependência: `catalog` e `lib` são a base; `catalog` não importa de outra feature. | Must | Given o código final, When `ls mobile/src/components`, Then só `navigation ui`; `ls mobile/src/hooks mobile/src/services/api-client.ts` falha; `grep -rn "@/features/" mobile/src/features/catalog \| grep -v "@/features/catalog" \| wc -l` = 0. |
| R2  | Nenhuma tela, componente `.tsx` ou `_layout.tsx` usa `useQuery`/`useInfiniteQuery`/`useQueries`/`useMutation`/`useQueryClient` nem importa valores da camada `api`; só hooks da feature (`features/<m>/hooks`). Tipos entram por `import type`. | Must | `grep -rnE "useQuery\(\|useInfiniteQuery\(\|useQueries\(\|useMutation\(" mobile/src --exclude-dir=hooks \| wc -l` = 0 (hoje 80); `grep -rn "useQueryClient" mobile/src/app mobile/src/components \| wc -l` = 0 (hoje 20); `grep -rnE "from '(@/features/[a-z-]+/api[^']*\|@/services/(api-client\|lists-api\|tracking-api))'" mobile/src --include=*.tsx \| grep -v "import type" \| wc -l` = 0 (hoje 25). |
| R3  | Cada mutation invalida/atualiza o cache **por prefixo da fábrica** dentro do hook da feature (inclui o `runMutation` de `lists`, a invalidação do `profile`, a de notificações em `_layout.tsx`); o comportamento de atualização de Home/listas/histórico é o mesmo de hoje. | Must | Given uma mutation (ex.: favoritar), When concluída, Then Home e `lists` refletem o novo estado sem recarregar (roteiro R12); `grep -rn "invalidateQueries" mobile/src/app mobile/src/components \| wc -l` = 0. |
| R4  | **Telas finas** (checklist da seção 7): arquivo de rota em `src/app` ≤ 250 linhas; componente em `features/*/components` ≤ 300; hook ≤ 200; estado de campo de formulário fora da rota; ≤ 5 `useState` por arquivo de rota. | Must | `find mobile/src/app -name "*.tsx" -exec wc -l {} + \| sort -rn \| head -3` → maior ≤ 250 (hoje 995); `find mobile/src/features -path "*components*" -name "*.tsx" -exec wc -l {} + \| sort -rn \| head -3` → maior ≤ 300; `RatingReviewSection` ≤ 250 (hoje 771). |
| R5  | Os 7 `Redirect` de sessão são removidos; ficam só os 2 de onboarding (`home` e `onboarding`). As telas não testam `session` para redirecionar. | Must | `grep -rn "<Redirect" mobile/src/app \| wc -l` = 2 (hoje 9); Given logout em qualquer tela privada, When a sessão some, Then o app vai a `/` sem tela em branco nem erro no console. |
| R6  | Formulários não-auth usam RHF + `zodResolver`, schemas em `features/<m>/schemas.ts` (o `schemas/auth-forms.ts` vai para `features/auth`), com **as mesmas** mensagens inline, limites e regras de hoje (nome da lista, nome ≤ 50/bio ≤ 280 do perfil, data assistida do tracking, comentário, busca/etapas do onboarding). Erro de API continua em toast. | Must | `grep -rl "useForm" mobile/src/features` lista `auth`, `lists`, `profile`, `tracking`, `ratings`, `onboarding` (hoje só `auth`); roteiro: cada campo inválido mostra a mesma mensagem inline e nenhum toast de validação. |
| R7  | Primitivos duplicados movidos para `components/ui` com lógica única: `PrimaryButton`, `ToggleRow` (para `PreferenceToggle`/`PrivacyToggle`), `Chip`/`SegmentButton`/`SmallButton`, `Pager` (cálculo de páginas compartilhado), `UserAvatar` e `getInitials` (de `lib/format`, `0017`). Quando as cópias diferem em estilo ou `accessibilityHint`, o primitivo preserva cada aparência por prop/`variant`; unificar a aparência é da `0019`. | Must | `grep -rnE "function (PreferenceToggle\|PrivacyToggle\|SelectionChip\|SegmentButton\|SectionButton\|SmallButton\|Pager\|Pagination)\b" mobile/src \| grep -v "components/ui" \| wc -l` = 0 (hoje 9); `ls mobile/src/components/auth/primary-button.tsx` falha; `grep -rn "slice(0, 1)" mobile/src \| wc -l` = 1 (hoje 3). |
| R8  | `LoadingState`/`RetryState` do `tracking.tsx` viram primitivo em `components/ui` (toast + "Tentar novamente"). | Should | `grep -rn "function RetryState\|function LoadingState" mobile/src \| grep -v "components/ui" \| wc -l` = 0 (hoje 2). |
| R9  | `Alert.alert` removido: `ConfirmDialog` (em `components/ui`, sobre `ModalDialog` e `PrimaryButton`) com o mesmo título, texto e rótulos ("Desmarcar como assistido?", "Excluir comentário?"). Única mudança visual aceita, pois o alerta nativo deixa de existir. | Must | `grep -rn "Alert.alert" mobile/src \| wc -l` = 0 (hoje 2); no Expo web, desmarcar episódio e excluir comentário abrem o diálogo e confirmam/cancelam. |
| R10 | Imports de assets usam `@/assets/...`. Validar o alias no bundler com 1 arquivo antes da troca em massa. | Must | `grep -rn "import.*assets/" mobile/src \| grep -v "@/assets" \| wc -l` = 0 (hoje 65); `npx tsc --noEmit` passa; o app abre no Expo web com ícones SVG e marca visíveis. |
| R11 | Sem regressão das regras de UI: erro de API em toast, validação/sucesso inline, só tokens, alvos de 44 px, `accessibilityRole`/`Label` mantidos, `Stack.Protected` intacto, nenhuma dependência nova. | Must | `grep -rnE "#[0-9A-Fa-f]{6}" mobile/src/app mobile/src/components mobile/src/features \| wc -l` = 0; `git diff mobile/package.json mobile/package-lock.json` vazio; contagem de `accessibilityRole` em `src` (hoje 164) não diminui. |
| R12 | Cada slice entrega verde em `npm run lint` e `npx tsc --noEmit` **e** executa o roteiro manual mínimo da feature (seção 10), registrado na PR. | Must | Evidência (comandos e roteiro) no relatório de cada slice (`RULES.md` §6). |
| R13 | Documentação atualizada ao fim: `.docs/ARCHITECTURE.md` §4.3/§6.2, `.docs/DEVELOPMENT.md` §2.3 ("Hoje"), `.docs/DESIGN_SYSTEM.md` (caminhos de `components/ui`), `mobile/AGENTS.md` se citar caminhos; ADR novo para a organização por feature. Wireframe **não** muda (sem mudança visual). | Must | `.docs/ARCHITECTURE.md` §6.2 itens 1, 3, 5, 6 e 7 marcados como resolvidos pela `0018`; ADR existe em `.adrs/`. |

## 5. Fluxos e estados

- **Caminho feliz:** a rota lê parâmetros, chama hooks da feature e compõe componentes; o usuário vê as mesmas telas de hoje.
- **Vazio / Carregando / Erro / Sucesso parcial:** inalterados; erro de API em toast + estado "Tentar novamente"; seções da Home e do tracking que falham isoladas continuam sem bloquear as demais.
- **Sessão que some:** o `Stack.Protected` desmonta as rotas privadas; hooks não disparam consulta sem sessão (há um frame possível com sessão nula).
- **Confirmação destrutiva / retry:** `ConfirmDialog` (cancelar fecha; confirmar executa e fecha; `busy` bloqueia o fechamento); `signal` e `retry` da `0017` valem e "Tentar novamente" chama o `refetch` do hook.

## 6. Acceptance Criteria

- [ ] Given `cd mobile`, When `npm run lint` e `npx tsc --noEmit` ao fim de cada slice, Then ambos passam.
- [ ] Given o código final, When executo os comandos da seção 10, Then todos atingem a meta.
- [ ] Given cada tela da seção 7, When abro o arquivo de rota, Then ele só compõe hooks/componentes da feature e respeita as metas de tamanho.
- [ ] Given o roteiro manual de cada feature (seção 10) em Expo web (375 px) e Android, When executo antes e depois do slice, Then telas, textos, toasts e mensagens inline são idênticos.
- [ ] Given logout ou 401 em tela privada, When a sessão some, Then o app volta ao login sem tela em branco; e ao desmarcar episódio/excluir comentário no Expo web o `ConfirmDialog` só confirma a ação ao confirmar.

## 7. O que fazer

**Execução:** um slice por feature, nesta ordem; cada slice é um commit/PR verde (`implement-me` + `commit-and-push`). Em cada um: mover `components/<x>` e hooks para `features/<m>`, trocar imports para `@/features/...` e `@/assets/...` dos arquivos tocados, escrever hooks, afinar telas, remover `Redirect` de sessão.

0. **Base:** `components/ui` (`PrimaryButton`, `IconAction`, `ModalDialog`, `UserAvatar`, `ToggleRow`, `Chip`/`SegmentButton`/`SmallButton`, `Pager`, `LoadingState`/`RetryState`, `ConfirmDialog`) e validação do alias `@/assets` (1 arquivo, bundle no Expo web).
1. **catalog** (`search`, `title/...`, `episode/...`): hooks `useCatalogTitle`, `useCatalogSeasons`, `useCatalogSeason`, busca; `isCatalogProvider` já único (`0017`).
2. **tracking**: `hooks/use-tracking.ts` vira `features/tracking/hooks`.
3. **lists**, 4. **home/discovery** (`components/discovery/*`), 5. **ratings**, 6. **social**, 7. **notifications**, 8. **gamification**, 9. **profile/settings**, 10. **onboarding**, 11. **auth** (`components/auth/*`, `schemas/auth-forms.ts`, `auth/callback`, `auth/reset-password`).
12. **Limpeza:** remover o barrel `services/api-client.ts`, `src/hooks`, rodar todos os comandos da seção 10, atualizar docs e ADR (R13).

**Checklist por tela** (meta comum: rota ≤ 250 linhas, 0 hooks de dados, ≤ 5 `useState`):

| Tela (linhas hoje) | O que sai da tela | Destino | Particularidades |
| ------------------ | ----------------- | ------- | ---------------- |
| `lists.tsx` (995) | 7 queries; 17 chamadas de serviço em `runMutation`; 17 `useState`; `SectionButton`, `SmallButton`, `ListActionButton`, `Pager`; 223 linhas de estilos | `features/lists/hooks` (consultas + mutations com invalidação), `components` (lista de listas, detalhe, picker de títulos, formulário), `ui` (botões, `Pager`) | Form RHF (nome, descrição); `ModalDialog` do editor preservado; sem `Redirect` (linha 150) |
| `tracking.tsx` (903) | 4 queries; 19 `useState`; `Alert.alert` (l.190); `Pagination`, `LoadingState`, `RetryState`; `toLocalDateTimeInput`/`parseLocalDateTimeInput` | `features/tracking/hooks`, `components` (histórico, séries, pendentes, editor de data), `lib/format` | Form RHF para a data assistida com a mesma validação; `ConfirmDialog` (R9); sem `Redirect` (l.42) |
| `onboarding.tsx` (748) | 4 queries + 2 mutations; `SegmentButton`, `SelectionChip`, `TitleResult` | `features/onboarding/hooks`, `components` (uma por etapa), `ui` (chips) | Form RHF + zod por etapa; **manter** o `Redirect` de onboarding concluído (l.56) |
| `gamification.tsx` (638) | 4 queries + 1 mutation; `StatisticsSummary`, `StatTile`, `AchievementCard`, `PreferenceToggle`, `groupActivitiesByMonth` | `features/gamification/hooks`, `components`, `ui` (`ToggleRow`) | Ranking/estatísticas continuam com seus estados de erro isolados |
| `rating-review-section.tsx` (771) | 2 queries + 5 mutations; `AudienceSelector`, `ToggleButton`, `CommentCard`; `Alert.alert` (l.185); chaves com `viewerId` | `features/ratings/hooks`, `components` (nota, formulário, lista de comentários), `ConfirmDialog` | Form RHF (comentário, spoiler, audiência); `RatingReviewSection` ≤ 250 |
| `social.tsx` (573) | 5 queries + 4 mutations; `SocialAvatar`; `ModalDialog` "Remover amizade?" | `features/social/hooks`, `components` (busca, solicitações, amigos, feed), `ui` (avatar) | Abas e paginação por cursor preservadas |
| `title/[provider]/[contentType]/[externalId].tsx` (559) | 3 queries; `MovieTrackingControls`, `MovieWatchedAction`, `formatCatalogDate`, `isCatalogContentType` | `features/catalog/hooks` e `components` (cabeçalho, temporadas, episódio, ação de filme) | Valida parâmetros da rota como hoje; sem `Redirect` (l.127); deep links inalterados |
| `profile.tsx` (521) | 2 queries + 1 mutation; `ProfileEditor` (rascunho em `useState`), `PrivacyToggle`; iniciais | `features/profile/hooks`, `components` (editor, avatar, privacidade), `ui` (`ToggleRow`, avatar) | Form RHF (nome ≤ 50, bio ≤ 280); invalida `profile` e `reviews` por prefixo |

Telas menores sem hook direto ao fim (R2): `notifications` (365), `social-profile/[accountId]` (340), `search` (238), `home` (217), `episode/...` (170), `my-ratings` (114), `settings`, e os componentes de `discovery`, `navigation` (`AppHeader`, `UserDrawer`) e `social-activity-card`.

## 8. O que não fazer

- Não mudar JSX, estilos, tokens, textos, rotas ou deep links; não tocar no contrato HTTP nem nos schemas da `0017` (exceto mover); não criar primitivo com visual novo nem unificar aparências diferentes (`0019`) nem redesenhar telas (`0020`).
- Não mover tudo de uma vez: nada de PR único com os 11 módulos; não deixar o app quebrado entre slices.
- Não adicionar dependência, store global, `Redirect` novo nem `useQuery` em tela/componente; não editar `package.json`, lockfile nem `mobile/ios`/`android`.
- Não escrever testes nem afirmar que "testes passaram" para o mobile; só `lint`, `tsc` e o roteiro manual.

## 9. Riscos e dependências

| Tipo        | Descrição | Mitigação |
| ----------- | --------- | --------- |
| Dependência | Depende da `0017` (http-client, 401 global, fábricas de chave, `lib/format`, `lib/pagination`). `0019`/`0020` só começam após esta. | Não iniciar o slice 1 antes do barrel e das fábricas da `0017` estarem em `main`. |
| Risco       | Regressão sem testes (Q2) em telas de 500–1.000 linhas. | Slices pequenos, roteiro manual por feature com comparação antes/depois (Expo web e Android), mover antes de reescrever. |
| Risco       | Cache/invalidação: hooks novos podem invalidar prefixos diferentes dos atuais (`lists`, `reviews`, `tracking`, `notification-*`). | Mapear cada `invalidateQueries` atual antes do slice; conferir Home/listas após cada mutation. |
| Risco       | Remover `Redirect` expõe um frame com `session` nula (consultas disparando, erro de `session.user`). | Hooks com `enabled` por sessão e `viewerId` obtido do provider; roteiro de logout/401 em cada slice. |
| Risco       | Alias `@/assets` pode não resolver com o transformer SVG do Metro. | Validar com 1 arquivo e bundle (Expo web) antes dos 65; se falhar, parar e registrar como pergunta. |
| Risco       | `ConfirmDialog` muda o visual das 2 confirmações; unificar toggles/paginadores mudaria pixels (cópias com estilos diferentes). | Exceção explícita só para R9; demais por `variant`/props, com as diferenças como insumo da `0019`. Deep links: não renomear nem mover arquivos de rota. |

## 10. Resultado

- **Comando/verificação:** em `mobile/`: `npm run lint`; `npx tsc --noEmit`; comandos abaixo (Git Bash); roteiro manual de cada feature.

| Medida (comando, em `mobile/`) | Hoje | Meta |
| ------------------------------ | ---- | ---- |
| `grep -rnE "useQuery\(\|useInfiniteQuery\(\|useQueries\(\|useMutation\(" src --exclude-dir=hooks \| wc -l` | 80 | 0 |
| `grep -rn "useQueryClient" src/app src/components \| wc -l` | 20 | 0 |
| Maior arquivo de rota: `find src/app -name "*.tsx" -exec wc -l {} + \| sort -rn \| sed -n 2p` | 995 | ≤ 250 |
| `grep -rn "<Redirect" src/app \| wc -l` | 9 | 2 |
| `grep -rn "Alert.alert" src \| wc -l` | 2 | 0 |
| `grep -rn "import.*assets/" src \| grep -v "@/assets" \| wc -l` | 65 | 0 |
| duplicatas de primitivo fora de `components/ui` (regex da R7) | 9 | 0 |
| `ls src/hooks src/services/api-client.ts` | existem | não existem |
| `grep -rl "useForm" src/features \| cut -d/ -f3 \| sort -u \| wc -l` (features com RHF) | 0 (os 4 arquivos de auth estão em `components/auth`) | 6 |

- **Evidência esperada:** lint e tipos sem erro; tabela nos valores-alvo; `package.json` sem diff; ADR e docs atualizados.
- **Roteiro manual mínimo por feature** (Expo web 375 px e, quando possível, Android; registrar plataforma):

| Feature | Roteiro |
| ------- | ------- |
| ui/base | `PrimaryButton`, toggles, chips e pager parecem iguais em login, `profile`, `notifications`, `onboarding`, `lists`, `tracking`; 44 px preservados |
| catalog | buscar um título; abrir filme e série; temporadas e episódio; rota com provider inválido; API desligada: toast + "Tentar novamente" |
| tracking | histórico paginado; marcar e desmarcar filme/episódio (diálogo de confirmação); editar data assistida (mensagem inline inválida e válida) |
| lists | favoritar, adicionar à watchlist, criar/editar/excluir lista (nome vazio e limite inline), adicionar, ordenar e remover título, paginar |
| home | abas Filmes/Séries, seções, "Começar a assistir", calendário de estreias; erro isolado de uma seção; usuário sem onboarding vai a `/onboarding` |
| ratings | nota (meia estrela), comentário criar/editar/excluir (diálogo), spoiler e revelar, audiência, `my-ratings` |
| social | buscar usuário, enviar/aceitar/recusar solicitação, remover amigo, feed e perfil contextual (consentimentos) |
| notifications | inbox (marcar como lida), preferências, badge do header |
| gamification | dashboard, conquistas, estatísticas por mês, ranking, preferências |
| profile | editar nome/bio/avatar/privacidade (limites inline), salvar, créditos, `settings` |
| onboarding | fluxo completo, retomar etapa, editar depois de concluído |
| auth | login, cadastro, esqueci a senha, `reelix://auth/callback`, redefinir senha, logout, 401 forçado (API com audiência inválida) |

- **Comportamento observável:** aplicação idêntica à de hoje, exceto o `ConfirmDialog` das 2 confirmações.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| Premissa (Q2): sem testes mobile; proteção = `lint`, `tsc`, slices pequenos e roteiro manual (R12). | Risco de regressão | Não | Usuário |
| Premissa: os 2 `Redirect` de onboarding ficam (não são sessão); mover para guarda própria é decisão futura. | Escopo de R5 | Não | Tech lead |
| Premissa: buscas de campo único sem submit (`search`, busca do `social`) ficam fora do RHF; só formulários com validação/envio entram (R6). | Escopo de R6 | Não | Tech lead |
| Premissa: onde cópias de primitivos divergem em estilo, preservar por `variant` e deixar a unificação para a `0019`. | "Sem mudança visual" | Não | Tech lead |
| Premissa: `navigation/` (`AppHeader`, `UserDrawer`, `BackButton`) é compartilhado e fica em `components/navigation`, consumindo hooks das features. | R1 | Não | Tech lead |
| Premissa (Q3/Q6): sem tema claro/tablet; Expo web só para dev/inspeção. `ConfirmDialog` reaproveita `ModalDialog` e `PrimaryButton`. | Escopo, R9 | Não | Usuário |
| Número do ADR novo: próximo livre em `.adrs/` na implementação. | Numeração | Não | Tech lead |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação da spec em modo lote (breakdown arquitetura/design/testes); linha de base medida no código. | Features e telas finas depois da fundação de dados (`0017`). |
| 2026-11-24 | Implementação em 12 commits (um por fatia). Premissas registradas: `SocialAvatar` mantido em `features/social` com `getInitials` (aparência diferente do `UserAvatar`); `isCatalogProvider` exposto às rotas por `features/catalog/route-params`; `ToggleRow` e `Pager` com `variant` para preservar as aparências antigas; o plano ficou em `.plans/mobile-features-e-telas-finas-plan.md` e o ADR em `.adrs/XXXX-0018-...` (número provisório). | Execução orquestrada (sem dispositivo, sem roteiro manual). |
