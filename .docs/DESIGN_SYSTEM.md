# Design System

> **Público:** quem cria ou altera componentes de UI do app mobile.
> **Obrigatório** em conjunto com `DESIGN.md`.
> **Estado:** baseline auditado em 2026-11-19. Existem tokens em `mobile/src/theme/tokens.ts`, Sora carregada no root e uma dúzia de componentes compartilhados; **faltam** primitivos básicos (`Text`, `Screen`, `Card`, `Input`, estados vazio/erro/carregando). A consolidação virá numa spec futura em `.specs/` (a pasta ainda não existe).

## 1. Decisão

O design system do Reelix é **React Native `StyleSheet` + tokens em TypeScript**, dentro do app Expo (`mobile/`). `shadcn/ui` e `TailwindCSS` **não se aplicam** ao mobile: shadcn é para React web e o projeto não usa NativeWind. Só reconsidere shadcn/Tailwind se um dia existir um painel web separado.

| Item | Estado |
| --- | --- |
| Estilo | `StyleSheet.create` em 40 arquivos de `mobile/src`; sem NativeWind, styled-components nem biblioteca de UI |
| Tokens | `mobile/src/theme/tokens.ts`: `colors`, `spacing`, `typography`, `radii`, `dimensions`, `fontFamily` (objetos `as const`); espelham parte de `mobile/assets/brand/docs/design-tokens.json` (§3.3) |
| Fontes | Sora em 5 pesos (`@expo-google-fonts/sora`), carregada em `src/app/_layout.tsx` com `useFonts`; sem fallback |
| Tema | Só escuro (`userInterfaceStyle: "dark"`); sem tokens por tema |
| Ícones e ilustrações | SVGs do kit `mobile/assets/brand/`, importados como componentes (`react-native-svg` + `react-native-svg-transformer`, `metro.config.js`) |
| Imagens | `expo-image` (`Image` com `contentFit="cover"`, `transition={150}`) |
| Componentes compartilhados | `PrimaryButton`, `AuthInput`, `AuthLink`, `AuthScreen`, `FormMessage`, `IconAction`, `ModalDialog`, `UserAvatar`, `AppHeader`, `BackButton`, `UserDrawer`, `TitlePosterCard`, `MediaTitleRow`, `AchievementMedal`, `AchievementGrid`, `ToastProvider` (§5) |
| Componentes ausentes | Ver §5.5 |
| Animação | `Animated` do RN (`useNativeDriver: false`); `react-native-reanimated` instalado sem uso |
| Compilador | `experiments.reactCompiler: true` e `typedRoutes: true` em `app.json` |
| Testes de UI | Nenhum arquivo de teste em `mobile/`; nenhum `testID` |
| Alias de import | `@/` aponta para `mobile/src/` (`tsconfig.json`) |

Ao instalar ou mudar algo aqui, atualize esta tabela e `DESIGN.md` na mesma mudança.

## 2. Onde cada componente vive

| Tipo | Local hoje | Exemplo |
| --- | --- | --- |
| Tokens | `mobile/src/theme/tokens.ts` | `colors`, `spacing` |
| Componente compartilhado solto | `mobile/src/components/<nome>.tsx` | `icon-action.tsx`, `modal-dialog.tsx`, `user-avatar.tsx` |
| Componente por domínio | `mobile/src/components/<dominio>/` | `auth/`, `discovery/`, `gamification/`, `navigation/`, `reviews/`, `social/` |
| Provider com UI | `mobile/src/providers/` | `toast-provider.tsx` |
| Tela (rota) | `mobile/src/app/` | `home.tsx`, `tracking.tsx` |
| Dados e API | `mobile/src/services/`, `mobile/src/hooks/` | `api-client.ts`, `use-tracking.ts` |
| Arte do kit de marca | `mobile/assets/brand/` | `navigation-icons/`, `badges/` |

Regras:

- Não ponha componente em `src/app/`: toda pasta e arquivo ali vira rota (Expo Router).
- Se o componente é usado em mais de uma tela ou feature e não depende de regra de negócio nem de chamada de API, ele é um **primitivo**.
- **Onde criar primitivos novos (convenção proposta, ainda não adotada):** `mobile/src/components/ui/<nome>.tsx`. A pasta não existe hoje; os primitivos de fato existentes estão espalhados (`components/auth/primary-button.tsx`, `components/icon-action.tsx`, `components/modal-dialog.tsx`). Mover o `PrimaryButton` para fora de `auth/` faz parte da spec futura; não faça isso numa mudança de tela.
- Composição de feature (que chama API, usa React Query ou conhece o domínio) fica em `components/<dominio>/`, nunca em `components/ui/`.
- Arquivo em `kebab-case.tsx`; export nomeado em `PascalCase` (não há `export default` nos componentes atuais; só as telas usam default, por exigência do Expo Router).

## 3. Tokens

### 3.1 Estrutura

Cinco grupos de design mais dimensões e fonte, todos em `tokens.ts`. Não há camada semântica separada da primitiva: os nomes já são semânticos (`brand`, `surface`, `textPrimary`), e cada token tem um único valor (sem variantes por tema).

| Grupo | Chaves |
| --- | --- |
| `colors` | `brand` `#FF2D55`, `brandSecondary` `#FF6688`, `brandSurface` `#35131D`, `background` `#080B0F`, `backgroundTransparent` `#080B0F00`, `surface` `#1A1A1E`, `surfaceElevated` `#2A2A2E`, `border` `#2A2A2E`, `textPrimary` `#FFFFFF`, `textSecondary` `#B8B8BD`, `error` `#FF6688`, `success` `#9ADDAA`, `warning` `#FBBF24`, `info` `#38BDF8`, `white` `#FFFFFF` |
| `spacing` | `extraSmall` 4, `small` 8, `medium` 16, `large` 24, `extraLarge` 32 |
| `typography` | `display` 32, `title` 28, `section` 18, `body` 15, `bodySmall` 14, `caption` 12, `eyebrow` 11 |
| `radii` | `extraSmall` 4, `small` 8, `medium` 14, `large` 24, `pill` 999 |
| `dimensions` | `posterWidth` 120, `posterHeight` 180, `minimumTouchTarget` 44, `drawerWidth` 320, `avatarSize` 56, `logoWidth` 120, `logoHeight` 36, `iconSize` 24 |
| `fontFamily` | `regular` `Sora_400Regular`, `medium` `Sora_500Medium`, `semiBold` `Sora_600SemiBold`, `bold` `Sora_700Bold`, `extraBold` `Sora_800ExtraBold` |

Papel de cada cor, contraste calculado e tamanhos de texto estão em `DESIGN.md` §2, §3 e §4.

**Ausentes**: token de `lineHeight`, `letterSpacing`, sombra, elevação, duração de animação, `zIndex`, tamanho de ícone além de `iconSize`, e cores semânticas por tema.

### 3.2 Regras

1. Hex só aparece em `tokens.ts`. Exceções existentes (a corrigir): `shadowColor: '#000000'` em `toast-provider.tsx:135` e `lightColor: '#27D17F'` em `push-registration.ts:33`.
2. `fontSize` vem de `typography` e `fontFamily` de `fontFamily`; nunca `fontWeight` (não há ocorrência no código; o peso é a família). Exceção existente: `fontSize: 24` no `×` do toast.
3. Espaçamento (`padding`, `margin`, `gap`) vem de `spacing`. Os 14 valores numéricos de `onboarding.tsx`, `profile.tsx`, `_layout.tsx` e `auth-link.tsx` são dívida, não precedente.
4. Raio vem de `radii` (dívida: `my-ratings.tsx:110` e `notifications.tsx:352`).
5. Alvo de toque mínimo é `dimensions.minimumTouchTarget`. Tamanho de pôster, avatar, ícone e logo vêm de `dimensions`. `width`/`height` numéricos soltos (cerca de 90) são dívida; se o valor se repetir, vira token em `dimensions`.
6. `lineHeight`: calcule a partir de `typography` (`typography.body * 1.6`) ou use um token quando existir. Não adicione literais novos (18 hoje).
7. Cor nova: entra em `tokens.ts` e, se vier do kit de marca, em `design-tokens.json` na mesma mudança.
8. Texto sobre `brand` usa `background`, não branco (contraste 5,41:1; branco dá 3,65:1).
9. `error` e `brandSecondary` têm o mesmo hex: não use a cor como único sinal de erro.
10. Token sem uso (`info`) pode ficar, mas não é exemplo de uso.

### 3.3 Divergências entre `tokens.ts` e `design-tokens.json`

| Tema | `design-tokens.json` | `tokens.ts` |
| --- | --- | --- |
| Nomes de cor | `primary`, `secondary`, `surfaceRaised`, `text`, `textMuted` | `brand`, `brandSecondary`, `surfaceElevated`, `textPrimary`, `textSecondary` (mesmos valores) |
| `neutral` `#E5E7EB` | Presente | **Ausente** |
| Cores só no código | Ausentes | `brandSurface`, `backgroundTransparent`, `border`, `error`, `success`, `warning`, `info`, `white` |
| Raios | `small` 8, `medium` 14, `large` 24, `pill` 999 | Também `extraSmall` 4 |
| Espaçamento | `xs`/`sm`/`md`/`lg`/`xl` = 4/8/16/24/32 | `extraSmall`…`extraLarge`, mesmos valores |
| Tipografia | Família `Sora`, fallback `Arial, sans-serif`, pesos de título 700 e corpo 400; **sem tamanhos** | Cinco famílias por peso (400 a 800) e sete tamanhos; **sem fallback** |
| `dimensions` | Ausente | 8 chaves |
| `docs/palette.svg` | Paleta visual do kit | Não conferida contra `tokens.ts` neste baseline |

O README do kit diz que o app carrega `Sora_700Bold` e `Sora_600SemiBold`; o `_layout.tsx` carrega cinco pesos. Em conflito entre as fontes, vale `tokens.ts` para o que o app renderiza. A sincronização das duas fontes de tokens é pendência (`DESIGN.md` §13.3).

## 4. Contrato de componentes

Todo primitivo novo deve:

- Receber **tokens**, não valores soltos, e expor variantes explícitas (`variant`, `size`) em vez de flags booleanas.
- Definir os estados aplicáveis: padrão, pressionado, foco, desabilitado, carregando, erro, selecionado.
- Ser acessível: `accessibilityRole`, `accessibilityLabel` quando o rótulo não for o texto visível, `accessibilityState` para `disabled`, `busy`, `selected`, `checked` e `expanded`; alvo de toque de 44px.
- Aceitar `style` quando fizer sentido compor (os componentes atuais **não** aceitam).
- Não conter chamada de API, regra de negócio nem texto de produto fixo; o texto vem por props. Componentes de domínio podem.
- Mostrar erro de API pelo toast (§5.3), nunca inline.
- Ter texto e `accessibilityLabel` em pt-BR.
- Não depender de `react-hook-form` se for genérico (o `AuthInput` depende; ver §5.1).
- Não precisar de `useMemo`/`useCallback` por padrão: o React Compiler está ligado (`app.json`).

Convenções: props tipadas em `type Props` ou `type <Nome>Props`; sem `any`; `children` para conteúdo composicional.

## 5. Componentes atuais

Contagens de uso: quantidade de arquivos que importam o componente (grep de `import`, excluindo o próprio arquivo).

### 5.1 Compartilhados

| Componente | Arquivo | Props | Comportamento e acessibilidade |
| --- | --- | --- | --- |
| `PrimaryButton` | `components/auth/primary-button.tsx` (21 usos em todo o app, embora viva em `auth/`) | `label`, `onPress`, `loading?`, `disabled?`, `variant?` (`'primary'` padrão, `'secondary'`, `'quiet'`) | `Pressable` com `accessibilityRole="button"`, `accessibilityLabel={label}` e `busy`/`disabled`; `minHeight: 54`, `radii.medium`. `primary`: fundo `brand`, texto `background`. `secondary`: `surfaceElevated` com borda `border`, texto `textPrimary`. `quiet`: transparente, texto `error`. `loading` troca o texto por `ActivityIndicator` e desabilita; pressionado `opacity` 0,82, desabilitado 0,55. Sem ícone, `size` nem `style` |
| `AuthInput<TValues>` | `components/auth/auth-input.tsx` (4 usos, só autenticação) | `control`, `name` (`react-hook-form`), `label`, `placeholder?`, `autoComplete?`, `keyboardType?`, `secureTextEntry?` (padrão `false`), `returnKeyType?` (padrão `'next'`) | Rótulo `bodySmall` acima; moldura de 54px (`surface`, borda `border`); borda `brandSecondary` no foco e `error` com erro; erro em `caption` com `accessibilityRole="alert"` e repetido em `accessibilityHint`. Com `secureTextEntry`, botão Mostrar/Ocultar (`minWidth: 76`, alto 44, label `Mostrar senha`/`Ocultar senha`, `selected`). Fixa `autoCapitalize="none"`, `autoCorrect={false}` e `textContentType` (`password`, `newPassword` ou `emailAddress`), então só serve a e-mail e senha |
| `AuthLink` | `components/auth/auth-link.tsx` (4 usos) | `href` (`'/'`, `'/sign-up'` ou `'/forgot-password'`), `label`, `align?` (`'center'` padrão, `'left'`, `'right'`) | `Link` do `expo-router` com `accessibilityRole="link"`, `minHeight: 44`, texto `brandSecondary`. `href` é uma união fechada de três rotas |
| `AuthScreen` | `components/auth/auth-screen.tsx` (6 usos) | `title`, `subtitle`, `children` | `ScrollView` centralizado, `maxWidth: 440`, logo horizontal (`accessibilityLabel` "Reelix — filmes e séries"), linha de marca de 40×3, título (`accessibilityRole="header"`, `display`, `extraBold`), cartão de formulário (`surface`, `radii.large`). Mostra `BackButton` quando `pathname !== '/'` |
| `FormMessage` | `components/auth/form-message.tsx` (3 usos) | `message`, `tone` (`'error'` ou `'success'`) | `Text` com `accessibilityLiveRegion="polite"`; role `alert` no erro. Erro de **API** não usa este componente (usa toast); serve a mensagens inline de formulário |
| `IconAction` | `components/icon-action.tsx` (5 usos) | `label`, `onPress`, `children` (o ícone), `selected?`, `disabled?`, `loading?`, `radio?` | Botão 44×44 (`radii.small`, borda de 2px). Papel: `button` sem `selected`, `checkbox` com `selected`, `radio` com `radio`. `selected` troca borda para `brand` e fundo para `brandSurface`. `loading` mostra `ActivityIndicator`. Mostra tooltip com o `label` em hover, foco e toque longo (largura `posterWidth + spacing.large`). Pressionado/desabilitado/carregando: `opacity` 0,65 |
| `ModalDialog` | `components/modal-dialog.tsx` (5 usos) | `visible`, `title`, `onClose`, `busy?`, `children` | `Modal` transparente com `animationType="fade"`; overlay de tela cheia em `background` (**opaco**, não é scrim), diálogo `surface` com `radii.medium`, `maxWidth: drawerWidth * 2`, corpo em `ScrollView` e `KeyboardAvoidingView`. `accessibilityViewIsModal`; fechar por botão "Fechar janela" (44px), toque no fundo ou botão voltar do Android; com `busy` o fechamento fica bloqueado. Renderiza o próprio `ToastViewport` (o `Modal` cobre o do provider) |
| `UserAvatar` | `components/user-avatar.tsx` (5 usos) | `name`, `avatarKey?`, `uri?`, `size?` (padrão `dimensions.minimumTouchTarget`) | Círculo (`radii.pill`). Ordem: SVG do `avatarOptions` pela chave, depois imagem `uri` (`expo-image`), depois inicial do nome (`pt-BR`). `accessibilityRole="image"` com label "Avatar de {nome}, {personagem}". Exporta `avatarOptions` (9 itens: `key`, `label`, `category`, `Graphic`) |

### 5.2 Navegação

| Componente | Arquivo | Props | Comportamento |
| --- | --- | --- | --- |
| `AppHeader` | `components/navigation/app-header.tsx` (1 uso: `home`) | nenhuma | Logo branco (`logoWidth`×`logoHeight`), botões de busca e notificações (44px, `radii.pill`, `surfaceElevated`), badge de não lidas (`brand`, mostra `99+`) com label "Notificações, N não lidas", e o `UserDrawer`. Consulta a contagem a cada 60s; erro vai para `useErrorToast` |
| `BackButton` | `components/navigation/back-button.tsx` (14 usos) | `onPress?` | Dois `IconAction`: "Voltar" (**glifo `←` em `Text`**, não ícone) e "Ir para Home" (`home.svg`). Sem `onPress`, volta; sem histórico, vai a `/home` (com sessão) ou `/` |
| `UserDrawer` | `components/navigation/user-drawer.tsx` (1 uso, dentro do `AppHeader`) | nenhuma | Avatar como gatilho (`accessibilityState.expanded`), `Modal` com `Animated.View` deslizando (`translateX`, 220ms abrir e 200ms fechar, largura `min(320, tela - 44)`), nome e e-mail, conquistas (`AchievementGrid compact`), 9 destinos de navegação, confirmação inline de "Sair da conta". Renderiza seu `ToastViewport` |

### 5.3 Toast (`providers/toast-provider.tsx`)

| API | Contrato |
| --- | --- |
| `ToastProvider` | Envolve o app em `_layout.tsx`, dentro do `QueryClientProvider` e fora do `AuthProvider`; renderiza um único `ToastViewport` |
| `useToast()` | `{ toast, showToast(message, tone?), showError(message), dismissToast() }`; lança erro fora do provider. `tone` é `'error'` (padrão) ou `'success'` |
| `useErrorToast(error, message)` | Chama `showError(message)` quando `error` é verdadeiro; dispara de novo se `error` mudar de identidade |
| `ToastViewport` | Exibe o toast atual; deve ser renderizado **dentro** de todo `Modal` próprio (já feito em `ModalDialog` e `UserDrawer`) |

Comportamento: só **um** toast por vez (um novo substitui o atual), some após 5 s (`TOAST_DURATION_MS`), entra com fade de 180ms, aparece no topo respeitando a área segura. Erro: borda `brand`, título "Algo deu errado" em `brandSecondary`, live region `assertive`. Sucesso: borda e título `success`, título "Tudo certo", `polite`. Botão de fechar de 44px com o glifo `×` e label "Fechar aviso". Nenhum código chama `showToast(..., 'success')` hoje.

### 5.4 Composições de feature (não são primitivos)

| Componente | Arquivo | Props | Papel |
| --- | --- | --- | --- |
| `TitlePosterCard` | `components/discovery/title-poster-card.tsx` (6 usos) | `title` (`id`, `provider`, `externalId`, `contentType`, `title`, `releaseDate`, `posterUrl`, `genres?`), `children?`, `actionLabel?` | Card 120×180 que abre `/title/...`; pôster ausente mostra "Pôster indisponível". Label "{actionLabel ?? 'Abrir detalhes de'} {título}" |
| `MediaTitleRow` | `components/discovery/media-title-row.tsx` (2 usos) | `title` (`CatalogTitle`), `children?`, `layout?` (`'row'` ou `'poster'`) | Linha com pôster 64×96, tipo, ano e gêneros; `children` viram ações abaixo |
| `AchievementMedal` | `components/gamification/achievement-medal.tsx` (3 usos) | `code`, `title`, `earned`, `size?` (64) | Mapeia 8 códigos para os SVGs de `badges/`; código desconhecido retorna `null`; bloqueada com `opacity` 0,35; `accessibilityRole="image"` |
| `AchievementGrid` | `components/gamification/achievement-grid.tsx` (2 usos) | `achievements`, `compact?` | Grade de medalhas tocáveis que abre `ModalDialog` com detalhes |
| `SocialActivityCard` | `components/social/social-activity-card.tsx` (2 usos) | `activity`, `compact?` | Atividade de amigo; busca o título por React Query, erro via toast |
| `TitleCollectionActions` | `components/discovery/title-collection-actions.tsx` | `title` | Ações Quero assistir, favoritar e adicionar a lista (com `ModalDialog`) |
| `RatingReviewSection` | `components/reviews/rating-review-section.tsx` | `targetId`, `targetType`, `seriesId?`, `allowRating?` (`true`) | Nota em meia estrela, comentário, audiência e spoiler; confirma exclusão com `Alert.alert` |
| Seções da Home | `components/discovery/` | `DiscoveryFeedSection` (`title`, `type`, `category`, `genreIds?`), `FavoriteTitleSection` (`type`, `title`, `collection?`), `StartWatchingSection`, `TrackedSeriesSection` (`onOpenTracking`), `UpcomingCalendarSection` | Cada uma reimplementa seus blocos de carregando, vazio e erro |
| `formatRatingStars` | `components/reviews/rating-stars.ts` | `score` | Função, não componente: devolve texto com `★`, `◐` e `☆` |

### 5.5 Componentes ausentes hoje

Cada item abaixo foi procurado no código e não existe como componente compartilhado.

| Ausente | O que existe no lugar |
| --- | --- |
| `Text` / tipografia | `Text` do RN com `color` + `fontSize` + `fontFamily` repetidos em cada `StyleSheet` |
| `Screen` / layout de tela | `SafeAreaView` em 5 telas, `ScrollView` e `View` soltos nas demais; sem cabeçalho de tela compartilhado |
| `Card` / `Surface` | `surface` + `radii` + `padding` repetidos (`formCard`, `status`, cartões de seção) |
| `Input` genérico | `TextInput` cru em 8 arquivos (`lists`, `onboarding`, `profile`, `search`, `social`, `tracking`, `title-collection-actions`, `rating-review-section`), cada um com seu estilo; `AuthInput` só serve a autenticação |
| `Chip` / `Tabs` | `Pressable` com `accessibilityRole="tab"` implementado em `home`, `search`, `social`, `social-profile` e no calendário |
| `Skeleton` | Não existe; carregamento é `ActivityIndicator` (76 usos) |
| `EmptyState` | Texto solto; `assets/brand/ui/empty-state.svg` sem uso |
| `ErrorState` / `LoadingState` | Bloco "status" + "Tentar novamente" reescrito em cada seção (22 arquivos) |
| `Pagination` / "carregar mais" | `onEndReached` em `FlatList` e botões como "Mais listas" |
| `Switch` | `Switch` do RN em `gamification`, `notifications` e `profile`, repetindo `thumbColor` e `trackColor` |
| `Badge` | Badge de não lidas inline no `AppHeader` |
| `Icon` | Cada SVG importado individualmente com `width`/`height` de `dimensions.iconSize` |
| `ConfirmDialog` | `Alert.alert` (2), `ModalDialog` (1) e confirmação inline (1) |
| `ProgressBar` | Trilha de progresso inline em `gamification.tsx` e `onboarding.tsx` |
| `RatingStars` (controle) | Botões de estrela inline em `rating-review-section.tsx` e glifos em `rating-stars.ts` |
| `Divider`, `SectionHeader` | Não procurados em detalhe; estilos repetidos |
| Tokens de sombra, elevação, `lineHeight`, duração, `zIndex` | Valores soltos |

## 6. Como adicionar um componente

1. Confirme em §5 e em `mobile/src/components/` que não existe equivalente; se o caso estiver em §5.5, é um candidato legítimo a primitivo.
2. Defina o contrato: propósito, props, variantes, estados e acessibilidade (§4). Escreva em pt-BR os rótulos e `accessibilityLabel`.
3. Crie o arquivo (primitivo em `components/ui/<nome>.tsx`, composição em `components/<dominio>/`) com `StyleSheet.create` e **só** tokens de `@/theme/tokens`. Precisa de um valor que não existe? Adicione o token primeiro.
4. Use ícones e ilustrações do kit (`mobile/assets/brand/`) importados como SVG; não use glifos de texto nem outra biblioteca de ícones.
5. Trate os estados: carregando, desabilitado, erro e foco. Erros de API passam pelo toast, não pelo componente.
6. Verifique alvo de 44px, roles, labels, `accessibilityState` e contraste dos pares novos (use os números de `DESIGN.md` §2.5 ou calcule).
7. Use o componente em pelo menos uma tela real e, se a tela mudou, atualize `.docs/design/wireframes/reelix-visual-wireframes.html`.
8. Rode `npx expo lint` e `npx tsc --noEmit` em `mobile/` antes de concluir.
9. Atualize este documento (§1, §5 e §5.5) se o contrato ou o local mudar.

## 7. Do / Don't

| Faça | Não faça |
| --- | --- |
| Compor primitivos para montar uma tela | Copiar um componente e trocar uma cor |
| Usar `colors.brand` e demais tokens | Escrever `#FF2D55` no componente |
| Manter um único `PrimaryButton` com `variant` | Criar `RedButton`, `BigButton` |
| Passar `loading`/`disabled` ao botão | Esconder o botão enquanto envia |
| Importar `@/theme/tokens` | Importar valores de `design-tokens.json` no código |
| Testar estados e leitor de tela | Validar só o caminho feliz |
| Registrar a nova lacuna em §5.5 ou `DESIGN.md` §13 | Deixar dívida sem documentar |

## 8. Lacunas para implementar

Consolidação planejada na spec futura em `.specs/`; nada abaixo está em andamento.

- [ ] Criar `Text` com variantes de `typography` e `lineHeight` tokenizado.
- [ ] Criar `Screen` (área segura, `ScrollView`, cabeçalho, estados de carregamento/erro).
- [ ] Criar `Card`/`Surface` e `Input` genérico (e desacoplar do `react-hook-form` ou criar variante controlada).
- [ ] Criar `Chip`/`Tabs`, `Switch`, `Badge`, `ProgressBar`, `RatingStars` (controle) e `Icon`.
- [ ] Criar `EmptyState`, `ErrorState`, `LoadingState` e `Skeleton`; aproveitar ou descartar `ui/empty-state.svg`.
- [ ] Criar `ConfirmDialog` e unificar os quatro padrões de confirmação.
- [ ] Mover `PrimaryButton` para o diretório de primitivos e passar a aceitar `style`.
- [ ] Trocar `←` e `×` por ícones do kit e `★◐☆` por controle acessível.
- [ ] Tokens de sombra, elevação, `lineHeight`, duração e `zIndex`; remover os 2 hex fora de `tokens.ts` e o `fontSize: 24`.
- [ ] Migrar paddings, margins, raios e dimensões numéricos para tokens.
- [ ] Sincronizar `tokens.ts` e `design-tokens.json` (§3.3); decidir `error` distinto de `brandSecondary`.
- [ ] Migrar animações para `react-native-reanimated` (ou remover a dependência) e respeitar movimento reduzido.
- [ ] Decidir o destino de `expo-symbols`, `@expo/ui` e `expo-glass-effect` (instalados, sem uso).
- [ ] Adicionar `testID` e testes de componente.
- [ ] Remover os assets do template Expo de `assets/images/` e `assets/expo.icon/` se confirmado que não são usados.
