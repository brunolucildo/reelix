# Design

> **Público:** qualquer pessoa que crie, altere ou revise UI do app mobile.
> **Obrigatório** para tarefas de interface. Complementa `DESIGN_SYSTEM.md`.
> **Estado:** baseline auditado em 2026-11-19: descreve a UI **como está** (tema escuro único, Sora, tokens em `mobile/src/theme/tokens.ts`, sem sistema de componentes completo). A refatoração visual será uma spec futura em `.specs/` (a pasta ainda não existe); até lá este documento registra regras e lacunas, não uma identidade nova.

## 1. Direção

Interface **cinematográfica**: superfícies quase pretas, tipografia branca de alto contraste e um acento vermelho/rosa de marca. Use o vermelho para ação primária, navegação ativa e ênfase; não o use como preenchimento de tela inteira nem como gradiente decorativo. Os demais tons de apoio (verde, âmbar, azul) existem só para estados.

Fontes da identidade, em ordem de autoridade:

1. `mobile/src/theme/tokens.ts`: o que o app realmente usa.
2. `mobile/assets/brand/` (logos, ícones, badges, avatares, `docs/design-tokens.json`): kit de marca editável.
3. `.docs/design/visual_identity.png`: **mood board**, não especificação. Ele mostra valores e tipografia diferentes do kit (ver §13.2); em conflito, vale o kit e o `tokens.ts`.
4. `.docs/design/wireframes/reelix-visual-wireframes.html`: wireframes AS IS / TO BE (ver §11).

Princípios, em ordem de prioridade:

1. **Legibilidade** vence estilo. Texto branco ou `textSecondary` sobre superfícies escuras; nunca texto de corpo em cor de acento.
2. **Dados reais.** Nada de título, contagem ou módulo simulado: se o módulo não existe, omita a UI ou mostre um estado vazio explícito.
3. **Feedback claro.** Erro de API em toast; validação e sucesso inline; todo estado (carregando, vazio, erro) é desenhado.
4. **Consistência por tokens.** Cor, espaçamento, tipografia e raio vêm de `tokens.ts`, não de valores soltos.
5. **Acento com intenção.** Uma ação primária por tela em `brand`; destaques secundários em `brandSecondary`.
6. **Movimento contido.** Transições curtas e funcionais, nunca decorativas em excesso.
7. **Acessível por padrão.** Alvos de 44px, roles e labels, estados que não dependem só de cor.

## 2. Paleta

### 2.1 Tokens atuais (`colors`)

| Token | Hex | Papel no código |
| --- | --- | --- |
| `brand` | `#FF2D55` | Ação primária (`PrimaryButton`), seleção, badge de não lidas, spinner, trilha de progresso |
| `brandSecondary` | `#FF6688` | Foco de campo, links, rótulos de ação, título do toast de erro |
| `brandSurface` | `#35131D` | Fundo de controle selecionado (`IconAction` com `selected`) |
| `background` | `#080B0F` | Fundo de tela, texto sobre `brand`, fundo do overlay do `ModalDialog` |
| `backgroundTransparent` | `#080B0F00` | Início do degradê do hero do título (`LinearGradient`) |
| `surface` | `#1A1A1E` | Cartões, campos, drawer, diálogo |
| `surfaceElevated` | `#2A2A2E` | Botão secundário, botões de ícone, toast, placeholder de pôster |
| `border` | `#2A2A2E` | Bordas finas (mesmo hex de `surfaceElevated`) |
| `textPrimary` | `#FFFFFF` | Texto principal |
| `textSecondary` | `#B8B8BD` | Texto de apoio, metadados, placeholder de campo |
| `error` | `#FF6688` | Texto e borda de erro (mesmo hex de `brandSecondary`) |
| `success` | `#9ADDAA` | Confirmação inline, borda do toast de sucesso |
| `warning` | `#FBBF24` | Avisos inline: validação, limite de seleção e nota de privacidade (4 usos) |
| `info` | `#38BDF8` | Definido, **sem nenhum uso** em `mobile/src` |
| `white` | `#FFFFFF` | Thumb do `Switch`, alguns textos (5 usos) |

Hex só existe em `tokens.ts`. Exceções conhecidas no código: `shadowColor: '#000000'` em `providers/toast-provider.tsx:135` e `lightColor: '#27D17F'` em `services/push-registration.ts:33` (cor do LED do canal de notificação Android). Fora de `src`, `app.json` repete `#080B0F` (ícone adaptativo e splash).

### 2.2 Outras cores do kit de marca (fora de `tokens.ts`)

| Valor | Origem | Observação |
| --- | --- | --- |
| `#E5E7EB` (`neutral`) | `design-tokens.json` e `assets/brand/README.md` | Ícones e arte neutros; **não existe** em `tokens.ts` |

### 2.3 Regras de uso

- Texto sobre `brand` usa `background` (contraste 5,41:1), nunca branco (3,65:1).
- Erro (`error`) e destaque (`brandSecondary`) têm o **mesmo hex**: hoje não há como distinguir os dois só pela cor. Por isso erro vem sempre acompanhado de mensagem em texto e, no toast, do título "Algo deu errado".
- Cor nunca é o único indicador de estado; combine com texto, ícone ou `accessibilityState` (ver §8 para os casos que hoje descumprem).
- Não crie cor nova no componente. Uma cor nova entra em `tokens.ts` e, se for do kit de marca, também em `design-tokens.json`.

### 2.4 Tema

Só **tema escuro**: `app.json` fixa `userInterfaceStyle: "dark"` e o código não usa `useColorScheme` nem `Appearance`. Não há tema claro nem tokens semânticos por tema; `colors` é um objeto único. Se um tema claro vier, será decisão de spec (ver §13).

### 2.5 Contraste calculado

Calculado em 2026-11-19 a partir dos hex de `tokens.ts` com a fórmula WCAG 2.x (luminância relativa). **Não há teste automatizado no repositório e nada foi medido em dispositivo.** Meta: texto 4,5:1; componentes de UI 3:1.

| Par | Contraste | Leitura |
| --- | --- | --- |
| `textPrimary` sobre `background` / `surface` / `surfaceElevated` | 19,72 / 17,35 / 14,29 | Passa |
| `textSecondary` sobre `background` / `surface` / `surfaceElevated` | 9,98 / 8,78 / 7,24 | Passa |
| `background` sobre `brand` (rótulo do `PrimaryButton`) | 5,41 | Passa |
| `textPrimary` sobre `brand` | 3,65 | Não use para texto normal |
| `brand` sobre `background` / `surface` | 5,41 / 4,76 | Passa como texto e ícone |
| `brandSecondary` e `error` sobre `background` / `surface` / `surfaceElevated` | 7,05 / 6,20 / 5,11 | Passa |
| `success` sobre `background` / `surfaceElevated` | 12,48 / 9,05 | Passa |
| `warning` sobre `background` / `surface` | 11,81 / 10,39 | Passa |
| `info` sobre `background` | 9,20 | Passa (sem uso hoje) |
| `brandSecondary` sobre `brandSurface` | 5,95 | Passa |
| `border` sobre `background` / `surface` | 1,38 / 1,21 | **Abaixo de 3:1** para a borda de campos; a conformidade com 1.4.11 não foi avaliada |
| `surface` sobre `background` | 1,14 | Separação de cartão é sutil; não carrega significado sozinha |
| Texto com `opacity` (estado `disabled` 0,55; `pressed` 0,65 a 0,82) | **não medido** | Opacidade reduz o contraste efetivo |
| Texto sobre pôster/backdrop (hero do título) | **não medido** | Depende da imagem; há degradê até `background` |

## 3. Tipografia

Família: **Sora** (`@expo-google-fonts/sora`), carregada uma vez no `app/_layout.tsx` com `useFonts` em cinco pesos, referenciados só por `fontFamily` de `tokens.ts`. Não há fallback configurado: se a fonte falha, o app mostra uma tela de erro (`accessibilityRole="alert"`) em vez de cair em fonte do sistema.

| Token `fontFamily` | Fonte | Usos |
| --- | --- | --- |
| `regular` | `Sora_400Regular` | Corpo, metadados |
| `medium` | `Sora_500Medium` | Itens de menu, rótulos secundários |
| `semiBold` | `Sora_600SemiBold` | Rótulos de campo, links, títulos de card |
| `bold` | `Sora_700Bold` | Botões, títulos de seção, nomes |
| `extraBold` | `Sora_800ExtraBold` | Título de tela de autenticação (`display`) |

| Token `typography` | Tamanho | Observação |
| --- | --- | --- |
| `display` | 32 | Título das telas de autenticação (1 uso) |
| `title` | 28 | Títulos de tela; glifo `←` do `BackButton` |
| `section` | 18 | Cabeçalho de seção e de diálogo |
| `body` | 15 | Texto corrido e rótulo de botão |
| `bodySmall` | 14 | Texto de apoio, rótulos de campo, links |
| `caption` | 12 | Metadados, erro de campo, legenda |
| `eyebrow` | 11 | Sobretítulo (2 telas: `notifications`, `onboarding`) |

Regras:

- `fontSize` e `fontFamily` vêm sempre dos tokens. Exceção atual: `fontSize: 24` no `×` do toast (`toast-provider.tsx:191`).
- Não existe token de `lineHeight` nem de `letterSpacing`: há 26 usos de `lineHeight` (18 com número literal, os demais calculados a partir de `typography`/`spacing`) e um `letterSpacing: -0.8` literal no título de autenticação.
- Corpo mínimo do app é 15px e metadados chegam a 11px. Não há regra de tamanho mínimo documentada além disso.
- Escala dinâmica de fonte: não há `allowFontScaling` nem `maxFontSizeMultiplier` no código (vale o padrão do React Native, que escala). **Não testado** com fonte grande do sistema.
- Não existe componente `Text`; cada tela repete `color` + `fontSize` + `fontFamily` em `StyleSheet` (ver `DESIGN_SYSTEM.md` §5.5).

## 4. Espaçamento, raios e dimensões

`spacing` (múltiplos de 4): `extraSmall` 4, `small` 8, `medium` 16, `large` 24, `extraLarge` 32. Mais usados: `small` (149) e `medium` (141).

`radii`: `extraSmall` 4, `small` 8 (campos, botões de ícone), `medium` 14 (botões, cartões, diálogos), `large` 24 (cartão de formulário), `pill` 999 (avatares, botões circulares).

`dimensions`: `posterWidth` 120, `posterHeight` 180, `minimumTouchTarget` 44, `drawerWidth` 320, `avatarSize` 56, `logoWidth` 120, `logoHeight` 36, `iconSize` 24.

Fora dos tokens hoje: 14 `padding`/`margin` numéricos (concentrados em `onboarding.tsx`, `profile.tsx`, `_layout.tsx` e `auth-link.tsx`), 2 `borderRadius` literais (`my-ratings.tsx:110`, `notifications.tsx:352`) e cerca de 90 `width`/`height`/`min*`/`max*` numéricos (por exemplo `minHeight: 54` nos campos e no botão, `width: 64`/`height: 96` no pôster da linha de título). Não existe token de sombra, elevação, duração de animação nem `zIndex`.

Layout:

- Orientação **retrato** (`app.json`). Não há breakpoints, layout de tablet nem de paisagem; a largura é limitada por `maxWidth` pontuais (440 na autenticação, `drawerWidth * 2` = 640 em diálogos e atividades sociais).
- Área segura: só 5 telas usam `SafeAreaView` (`home`, `lists`, `my-ratings`, `settings`, `social-profile`) e `tracking` usa `useSafeAreaInsets`. As demais telas não tratam a área segura explicitamente; **o efeito visual não foi verificado**.
- Alvos de toque de no mínimo 44×44px (`dimensions.minimumTouchTarget`, 79 usos).

## 5. Marca, ícones, ilustrações e badges

Fonte: `mobile/assets/brand/` (SVGs editáveis; PNGs gerados por `npm run brand:generate`). Veja o `README.md` da pasta para o catálogo completo.

- **Logos** (`logos/`): `logo-horizontal` (cor, em fundo escuro), `logo-white` (monocromático branco, cabeçalho), `logo-vertical` (splash), `logo-black` (fundo claro), `symbol` (marca isolada) e `tmdb-logo-blue` (atribuição de dados, usado em `profile.tsx`). Não redesenhe, recolora nem aproxime o logo com texto estilizado.
- **Ícones de app** (`app-icons/`): ícone escuro, alternativo vermelho, primeiro plano e monocromático do Android, favicon. O ícone da loja não serve de primeiro plano adaptativo.
- **Ícones de navegação** (`navigation-icons/`): 28 SVGs 24×24 com traços vermelho, rosa ou neutro explícitos; 22 deles são importados hoje. Preserve traço e preenchimento; não troque por emoji nem por glifo de plataforma.
- **Ícones de gênero** (`genre-icons/`, 9 SVGs 48×48): **nenhum import em `mobile/src`**.
- **Badges** (`badges/`, 8 medalhas 96×108): usados pelo `AchievementMedal`, um por código de conquista.
- **Avatares** (`avatars/`, 9 desenhos, `viewBox="0 0 100 100"`): `noir`, `cosmic`, `phantom`, `comedy`, `animation`, `adventure`, `robot`, `vampire`, `maestro` (nomes, temas e regras no `README.md` da pasta). A opção "iniciais" remove a chave persistida. Nova opção exige atualização coordenada do catálogo mobile (`avatarOptions` em `user-avatar.tsx`), do domínio e da restrição SQL; a API não aceita URL externa como chave. Não há upload de avatar.
- **Ilustrações de UI** (`ui/empty-state.svg`, `ui/splash-screen.svg`): conceitos editáveis, **sem uso** em `mobile/src` (o splash usa `logo-vertical.png`, via `app.json`).

Uso dos SVGs: importados como componentes (`react-native-svg` + `react-native-svg-transformer`, configurado em `metro.config.js`), com `width`/`height` vindos de `dimensions.iconSize`, `logoWidth` etc. Atenção ao reaproveitamento de ícones: `close.svg` serve também de "Sair da conta" no drawer e `clock.svg` serve a "Quero assistir" e a "Acompanhamento".

Divergência a resolver: o `assets/brand/README.md` diz que os assets do Expo foram removidos de `assets/images/`, mas a pasta ainda contém os PNGs do template (`expo-logo.png`, `react-logo*.png`, etc.) e existe `assets/expo.icon/`. Nenhum import foi encontrado em `src`, `app.json` ou `plugins/`.

## 6. Navegação e estrutura de tela

- Expo Router com `Stack` sem cabeçalho nativo (`headerShown: false`). Rotas públicas protegidas por `Stack.Protected` (`index`, `sign-up`, `forgot-password`) e rotas autenticadas (`home`, `onboarding`, `profile`, `lists`, `tracking`, `search`, `social`, `notifications`, `gamification`, `settings`, `my-ratings`, `social-profile/[accountId]`, `title/...`, `episode/...`, `auth/reset-password`); `auth/callback` fica fora das proteções.
- Não há barra de abas nem cabeçalho compartilhado. O `AppHeader` (logo, busca, notificações, avatar) só existe na `home`; as demais telas usam o `BackButton` (voltar + ir para a Home). O menu da conta é o `UserDrawer`, um drawer próprio sobre `Modal`.
- Abas de conteúdo (Séries/Filmes na `home`, e em `search`, `social`, `social-profile` e no calendário) são `Pressable` com `accessibilityRole="tab"` implementados localmente em cada tela.
- Telas de autenticação usam o `AuthScreen` (logo horizontal, título, subtítulo, cartão de formulário).
- Estados de inicialização do app (`Carregando sua sessão...`, erro de inicialização com "Tentar novamente", fonte falhou) vivem em `_layout.tsx`.

## 7. Movimento

Hoje só existem três animações, todas com `Animated` do React Native e `useNativeDriver: false`: abertura e fechamento do drawer (220ms e 200ms), entrada do toast (180ms) e `transition={150}` nas imagens do `expo-image`. `react-native-reanimated` está instalado e não é usado.

Regras:

- Duração curta: 150–220ms para feedback e entrada de painel (valores já em uso). Registre novas durações num token quando o token de duração existir.
- Animar só propriedades que não causam relayout repetido; sem "piscar" constante.
- **Reduzir movimento:** o código não consulta `AccessibilityInfo.isReduceMotionEnabled`. Toda animação nova deve respeitar essa preferência; as existentes são uma lacuna.
- Ao migrar para `react-native-reanimated`, use o driver nativo; `expo-linear-gradient` já é usado de forma funcional no hero do título (transição até `background`), não como enfeite.

## 8. Acessibilidade (obrigatório)

Meta declarada: alvos adequados, contraste e leitura por leitor de tela. **Nenhuma verificação com TalkBack ou VoiceOver foi registrada**, e não há lint de acessibilidade além do `eslint-config-expo`.

Regras:

- **Alvos de toque** de no mínimo 44×44px (`minimumTouchTarget`); use `hitSlop` só quando o visual precisar ser menor (hoje só o botão de fechar do toast).
- **Roles e labels** em todo controle só com ícone e em todo alternador. O código tem 164 `accessibilityRole` e 103 `accessibilityLabel`. Padrões em uso: `button`, `link`, `tab`/`tablist`, `radio`, `checkbox`, `header`, `alert`, `image`, `progressbar`.
- **Estados**: `accessibilityState` com `selected`, `checked`, `disabled`, `busy`, `expanded`. O `PrimaryButton` e o `IconAction` já expõem `busy`/`disabled`.
- **Campos**: rótulo visível acima do campo (`AuthInput`), `accessibilityLabel` no `TextInput`, erro como `Text` com `accessibilityRole="alert"` e repetido em `accessibilityHint`. Foco visível: borda `brandSecondary` (contraste 6,20:1 sobre `surface`).
- **Erro e sucesso** anunciados: o toast usa `accessibilityLiveRegion` `assertive` (erro) ou `polite` (sucesso); `FormMessage` usa `polite`.
- **Diálogos e drawer** usam `accessibilityViewIsModal`; fechar tem label ("Fechar janela", "Fechar menu").
- **Imagens**: pôster com `accessibilityLabel`; arte decorativa não deve ser anunciada.
- **Estado não pode depender só de cor.** Lacuna: o estado `selected` do `IconAction` se distingue visualmente só pela borda `brand` e pelo fundo `brandSurface` (o leitor de tela recebe `checked`).
- Testes: não há `testID` em nenhum componente (0 ocorrências) e `mobile/` não contém nenhum arquivo de teste (`*.test.*`, `*.spec.*`, `__tests__`).

## 9. Padrões de UX por tela

Regras obrigatórias para toda tela nova ou alterada (herdadas dos padrões do projeto e confirmadas no código).

**Feedback**

- **Erro de API, rede, carregamento, salvamento e saída da conta:** toast com `useToast().showError(message)` ou `useErrorToast(error, message)` de `mobile/src/providers/toast-provider.tsx`. Não renderize erro de API como texto inline e não crie outro mecanismo de toast, snackbar ou alerta de erro.
- **Validação de campo, limite de seleção e mensagem de sucesso:** inline, junto ao controle. Hoje o toast de sucesso existe (`showToast(message, 'success')`), mas nenhum código o chama; o sucesso fica inline (`FormMessage` com `tone="success"`, textos com `accessibilityLiveRegion`).
- **Tela que não renderiza sem dados:** toast **e** estado de erro no lugar do conteúdo, com ação "Tentar novamente". Nunca deixe tela em branco nem spinner infinito (padrão presente em `home`, `discovery-feed-section` e em 22 arquivos com a ação).

**Estados** (todo bloco que busca dados define os quatro)

| Estado | Padrão atual | Observação |
| --- | --- | --- |
| Carregando | `ActivityIndicator` (`brand`) com texto curto, por exemplo "Carregando títulos..." | 76 usos; sem skeleton |
| Vazio | Texto em `textSecondary` explicando o que fazer, por exemplo "Registre episódios assistidos para acompanhar suas séries aqui." | Sem componente; `ui/empty-state.svg` sem uso |
| Erro | Toast + texto "indisponível" + botão "Tentar novamente" | Cada seção reimplementa o bloco |
| Sucesso parcial / paginação | "Mais listas", `onEndReached` em listas horizontais, `RefreshControl` em `social` e `social-profile` | Sem componente de paginação |

**Formulários**

- Campos de senha usam `AuthInput` com `secureTextEntry`, que já traz o alternador Mostrar/Ocultar com labels acessíveis.
- Validação com `react-hook-form` + `zod`; mensagem de erro do campo logo abaixo, em `error`.
- Botão de envio com `loading` enquanto envia; não bloqueie com spinner de tela inteira.
- `AuthInput` está acoplado ao `react-hook-form`; fora dos formulários de autenticação 8 arquivos usam `TextInput` cru com estilo repetido (ver `DESIGN_SYSTEM.md` §5.5).

**Confirmações destrutivas** não têm padrão único: há `Alert.alert` nativo (`tracking.tsx:190` e `rating-review-section.tsx:185`), `ModalDialog` ("Remover amizade?", `social.tsx:420`) e confirmação inline no drawer ("Sair da conta?"). Para novas telas, prefira `ModalDialog` até existir um componente de confirmação (decisão pendente, §13.3).

**Copy e dados**

- Idioma **pt-BR** em toda a interface (inclusive `accessibilityLabel`). Datas com `toLocaleDateString('pt-BR')`.
- A mensagem não revela informação sensível: se um e-mail tem conta, IDs internos, rastros de erro. Em resposta 401, peça para entrar de novo.
- Microcopy de erro diz o que aconteceu e o que fazer em seguida ("Não foi possível carregar seu perfil. Tente novamente.").
- Dados reais só: nada de `lorem ipsum`, título fictício, contagem ou estatística simulada.

**Rotas existentes** (para orientar onde um padrão se aplica): autenticação (`index`, `sign-up`, `forgot-password`, `auth/callback`, `auth/reset-password`), `onboarding`, `home`, `search`, `title/[provider]/[contentType]/[externalId]`, `episode/...`, `tracking`, `lists`, `my-ratings`, `profile`, `settings`, `social`, `social-profile/[accountId]`, `notifications`, `gamification`.

## 10. Conteúdo e tom

- Idioma: **pt-BR** na interface; esta documentação também.
- Tom direto e próximo ("Suas histórias começam aqui.", "Entre para acompanhar tudo o que você assiste."); evite jargão técnico e de marketing.
- Rótulos de ação com verbo: "Tentar novamente", "Criar e adicionar", "Confirmar saída".
- Datas e números no padrão `pt-BR`; datas de API em UTC, exibidas no fuso do aparelho.
- Não entregue placeholder, texto de template nem `lorem ipsum` em tela final.

## 11. Regras para wireframes

O arquivo `.docs/design/wireframes/reelix-visual-wireframes.html` guarda os wireframes visuais AS IS e TO BE (baseados nas specs de autenticação, onboarding, catálogo, tracking e listas).

- Toda tela adicionada ou alterada atualiza o arquivo **na mesma mudança**, incluindo o status AS IS/TO BE, as notas, os estados (carregando, vazio, erro) e os fluxos.
- Wireframe mostra estados reais: se a tela tem erro de API, mostre o toast e o estado "Tentar novamente"; não desenhe erro inline de API.
- Use valores de `tokens.ts` como referência de cor, espaçamento e raio. O CSS do arquivo hoje diverge deles (ver §13.2); o wireframe é ilustração, `tokens.ts` é a fonte.
- Sem dados inventados: use nomes e estados que o app realmente exibe, ou marque como exemplo.
- O arquivo permanece em `.docs/design/wireframes/`; este documento só o referencia.

## 12. Do / Don't

| Faça | Não faça |
| --- | --- |
| Usar `colors`, `spacing`, `typography`, `radii` e `fontFamily` de `tokens.ts` | Escrever `#FF2D55` ou `fontSize: 15` na tela |
| Texto `background` sobre botão `brand` | Texto branco sobre `brand` |
| Erro de API em toast + estado "Tentar novamente" | Erro de API como texto inline |
| Validação e sucesso inline | Toast para validação de campo |
| Alvo de 44px e `accessibilityRole`/`accessibilityLabel` em ícones | Botão de ícone sem label |
| `AuthInput` para senha (com Mostrar/Ocultar) | `TextInput` seguro sem alternador |
| SVGs do kit de marca como componentes | Glifo (`←`, `×`, `★`) no lugar de ícone do kit |
| Usar o logo do kit na variante certa | Redesenhar ou recolorir o logo |
| Atualizar o wireframe junto com a tela | Mudar a tela e deixar o wireframe desatualizado |

## 13. Estado e lacunas

Fatos da auditoria de 2026-11-19 sobre `mobile/src` (12.496 linhas de TS/TSX).

### 13.1 Situação atual

| Item | Estado |
| --- | --- |
| Tokens centralizados | Sim: `colors`, `spacing`, `typography`, `radii`, `dimensions`, `fontFamily` em `theme/tokens.ts` |
| Uso de `StyleSheet` | `StyleSheet.create` em 40 arquivos; sem biblioteca de estilo |
| Hex fora do `tokens.ts` | 2 ocorrências: `toast-provider.tsx:135` (`#000000`) e `push-registration.ts:33` (`#27D17F`) |
| `fontSize` fora dos tokens | 1 ocorrência: `toast-provider.tsx:191` (24) |
| Paddings/margins numéricos | 14 |
| `width`/`height`/`min*`/`max*` numéricos | cerca de 90 |
| `lineHeight` | 26 usos, 18 com número literal; sem token |
| Sombra, elevação, duração, `zIndex` | Sem token; valores soltos (toast: `shadowRadius: 18`, `zIndex: 1000`) |
| Tema | Só escuro; retrato; sem breakpoints nem tablet |
| Animação | Só `Animated` do RN com `useNativeDriver: false`; `reanimated` instalado sem uso |
| Bibliotecas instaladas sem uso | `expo-symbols`, `@expo/ui`, `expo-glass-effect` (`expo-linear-gradient` é usado 1 vez) |
| Acessibilidade | 164 `accessibilityRole`, 103 `accessibilityLabel`, 0 `testID`; preferência de movimento reduzido não consultada |
| Alerta nativo | `Alert.alert` em 2 pontos: `tracking.tsx:190` e `rating-review-section.tsx:185` |
| Ilustrações e ícones sem uso | `ui/empty-state.svg`, `ui/splash-screen.svg`, 9 ícones de gênero, 6 ícones de navegação |
| Glifos no lugar de ícones | `←` (`BackButton`), `×` (toast), `★ ◐ ☆` (`rating-stars.ts`) |

### 13.2 Divergências entre fontes

| Tema | `tokens.ts` / código | Outra fonte |
| --- | --- | --- |
| Erro × destaque | `error` = `brandSecondary` = `#FF6688` | O toast de erro usa `brand` na borda e `brandSecondary` no título |
| Borda × superfície | `border` = `surfaceElevated` = `#2A2A2E` | Contraste 1,21:1 a 1,38:1 |
| Cor neutra | Sem `#E5E7EB` | `design-tokens.json` e README do kit listam `neutral` |
| Tipografia | Sora em 5 pesos (400 a 800) | README do kit cita só `Sora_700Bold` e `Sora_600SemiBold`; o mood board mostra **Plus Jakarta Sans** |
| Fallback de fonte | Nenhum | `design-tokens.json` declara `Arial, sans-serif`; docs antigas falavam em fallback do sistema |
| Paleta do mood board | `brand` `#FF2D55`, `background` `#080B0F` | Imagem exibe `#FF3B30`, `#B91C1C`, `#3B0F10`, `#0F0F10`, `#2A2A2E`, `#E5E7EB` (valores lidos na imagem) |
| Navegação | Drawer + `AppHeader` só na Home, sem abas | O mood board mostra barra inferior (Início, Explorar, Minha Lista, Perfil) |
| Wireframes | `surface` `#1A1A1E`, `surfaceElevated` `#2A2A2E`, `textSecondary` `#B8B8BD`, `success` `#9ADDAA` | CSS do HTML usa `#10141B`, `#202D36`, `#29323B`, `#A1A1AA`, `#B7F34A` |
| Raios | `extraSmall` 4 | `design-tokens.json` não tem esse nível |
| Assets do Expo | Presentes em `assets/images/` | README do kit afirma que foram removidos |

Divergências entre `tokens.ts` e `design-tokens.json` em detalhe: `DESIGN_SYSTEM.md` §3.3.

### 13.3 Pendências e decisões em aberto

- [ ] Spec de refatoração visual em `.specs/` (identidade, tema, componentes ausentes, migração de tokens). Este documento não define identidade nova.
- [ ] Decidir se `error` ganha cor própria, distinta de `brandSecondary`.
- [ ] Definir tokens de `lineHeight`, sombra/elevação, duração de animação e `zIndex`.
- [ ] Padrão único de confirmação destrutiva (hoje há quatro).
- [ ] Substituir os glifos `←`, `×` e `★◐☆` por ícones do kit ou SVG.
- [ ] Tratar `reduce motion` e testar escala de fonte do sistema.
- [ ] Medir contraste de texto sobre imagem e com `opacity`; avaliar borda de campo (1.4.11).
- [ ] Verificar área segura nas telas sem `SafeAreaView`.
- [ ] Testar com TalkBack/VoiceOver e adicionar `testID` onde houver teste de UI.
- [ ] Definir se web (`react-native-web` instalado, `web.output: "server"`) é alvo de design; hoje nada no código ou nesta documentação o trata como tal.
- [ ] Sincronizar `design-tokens.json`, `tokens.ts` e o CSS dos wireframes.

## 14. Referências

- `mobile/src/theme/tokens.ts`: tokens em uso.
- `mobile/assets/brand/README.md` e `mobile/assets/brand/docs/design-tokens.json`: catálogo e tokens do kit.
- `mobile/assets/brand/avatars/README.md`: avatares.
- `.docs/design/visual_identity.png`: mood board.
- `.docs/design/wireframes/reelix-visual-wireframes.html`: wireframes.
- `DESIGN_SYSTEM.md`: componentes e contrato.
