# 0019 — Design tokens e primitivos de UI

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** pronta
- **Slug:** design-tokens-e-primitivos-ui
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-19
- **Owner:** Produto/Design (a confirmar)
- **Relacionados:** mapa `.plans/arquitetura-design-testes-breakdown.md` (decisões D6, D7, D8, D10, D11); depende da `0018` (telas finas, `features/*`) e, por ela, da `0017`; habilita a `0020` (refatoração visual) e a `0022` (testes de primitivos); `.docs/DESIGN.md`, `.docs/DESIGN_SYSTEM.md`, ADR 0002; skills `frontend-design` e `web-design-guidelines`.

## 1. Escopo

### Dentro do escopo

- **Tokens:** evoluir `mobile/src/theme/tokens.ts` (fonte única) e sincronizá-lo com `mobile/assets/brand/docs/design-tokens.json` por um mapa de nomes único e um script de verificação; cor de erro distinta; `lineHeight`, `letterSpacing`, `borderWidth`, elevação/sombra, `zIndex`, movimento (duração e curva), opacidades de estado, anel de foco e dimensões nomeadas; contraste WCAG AA verificado por script.
- **Primitivos** em `mobile/src/components/ui/`: `Text`, `Screen`, `Button` (absorve `PrimaryButton`), `Input` (absorve o `TextInput` cru; `AuthInput` passa a usá-lo), `Card`, `Chip`/`SegmentedControl`, `Switch`, `Badge`, `Skeleton`, `EmptyState`, `ErrorState`, `LoadingState`, `Pagination`, `Icon` e `ConfirmDialog` (sobre `ModalDialog`), com contrato de props, acessibilidade embutida e uso exclusivo de tokens.
- **Movimento:** hook único de "reduzir movimento" e migração do toast e do drawer para `react-native-reanimated`.
- **Limpeza** de assets e dependências sem uso, decidida por item (§7, passo 8).
- **Regras de lint** que impedem hex, `fontSize`, `lineHeight`, espaçamento, raio e dimensão literais fora de `theme`, e o uso cru de `Alert`, `TextInput`, `Switch` e `PrimaryButton`.
- **Adoção mínima** nas telas: só a troca mecânica necessária para os primitivos substituírem os componentes e usos crus existentes (§7, passo 7).
- Atualizar `.docs/DESIGN_SYSTEM.md` (componentes, tokens, lacunas) e as partes de `.docs/DESIGN.md` e do README do kit que ficarem desatualizadas.

### Fora do escopo

- Qualquer mudança de arquitetura de dados, `http-client`, hooks, `features/*` ou guardas de rota: `0017` e `0018` (esta spec parte do resultado delas).
- Refatoração visual das telas, navegação (Q4), hierarquia, copy, ilustração, skeleton em cada tela e virtualização das listas: `0020`.
- Testes de componente, Jest, Playwright e axe dos primitivos: `0022` e `0023` (esta spec só garante `testID` opcional nas props).
- Tema claro, tablet e Expo web como alvo (Q3 e Q6 do mapa, premissa P1); novo logo, nova família de fonte ou novos SVGs do kit.
- `ProgressBar`, `RatingStars` (controle), `SectionHeader` e `Divider`: entram na `0020` quando uma tela exigir, seguindo o contrato daqui.

## 2. Contexto

A auditoria de 2026-11-19 (`.docs/DESIGN.md` §13, `.docs/DESIGN_SYSTEM.md` §5.5) mostra um design system pela metade. Os tokens existem e a disciplina de cor é boa (2 hex fora de `tokens.ts`), mas faltam camadas inteiras: `error` e `brandSecondary` têm o mesmo hex (`#FF6688`); a borda de campo mede 1,38:1 sobre `background`; há 18 `lineHeight` literais, ~85 dimensões literais, 40 `borderWidth` e 17 `opacity` soltos; nada de sombra, `zIndex`, duração ou "reduzir movimento"; o botão tem `minHeight: 54` enquanto o token de alvo é 44. Não existe `Text`, `Screen`, `Card`, `Input` genérico, `Skeleton`, `EmptyState` nem `ConfirmDialog`: `TextInput` cru em 8 arquivos além do `AuthInput`, `Alert.alert` em 2 pontos (no-op no React Native Web, `DEVELOPMENT.md` §4), 50 `<ActivityIndicator>` e o bloco "Tentar novamente" reescrito em 22 arquivos. O `design-tokens.json` usa outros nomes (`primary`, `surfaceRaised`, `text`) e tem a cor `neutral` ausente do código.

A `0020` refaz as telas com intenção; isso só é barato e consistente se os blocos de construção existirem antes. Por isso esta spec entrega a fundação **sem** redesenhar telas, preservando a identidade da marca (D7: quase preto, `#FF2D55`, Sora).

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Quem usa o app (leitura de erro, foco, contraste, leitor de tela) e quem constrói telas (menos decisões soltas por tela) |
| Decisão ou ação que muda | Tela nova ou refeita compõe primitivos com tokens, em vez de copiar estilo; erro, destaque e borda passam a ser distinguíveis por cor; confirmação destrutiva e estados têm um padrão só |
| Resultado esperado       | Fundação visual única, verificável por script e lint, sobre a qual a `0020` e os testes (`0022`/`0023`) trabalham |
| Métrica de sucesso       | Literais de design fora de `theme` caem para zero (§10); todo par de cor exigido passa AA no script; `Alert.alert`, `TextInput`, `Switch` e `PrimaryButton` crus a zero |

## 4. Requisitos

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | `tokens.ts` é a fonte única; `design-tokens.json` é **gerado** dele (`npm run tokens:sync`) com os nomes de `tokens.ts` (`brand`, `brandSecondary`, `surface`, `surfaceElevated`, `textPrimary`, `textSecondary`, `neutral`, `borderStrong`, etc.) e `npm run tokens:check` falha se divergirem. `neutral` `#E5E7EB` passa a existir em `colors`. Mapa antigo→novo em `DESIGN_SYSTEM.md` §3.3 | Must | Given um valor mudado só no JSON When `npm run tokens:check` Then sai com código ≠ 0 e lista a chave; Given `tokens:sync` rodado When `tokens:check` Then sai com 0 |
| R2  | `colors.error` ganha hex próprio, **distinto** de `brandSecondary` e de `brand` (diferença de matiz ≥ 20° e contraste ≥ 4,5:1 sobre `background`, `surface` e `surfaceElevated`); o toast de erro e o rótulo `quiet`/`destructive` usam `error`; `brandSecondary` fica só para foco, links e destaque | Must | Given `tokens.ts` When `tokens:check` Then `error` ≠ `brandSecondary`, matiz ≥ 20° e AA nas três superfícies; `grep -n "error: '#FF6688'" mobile/src/theme/tokens.ts` retorna 0 |
| R3  | Tokens novos: `lineHeights` (por papel de `typography`), `letterSpacing`, `borderWidth` (fina, grossa), `opacity` (pressed, disabled), `elevation` (sombra + `elevation` Android, 2 níveis) com `colors.shadow`, `zIndex` (base, raised, overlay, toast), `motion` (`duration` fast/base/entrance e `easing`), `focusRing` (cor e largura), `colors.onBrand`, `colors.overlayScrim`, `colors.borderStrong` e `colors.notificationLight` | Must | Given `tokens.ts` When `npx tsc --noEmit` Then passa; cada grupo existe exportado e tipado `as const`; a `lightColor` de `push-registration.ts` e o `shadowColor` do toast usam token |
| R4  | `dimensions` ganha nomes para as medidas repetidas ou estruturais (altura de controle `controlHeight` 54 e `compactControlHeight` 44 separados do alvo mínimo, pôster de linha, medalha, logo, largura máxima de formulário e de diálogo, escala de ícones `small`/`medium`/`large`); as ~85 dimensões literais e os 2 `width={n}`/`height={n}` de JSX migram para tokens | Must | Given o código When o grep de dimensões literais (§10) Then retorna 0 fora de `src/theme` |
| R5  | Contraste por script: `npm run tokens:check` calcula WCAG 2.x para uma lista de pares declarada no próprio script e falha abaixo da meta. Texto ≥ 4,5:1 (`textPrimary`/`textSecondary` nas 3 superfícies, `onBrand` sobre `brand`, `error`, `success`, `warning`, `info` sobre `background`/`surface`, `brandSecondary` sobre `brandSurface`); componentes ≥ 3:1 (`borderStrong` e `focusRing` sobre `surface` e `background`; `brand` sobre `background`) | Must | Given o repositório When `cd mobile && npm run tokens:check` Then imprime a tabela par → razão e sai com 0; Given `borderStrong` trocado por um cinza de 1,4:1 When o script roda Then falha apontando o par |
| R6  | `Text`: variantes `display`, `title`, `section`, `body`, `bodySmall`, `caption`, `label`; `tone` (`primary`, `secondary`, `brand`, `error`, `success`, `onBrand`); `weight` opcional entre as famílias; `numeric` liga números tabulares; usa `typography`, `lineHeights` e `fontFamily`; `maxFontSizeMultiplier` padrão definido (P3); linhas longas truncam com `numberOfLines` e `minWidth: 0` no contêiner | Must | Given `<Text variant="title">` When renderizado Then `fontSize`, `lineHeight` e `fontFamily` vêm de tokens (sem literais no arquivo); `accessibilityRole="header"` só quando `header` é passado |
| R7  | `Screen`: `SafeAreaView` (`edges` configurável, padrão todas), gutter `spacing.medium`, `scroll` (`none`/`scroll`), `keyboardShouldPersistTaps="handled"`, `contentMaxWidth` opcional, `refreshControl`, fundo `background`; listas longas usam `FlatList` como filho com `scroll="none"` | Must | Given `Screen` em tela com notch When renderizada Then o conteúdo não fica sob barra de status nem indicador de gesto; `settings` e `AuthScreen` passam a usá-lo |
| R8  | `Button` absorve `PrimaryButton`: `variant` `primary`/`secondary`/`quiet`/`destructive`, `size` `md` (altura `controlHeight`) e `sm` (`compactControlHeight`), `loading` (rótulo mantido para leitor de tela, `busy`), `disabled`, `icon` opcional, `fullWidth`, `style`, `testID`; estados pressed/disabled por `opacity` token; foco visível com `focusRing`; rótulo `onBrand` sobre `brand` | Must | Given `Button loading` When tocado Then `onPress` não dispara e `accessibilityState.busy` é `true`; `grep -rn "primary-button" mobile/src` retorna 0 |
| R9  | `Input` controlado e genérico (sem `react-hook-form`): `label` visível, `value`, `onChangeText`, `error` (texto com `accessibilityRole="alert"`, junto ao campo), `hint`, `multiline`, `maxLength` com contador, `inputMode`/`keyboardType`, `autoComplete`, `textContentType`, `returnKeyType`, `onSubmitEditing`, `secureTextEntry` com alternador Mostrar/Ocultar; borda `borderStrong`, foco com `focusRing`; **nunca** bloqueia colar nem usa `autoFocus` por padrão; `AuthInput` vira adaptador `Controller` sobre `Input` e deixa de fixar `textContentType`/`autoCapitalize` | Must | Given `Input error="Informe um e-mail válido."` When renderizado Then o texto aparece abaixo do campo, a borda usa `error` e a mensagem é anunciada; `grep -rn "<TextInput" mobile/src --exclude-dir=ui` retorna 0 |
| R10 | `Card` (superfícies `surface`/`surfaceElevated`, raio e padding por `variant`/`padding`, `pressable` opcional com role e foco), `Badge` (contagem com `99+`, `tone`, rótulo acessível via prop) e `Switch` (role `switch`, `accessibilityState.checked`, cores por token, linha com rótulo de 44 px) | Must | Given `Switch value` When ativado por leitor de tela Then anuncia o rótulo e o estado; `grep -rn "<Switch" mobile/src --exclude-dir=ui` retorna 0 |
| R11 | `Chip`/`SegmentedControl` (`tablist`/`tab` ou `radiogroup`, `selected`, alvo 44, rolagem horizontal quando exceder a largura) e `Pagination` (anterior/próxima com `Icon`, "página X de Y" com números tabulares, desabilita nas pontas, rótulos pt-BR) substituem os `Pressable role="tab"` e as setas `←` de `my-ratings` e `tracking` | Must | Given 2 abas e a segunda tocada When ativadas Then só a segunda tem `selected: true`; `grep -rn "accessibilityRole=\"tab\"" mobile/src --exclude-dir=ui` cai de 10 para 0 |
| R12 | `Skeleton` (bloco com pulso em opacidade, desligado com reduzir movimento), `LoadingState` (`inline`/`fullscreen`, mensagem, `accessibilityLiveRegion="polite"`), `EmptyState` (ilustração opcional `assets/brand/ui/empty-state.svg`, título, texto de convite, ação `Button`) e `ErrorState` (mensagem com a correção e ação "Tentar novamente"; o erro de API continua indo ao toast, não inline) | Must | Given um `ErrorState` When renderizado Then expõe `accessibilityRole="alert"` e um `Button` "Tentar novamente"; `EmptyState` aparece em `home` e `tracking` no lugar do texto solto |
| R13 | `Icon` (nome → SVG do kit, `size` por token, `tone`, decorativo oculto do leitor de tela por padrão e com `accessibilityLabel` quando informativo, `rotate`); `BackButton` troca `←` por `arrow-down` girado e o toast troca `×` por `close.svg`; nenhum SVG novo no kit | Must | Given o código When `grep -rnE ">←<\|>×<\|'←'" mobile/src` Then retorna 0 (glifos de avaliação `★◐☆` ficam para a `0020`) |
| R14 | `ConfirmDialog` sobre `ModalDialog` (título, descrição, `confirmLabel`/`cancelLabel` específicos, `destructive`, `busy`, foco no cancelar, `overlayScrim` translúcido) substitui os dois `Alert.alert` (`tracking`, `rating-review-section`) | Must | Given exclusão de avaliação When o usuário toca em Excluir Then o `ConfirmDialog` abre (também no Expo web) e só após "Excluir avaliação" a ação roda; `grep -rn "Alert\.alert" mobile/src` retorna 0 |
| R15 | Movimento: hook `useReducedMotion` único (sobre `react-native-reanimated`); toast e drawer migram de `Animated` com `useNativeDriver: false` para `reanimated` com `motion.duration`; com reduzir movimento ligado, entrada e saída são instantâneas | Must | Given reduzir movimento ligado When o toast aparece Then não anima; `grep -rn -e useNativeDriver -e "Animated\." mobile/src` retorna 0 |
| R16 | Lint: no `eslint.config.js`, `no-restricted-syntax` e `no-restricted-imports` falham em hex, `fontSize`/`lineHeight`/`letterSpacing`/`borderRadius`/`padding*`/`margin*`/`gap`/`width`/`height`/`min*`/`max*` numéricos (exceto 0 e 1) e importação de `Alert`, `TextInput`, `Switch` de `react-native` fora de `src/components/ui` e `src/theme` | Must | Given `padding: 12` colado numa tela When `npm run lint` Then falha apontando arquivo e linha; Given o repositório pronto When `npm run lint` Then passa |
| R17 | Todo primitivo: tokens apenas; `style` e `testID` aceitos; roles, labels e `accessibilityState` embutidos; alvo ≥ 44; pt-BR; sem chamada de API; foco visível e sem `autoFocus` | Must | Given cada arquivo de `components/ui` When `grep` de hex/literais (§10) Then 0; checklist de contrato do `DESIGN_SYSTEM.md` §4 marcada por componente no PR |
| R18 | Limpeza decidida por item (§7, passo 8): remover 7 PNGs do template Expo, a pasta `assets/expo.icon/` e as dependências `expo-symbols`, `@expo/ui`, `expo-glass-effect`; manter por justificativa os demais | Must | Given a limpeza When `cd mobile && npm run lint && npx tsc --noEmit && npx expo-doctor` Then passam; `ls mobile/assets/images` não lista PNG do template |
| R19 | Docs: `.docs/DESIGN_SYSTEM.md` (§1 estado, §3 tokens e mapa, §5 componentes, §5.5 e §8 lacunas) e as divergências de `.docs/DESIGN.md` §2, §4 e §13, o README do kit e o link para `tokens:check`/`tokens:sync` atualizados na mesma mudança | Must | Given o PR When `grep -n "components/ui" .docs/DESIGN_SYSTEM.md` Then lista os 14+ primitivos; §13.3 de `DESIGN.md` marca os itens resolvidos |

## 5. Fluxos e estados

- **Caminho feliz:** quem constrói uma tela importa `Screen`, `Text`, `Button`, `Input` e `Card` de `@/components/ui`, usa `colors`/`spacing`/`dimensions` e nenhuma cor, tamanho ou duração literal; `npm run lint` e `npm run tokens:check` passam.
- **Vazio / carregando / erro:** são primitivos (`EmptyState`, `LoadingState`/`Skeleton`, `ErrorState`); erro de API continua no toast e o `ErrorState` só oferece a recuperação.
- **Sucesso parcial:** `Pagination` e `Chip` mantêm o estado selecionado por texto/ícone além da cor (`accessibilityState`).
- **Cancelamento/retry:** `ConfirmDialog` cancela por botão, toque fora ou voltar do Android (bloqueado em `busy`); `Button loading` impede duplo envio.
- **Reduzir movimento:** `Skeleton`, toast, drawer e `ConfirmDialog` ficam estáticos.

## 6. Acceptance Criteria

- [ ] Given `cd mobile && npm run lint` When rodado Then passa e as regras de R16 estão ativas.
- [ ] Given `cd mobile && npx tsc --noEmit` When rodado Then passa.
- [ ] Given `npm run tokens:check` When rodado Then valida sincronia com o JSON e os pares de contraste de R5 com saída 0.
- [ ] Given o grep de literais de design (§10) em `mobile/src` fora de `theme` When rodado Then todas as contagens são 0.
- [ ] Given `grep -rn "Alert\.alert\|primary-button\|useNativeDriver" mobile/src` Then 0 resultados.
- [ ] Given `Button`, `Input`, `Switch`, `SegmentedControl`, `Pagination`, `ConfirmDialog`, `Badge`, `EmptyState` e `ErrorState` When cada um tem ao menos um consumidor real (§7, passo 7) Then nenhum está órfão.
- [ ] Given `Expo web` em 375 px e o app em um dispositivo When abertas Entrada, Home, Perfil e Configurações Then não há regressão de layout nem texto cortado, e o foco do campo e do botão é visível.
- [ ] Given `.docs/DESIGN_SYSTEM.md` When lido Then descreve os primitivos reais e não lista mais como ausentes os itens entregues.

## 7. O que fazer

1. Confirmar o pós-`0018` (caminhos de `features/*`) e medir de novo a linha de base de §10 com os mesmos greps.
2. Evoluir `tokens.ts` (R1–R4) e criar `scripts/check-tokens.mjs` com `tokens:sync` e `tokens:check` em `package.json`; fixar o hex de `error` e de `borderStrong` pelo script (P2, P3).
3. Criar `components/ui/` com `Text`, `Icon` e `Button` primeiro (os demais dependem deles), depois `Input`, `Card`, `Badge`, `Switch`, `Chip`/`SegmentedControl`, `Pagination`, `Skeleton`, `LoadingState`, `EmptyState`, `ErrorState`, `Screen` e `ConfirmDialog`; `index.ts` reexporta.
4. Criar o hook de reduzir movimento e migrar toast e drawer.
5. Migrar `AuthInput` para `Input` e `PrimaryButton` para `Button` (renomear imports nos 21 arquivos; remover `components/auth/primary-button.tsx`).
6. Ativar as regras de lint (R16) por último, depois de zerar os literais.
7. Adoção mínima: `Input` nos 8 arquivos com `TextInput` cru; `Switch` em `gamification`, `notifications` e `profile`; `ConfirmDialog` nos 2 `Alert.alert`; `Badge` no `AppHeader`; `SegmentedControl`/`Chip` nas 7 telas com abas locais; `Pagination` em `my-ratings` e `tracking`; `Card` e `Screen` em `AuthScreen` e `settings`; `EmptyState`/`ErrorState`/`LoadingState` em `home` e `tracking`; `Icon` no `BackButton` e no toast; `Text` nos arquivos tocados. Sem mudar hierarquia, copy ou layout além do necessário.
8. Limpeza: remover `assets/images/*.png` (7 arquivos sem referência em `src`, `app.json`, `plugins`, `scripts`, `eas.json`, `metro.config.js`) e `assets/expo.icon/`; remover `expo-symbols`, `@expo/ui` e `expo-glass-effect` com `npm uninstall` (0 imports, fora dos plugins); **manter** `react-native-reanimated` e `react-native-worklets` (agora usados), `react-native-gesture-handler` (peer do roteador), `expo-system-ui` (exigido por `userInterfaceStyle`), `expo-status-bar` e `expo-linear-gradient`; **manter** os 28 SVGs de navegação (os 6 sem uso hoje, `calendar`, `filter`, `fire`, `pause`, `play`, `share`, ficam como reserva do kit para a `0020` e só entram no registro do `Icon` quando usados), os 9 `genre-icons` e `ui/splash-screen.svg` (fonte editável gerada por `scripts/generate-brand-assets.mjs`, sem uso: documentar como "reserva" no README do kit); `ui/empty-state.svg` passa a ser usado pelo `EmptyState`.
9. Atualizar `.docs/DESIGN_SYSTEM.md`, `.docs/DESIGN.md` e o README do kit; rodar os gates e o roteiro de §6.

## 8. O que não fazer

- Não redesenhar telas, mudar navegação, copy ou hierarquia (é a `0020`); não mexer em `services/`, hooks ou `features/*` além de trocar imports.
- Não criar cor, tamanho, raio, sombra ou duração fora de `tokens.ts`; não importar `design-tokens.json` no código; não editar o JSON à mão depois do `tokens:sync`.
- Não usar `fontWeight`, `transition: all`, `autoFocus` sem justificativa, `contextMenuHidden` nem bloquear colar.
- Não trocar emoji ou glifo por biblioteca de ícones externa; não criar SVG novo no kit (premissa P5).
- Não apagar SVG do kit nem dependência de peer sem a justificativa de §7.8; não editar `package-lock.json` à mão.
- Não adicionar `Alert.alert` (no-op no web); confirmação destrutiva é `ConfirmDialog`.

## 9. Riscos e dependências

| Tipo        | Descrição | Mitigação |
| ----------- | --------- | --------- |
| Dependência | `0018` altera arquivos e pastas que esta spec migra (21 imports de `PrimaryButton`, 8 `TextInput`) | Só iniciar depois da `0018` mergeada; refazer a linha de base (§7.1) |
| Risco       | Regra de lint com falsos positivos (valores 0/1, `'100%'`, `StyleSheet.absoluteFill`) | Exceções explícitas na regra; teste manual com trecho que viola e outro que não |
| Risco       | Inflação de tokens de dimensão (~85 literais) | Reaproveitar uma escala curta; valor único e estrutural vira nome semântico em `dimensions`; revisão no PR |
| Risco       | Adoção parcial deixa duas formas coexistindo até a `0020` | Lint bloqueia as formas antigas; os primitivos só substituem equivalentes diretos |
| Risco       | Migração para `reanimated` quebra o drawer ou o toast no dispositivo | Validar em Expo web e no APK de development (já inclui o módulo nativo); manter duração e curva atuais |
| Risco       | Remover `expo.icon/` ou dependências afeta o build nativo | `npx expo-doctor`, `npx expo config` e `npx expo export --platform web` locais antes do PR |
| Risco       | Novo `error` destoa da marca ou do `warning` âmbar | Candidato inicial aprovado por script (P2); revisão de Design no PR com captura do toast |

## 10. Resultado

Como validar que a spec foi atendida (comandos em `mobile/`; baseline medida em 2026-11-19, antes da `0018`). Nas células, `\|` é só o escape de tabela Markdown: ao copiar o comando, use `|`.

| Medida | Comando | Hoje | Meta |
| ------ | ------- | ---- | ---- |
| Hex fora de `theme` | `grep -rnE "#[0-9A-Fa-f]{3,8}\b" src --include=*.ts --include=*.tsx --exclude-dir=theme \| wc -l` | 2 | 0 |
| `fontSize` literal | `grep -rnE "fontSize: [0-9]" src --exclude-dir=theme \| wc -l` | 1 | 0 |
| `lineHeight` literal | `grep -rnE "lineHeight: [0-9]" src --exclude-dir=theme \| wc -l` | 18 | 0 |
| `letterSpacing` literal | `grep -rnE "letterSpacing: -?[0-9]" src --exclude-dir=theme \| wc -l` | 5 | 0 |
| padding/margin numéricos | `grep -rnE "(padding\|margin)[A-Za-z]*: -?[0-9]" src --exclude-dir=theme \| wc -l` | 14 | 0 |
| `borderRadius` literal | `grep -rnE "[bB]orderRadius: [0-9]" src --exclude-dir=theme \| wc -l` | 2 | 0 |
| dimensões literais ≥ 2 | `grep -rnE "\b(width\|height\|minWidth\|minHeight\|maxWidth\|maxHeight): -?([2-9]\|[1-9][0-9]+)\b" src --exclude-dir=theme \| wc -l` | 85 (+2 em JSX) | 0 |
| `borderWidth` / `opacity` / `zIndex` / `elevation` literais | grep `borderWidth: [0-9]`, `opacity: [0-9.]`, `zIndex: [0-9]`, `elevation\|shadow[A-Za-z]*:` fora de `theme` | 40 / 17 / 9 / 5 | 0 / 0 / 0 / 0 |
| Durações literais | `grep -rnE "duration: [0-9]+\|transition=\{[0-9]+\}" src \| wc -l` | 5 (3 `duration`, 2 `transition`; `useNativeDriver: false` em 3) | 0 |
| `Alert.alert` | `grep -rn "Alert\.alert" src \| wc -l` | 2 | 0 |
| `<TextInput` fora de `ui` | `grep -rn "<TextInput" src --exclude-dir=ui \| wc -l` | 15 (9 arquivos, com `AuthInput`) | 0 |
| `<Switch` fora de `ui` | idem | 3 | 0 |
| `PrimaryButton` | `grep -rln "primary-button" src \| wc -l` | 21 arquivos | 0 |
| Glifos `←` `×` | `grep -rnE ">←<\|>×<\|'←'" src \| wc -l` | 4 | 0 |
| `role="tab"` local | `grep -rn "accessibilityRole=\"tab\"" src --exclude-dir=ui \| wc -l` | 10 | 0 |
| Arquivos em `components/ui` | `ls src/components/ui \| wc -l` | 0 | ≥ 15 |
| Dependências sem uso | `expo-symbols`, `@expo/ui`, `expo-glass-effect` em `package.json` | 3 | 0 |
| PNGs do template e `expo.icon` | `ls assets/images assets/expo.icon` | 7 PNG + pasta | ausentes |
| Contraste e sincronia | `npm run tokens:check` | script inexistente | saída 0 |

- **Gates:** `npm run lint`, `npx tsc --noEmit`, `npm run tokens:check`, `npx expo-doctor`, `npm audit --audit-level=high`; backend não muda.
- **Evidência esperada:** saída do `tokens:check` (tabela de razões), contagens acima no PR, capturas em 375 px de Entrada, Home, Perfil e Configurações, e o `DESIGN_SYSTEM.md` atualizado.
- **Comportamento observável:** erro e destaque têm cores diferentes; campo, botão e controle mostram foco; excluir pede confirmação também no web; com reduzir movimento ligado, toast e drawer não animam.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| P1 (premissa, Q3/Q6 do mapa): só tema escuro, retrato; Expo web é ferramenta de teste, não alvo | Sem tokens por tema nem breakpoints | Não | Usuário |
| P2: hex de `error` recomendado `#FF7A59` (matiz 12°, 23° de `brand`; 7,68/6,76/5,57:1 sobre `background`/`surface`/`surfaceElevated`, medido hoje); o script é o árbitro e Design aprova na revisão | Aparência do erro e do `destructive` | Não | Produto/Design |
| P3: `borderStrong` recomendado `#707078` (3,53:1 sobre `surface`, 4,02:1 sobre `background`); `maxFontSizeMultiplier` padrão 1,5 em controles e títulos, corpo livre; validar com fonte grande do sistema | Legibilidade de campos e escala de fonte | Não | Produto/Design |
| P4: manter `react-native-reanimated`/`worklets` e usá-los (toast, drawer, `Skeleton`); remover só as 3 dependências sem uso | Peso do bundle e animação | Não | Tech lead |
| P5: sem SVG novo; seta de voltar = `arrow-down` girado; `RatingStars`, `ProgressBar` e `SectionHeader` ficam para a `0020` | Escopo do kit | Não | Produto/Design |
| P6: JSON do kit adota os nomes de `tokens.ts` (ninguém no código consome o JSON; só README, wireframe e docs o citam) | Quebra de quem lê o JSON fora do repo | Não | Tech lead |
| P7: Q4 (navegação) é da `0020`; esta spec não presume abas nem drawer (o `UserDrawer` só recebe a migração de animação) | Reuso do drawer | Não | Usuário |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação em modo lote (`breakdown-specs`), com linha de base medida por grep e contraste calculado | Fundação visual da fase Design (mapa `.plans/arquitetura-design-testes-breakdown.md`) |
