# 0020 — Design: refatoração visual das telas

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** pronta (Q4 decidida em 2026-11-19: abas inferiores, opção B)
- **Slug:** design-refatoracao-visual-das-telas
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-19
- **Owner:** Produto/Design (a confirmar)
- **Relacionados:** mapa `.plans/arquitetura-design-testes-breakdown.md` (D6 a D8, D10, D11; Q3, Q4, Q6); depende da `0019` (tokens e primitivos) e, por ela, da `0018`; habilita a `0022` e a `0023` (testam as telas finais); `.docs/DESIGN.md`, `.docs/DESIGN_SYSTEM.md`, `.docs/design/wireframes/reelix-visual-wireframes.html`, mood board `.docs/design/visual_identity.png`; skills `frontend-design` e `web-design-guidelines`.

## 1. Escopo

### Dentro do escopo

- **Plano de design versionado** (entregável obrigatório e primeiro passo): cor, tipografia, conceito de layout com wireframes ASCII, princípios e revisão contra o brief (R1).
- Refatoração visual, sobre os primitivos da `0019`, das telas: navegação (Q4); entrada, cadastro e recuperação; onboarding; Home (séries/filmes, seções, calendário); busca; detalhe do título (hero, ações, avaliações, comentários, spoilers) e episódio; tracking; listas e minhas avaliações; social e perfil de amigo; notificações; gamificação; perfil e configurações.
- Estados vazio, carregando (skeleton) e erro em todas as telas; copy pt-BR revisada; movimento contido; pôster com fallback; listas longas virtualizadas; safe area; alvos, foco, roles, labels e contraste AA verificados.
- Wireframe atualizado por tela, capturas antes/depois e auditoria `web-design-guidelines` adaptada ao React Native.
- Atualizar `.docs/DESIGN.md` (e `DESIGN_SYSTEM.md` se surgir primitivo novo).

### Fora do escopo

- Arquitetura de dados, `http-client`, `features/*` e hooks: `0017` e `0018` (esta spec só altera a camada visual e a composição das telas).
- Definição de tokens e dos primitivos básicos: `0019`; aqui só entram primitivos novos que uma tela exigir (`RatingStars`, `ProgressBar`, `SectionHeader`, `Poster`), com o contrato de `DESIGN_SYSTEM.md` §4.
- Testes automatizados (Jest/RNTL, Playwright, axe): `0022` e `0023`; testes de backend: `0021`; mudanças de API ou de backend (`0013` a `0016`).
- Tema claro, tablet, paisagem e Expo web como alvo de produto (premissas P1 e P2); nova identidade, logo, fonte ou novos módulos de produto (dados simulados).

## 2. Contexto

O app tem 19 telas e, hoje, aparência de "formulário com cartões": `lists.tsx` (995 linhas) e `tracking.tsx` (903) misturam tudo; só 5 das 19 telas tratam a área segura (`home`, `lists`, `my-ratings`, `settings`, `social-profile`; o `DESIGN.md` §4 diz que `tracking` usa `useSafeAreaInsets`, mas o código não usa); há 50 `<ActivityIndicator>` e nenhum skeleton ou estado vazio com ilustração; 26 `ScrollView` contra 3 `FlatList`; 22 ocorrências de `eyebrow` em caixa-alta, 20 metas com `·` e blocos "Tentar novamente" repetidos em 22 arquivos; um erro mostra "HTTP n" ao usuário (`home.tsx:42`). A navegação é um drawer com 9 destinos aberto só pelo `AppHeader` da Home; nas outras telas só há `BackButton`. A marca é clara (quase preto, `#FF2D55`, Sora, D7), mas a composição não carrega personalidade: tudo é a mesma superfície `surface` com o mesmo raio.

O assunto é concreto: app brasileiro de cinema e séries cujo trabalho principal é **descobrir, registrar e compartilhar**. A `0019` entrega os blocos; esta spec decide como eles se compõem, com um plano de design explícito antes de tocar em tela, e fecha as lacunas de `.docs/DESIGN.md` §13.3.

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Pessoa que usa o app diariamente em telefone, em pt-BR, inclusive com leitor de tela e fonte grande |
| Decisão ou ação que muda | Encontra o que assistir, registra um episódio e compartilha em menos toques; entende qualquer estado (carregando, vazio, falha) e o que fazer a seguir |
| Resultado esperado       | Interface com hierarquia e um traço memorável, coesa por tokens, sem telas em branco, sem erro técnico exposto e acessível |
| Métrica de sucesso       | Critérios de §10 a zero (literais, `Alert.alert`, `TextInput`/`ActivityIndicator` crus, "HTTP n"), 19/19 telas com os 3 estados e capturas, auditoria sem achados abertos |

## 4. Requisitos

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | **Plano de design** em `.plans/design-refatoracao-visual-das-telas-plan.md`, aprovado antes da primeira tela: cor (4 a 6 hex nomeados derivados de `tokens.ts`), tipografia e papéis (escala, pesos, linhas < 80 caracteres), conceito de layout com wireframes ASCII (Home, detalhe do título, Minha Lista/tracking, Perfil, Entrada), princípios, **um** elemento memorável, **um** momento de movimento orquestrado, e a revisão contra o brief (cada escolha marcada "default" ou "escolha"; cortes de padrões genéricos como cartões idênticos, eyebrows, `→`, gradiente decorativo) | Must | Given a branch When a primeira alteração em `src/app` é commitada Then o plano já está commitado com as 5 seções e a tabela default × escolha |
| R2  | **Navegação (Q4):** implementar o modelo decidido: A) manter drawer e `AppHeader` só na Home, com cabeçalho e voltar consistentes nas demais telas, ou B) abas inferiores (recomendado: Início, Buscar, Minha Lista, Amigos, Perfil) com cabeçalho contextual (sino e avatar), rotas e deep links preservados e `Stack.Protected` mantido | Must | Given o modelo escolhido When o usuário alcança cada um dos 9 destinos atuais Then chega em no máximo 2 toques a partir da Home (B) ou do menu (A), com item ativo indicado por texto/ícone além da cor e alvo ≥ 44 |
| R3  | Entrada, cadastro, recuperação, callback e redefinição: `Screen` + `Card`, `Input` e `Button` com `loading`, erro inline junto ao campo com foco no primeiro campo inválido, `autoComplete` e teclado corretos, colar liberado, sem `autoFocus` | Must | Given um e-mail inválido enviado When o formulário valida Then a mensagem fica sob o campo, o foco vai a ele e o botão não fica em loading; callback com falha mostra `ErrorState` com a saída |
| R4  | Onboarding: passos reais numerados (é uma sequência), progresso, seleção de gêneros com `Chip` e limite inline, retomada preservada, saída com alterações pede confirmação | Must | Given 3 de 5 gêneros e o usuário voltando When toca em voltar Then `ConfirmDialog` oferece "Continuar editando" e "Sair sem salvar" |
| R5  | Home: alternância Séries/Filmes por `SegmentedControl`, seções com `Skeleton`, `EmptyState` com convite à ação e `ErrorState` por seção, carrosséis em `FlatList` horizontal, calendário legível (datas por `Intl`, números tabulares) | Must | Given a Home com API fora When abre Then cada seção mostra `ErrorState` com "Tentar novamente" e um toast, sem texto "HTTP" nem tela em branco |
| R6  | Busca: campo `Input` com `inputMode="search"`, termo e tipo refletidos nos params da rota, resultados em `FlatList` com `Pagination`/fim de lista, estados inicial (convite), vazio ("Nenhum título para ..."), carregando e erro | Must | Given a busca "zzzz" sem resultado When enviada Then `EmptyState` cita o termo e sugere outra busca; o termo continua na rota ao voltar |
| R7  | Detalhe do título e episódio: hero com pôster/backdrop e `Poster` com fallback, texto sobre imagem com contraste ≥ 4,5:1 no pior caso (scrim calculado), ações primárias agrupadas, avaliações, comentários e spoiler oculto com revelação explícita, `ConfirmDialog` para excluir comentário/avaliação | Must | Given um backdrop branco simulado When o script de contraste combina o scrim com branco Then a razão do título ≥ 4,5:1; Given comentário com spoiler Then fica oculto até "Mostrar spoiler" |
| R8  | Tracking: progresso da série com `ProgressBar`/tira de episódios, lista em `FlatList`, `Pagination` ou rolagem infinita, ações por episódio com alvo 44, `Skeleton`, `EmptyState` ("Registre um episódio...") e `ErrorState` | Must | Given conta sem registros When abre Then `EmptyState` com ação para buscar; Given 200 registros Then a lista é virtualizada |
| R9  | Listas e minhas avaliações: criação, edição e exclusão com `ConfirmDialog`, itens em `FlatList`, `RatingStars` acessível no lugar dos glifos `★◐☆`, estados dos 3 tipos, aviso de alterações não salvas em edição | Must | Given a exclusão de uma lista When confirmada Then a lista some e o foco volta ao título da tela; `grep -rnE "'★'\|'◐'\|'☆'" src` retorna 0 |
| R10 | Social e perfil de amigo: busca e pedidos com `Chip`, atividades em `FlatList`, remoção de amizade com `ConfirmDialog` (um padrão só), `RefreshControl`, estados vazio ("Convide amigos"), carregando e erro | Must | Given nenhum amigo When abre Social Then `EmptyState` com ação "Buscar pessoas"; remover amizade abre `ConfirmDialog` |
| R11 | Notificações: lista em `FlatList` agrupada por dia, não lida marcada por texto/ícone além da cor, `Switch` de preferências com rótulo, estados dos 3 tipos | Must | Given 0 notificações When abre Then `EmptyState` explica quando aparecem; não lida expõe "não lida" ao leitor de tela |
| R12 | Gamificação: medalhas em grade com bloqueada/conquistada por texto além de opacidade, `ProgressBar` com `progressbar`, diálogo de detalhe, estados dos 3 tipos | Must | Given medalha bloqueada When lida por leitor de tela Then anuncia "bloqueada" e a regra para obtê-la |
| R13 | Perfil e configurações: seções claras, avatar, preferências com `Switch`, créditos TMDB, saída da conta por `ConfirmDialog`, formulário com aviso de sair com alterações | Must | Given alteração não salva When o usuário volta Then aparece o aviso; "Sair da conta" pede confirmação |
| R14 | **3 estados em todas as telas:** `Skeleton`/`LoadingState`, `EmptyState` e `ErrorState` (erro de API também em toast, nunca só inline); zero `ActivityIndicator` cru e zero tela em branco | Must | Given qualquer tela When cada estado é forçado Then há captura em `.docs/design/captures/<tela>/{loading,empty,error}-375.png`; `npm run design:audit` confirma 19/19 |
| R15 | Copy pt-BR revisada: voz ativa, rótulos específicos ("Excluir avaliação", não "Confirmar"), erro com a correção, vazio como convite, sem "HTTP n", nome da ação igual no botão e no feedback | Must | Given o código When `grep -rn "HTTP \${" src` Then 0; revisão de copy por tela anexada ao plano |
| R16 | Movimento contido: **um** momento orquestrado (definido no plano) mais feedback de ação (pressed, toast, diálogo); reduzir movimento respeitado por todos | Must | Given reduzir movimento ligado When o momento orquestrado ocorre Then é estático; `grep -rn "Animated\." src --exclude-dir=ui` retorna 0 |
| R17 | Imagens e listas: pôster e avatar com dimensões explícitas, `alt`/label ou ocultos se decorativos, fallback; carregamento tardio abaixo da dobra; listas potencialmente > 50 itens em `FlatList` com `keyExtractor`; sem leitura de layout na renderização | Must | Given `grep -rn "<ScrollView" src --exclude-dir=ui \| wc -l` Then ≤ 3, cada uma justificada no plano; `<Image` fora de `components/ui` = 0 (`UserAvatar` e `Poster` passam a viver lá) |
| R18 | Acessibilidade e segurança de áreas: toda tela usa `Screen` (safe area), alvos ≥ 44, foco visível não coberto por barras fixas, hierarquia de títulos (um `header` principal por tela), controles só com ícone rotulados, atualização assíncrona anunciada, escala de fonte do sistema testada | Must | Given Android com fonte no máximo When a tela abre Then sem texto cortado nem alvo < 44; `npm run tokens:check` e o checklist por tela passam |
| R19 | Formulários e ações destrutivas: tipo/teclado/autocomplete, botão com loading, foco no primeiro erro, aviso antes de sair com alterações; toda exclusão ou saída usa `ConfirmDialog` (desfazer é Could) | Must | Given `grep -rnE "Alert\.alert\|<TextInput" src --exclude-dir=ui \| wc -l` Then 0 |
| R20 | Wireframe `reelix-visual-wireframes.html` atualizado **por tela** (AS IS e TO BE, estados e fluxos) e capturas antes/depois em 375 e 768 (Expo web) ou dispositivo, em `.docs/design/captures/` | Must | Given cada tela When revisado o PR Then o wireframe reflete a tela nova e existem `antes-375`, `depois-375` e `depois-768` |
| R21 | Auditoria `web-design-guidelines` adaptada, com a lista de regras aplicáveis no plano. Aplicam: rótulo e role em controles, ação = botão e navegação = link, imagem com alt/decorativa, live region, hierarquia, foco, tipo de teclado, autocomplete, colar, erro junto ao campo, loading no envio, aviso de saída, reduzir movimento, só `transform`/`opacity` animados, números tabulares, `Intl`, texto longo/vazio, truncamento, dimensões de imagem, virtualização, estado na rota, confirmação destrutiva, alvo 44, alternativa a gesto, safe area, esquema escuro declarado (`userInterfaceStyle`), hover/pressed/foco evidentes. Não se aplicam (registrar): `transition: all`, `touch-action`, `text-wrap: balance`, `preconnect`, `meta color-scheme`, `beforeunload` do navegador | Must | Given a lista When cada regra é verificada por tela Then 0 achados abertos; os "não se aplica" têm motivo |
| R22 | `DESIGN.md` atualizado: §1 e §5 (direção, ícones), §6 (navegação nova), §7 (movimento), §9 (estados e confirmações), §13 (lacunas fechadas) e este plano linkado | Must | Given o PR When `grep -n "Q4\|0020" .docs/DESIGN.md` Then a decisão e a spec estão citadas e a §13.3 não lista itens resolvidos |

## 5. Fluxos e estados

- **Caminho feliz:** o plano é aprovado; cada tela é refeita sobre `Screen` e primitivos; o PR traz capturas, wireframe e checklist; os gates passam.
- **Vazio:** `EmptyState` com ilustração do kit quando couber, texto de convite e uma ação.
- **Carregando:** `Skeleton` com a forma do conteúdo; `LoadingState` só para ação curta; anúncio `polite`.
- **Erro:** `ErrorState` com a correção e "Tentar novamente", mais toast; erro de sessão pede entrar de novo sem revelar detalhes.
- **Sucesso parcial:** seção que falha não derruba a tela; as demais seguem visíveis.
- **Cancelamento/retry:** `ConfirmDialog` cancela sem efeito; saída com alteração não salva pergunta antes; retry refaz só o bloco que falhou.

## 6. Acceptance Criteria

- [ ] Given `.plans/design-refatoracao-visual-das-telas-plan.md` When aberto Then tem cor, tipo, layout com ASCII, princípios e a tabela default × escolha, commitado antes das telas.
- [ ] Given a Q4 respondida When a navegação é implementada Then os 9 destinos são alcançáveis em até 2 toques.
- [ ] Given `cd mobile && npm run lint && npx tsc --noEmit && npm run tokens:check && npm run design:audit` When rodados Then passam.
- [ ] Given os greps de §10 When rodados Then todas as metas são atingidas.
- [ ] Given as 19 telas When cada estado é forçado Then os 3 estados existem e têm captura.
- [ ] Given Android com fonte grande e leitor de tela When o roteiro de §10 é seguido Then sem texto cortado, alvo < 44 ou controle sem rótulo.
- [ ] Given a lista de R21 When cada regra é checada Then 0 achados abertos.
- [ ] Given o wireframe e `DESIGN.md` When revisados Then refletem as telas finais.

## 7. O que fazer

1. Medir de novo a linha de base de §10 pós-`0019` e capturar as telas "antes" (375 e 768) em `.docs/design/captures/`.
2. Escrever e commitar o plano de design (R1), com a revisão contra o brief; só então seguir.
3. Implementar a navegação (R2) com as cinco abas inferiores (Q4, opção B); mover o que o modelo exigir sem mudar rotas públicas.
4. Refazer as telas na ordem: entrada, Home, busca, detalhe e episódio, tracking, listas, social, notificações, gamificação, perfil e configurações, onboarding.
5. Criar `scripts/audit-screens.mjs` e `npm run design:audit` (manifesto das telas, capturas dos 3 estados, greps de §10).
6. Atualizar o wireframe por tela, `DESIGN.md` e a auditoria de R21; capturar "depois".

## 8. O que não fazer

- Não alterar contrato de API, hooks, `features/*`, rotas públicas ou deep links.
- Não mostrar dados inventados nem módulos não implementados; não gastar ousadia em mais de um elemento memorável.
- Não usar eyebrow em caixa-alta, `→` em botões, gradiente decorativo, entrada fade-slide em toda seção, hover em todo card nem numeração fora de sequência real.
- Não escrever hex, `fontSize`, espaçamento ou dimensão literal; não usar `Alert.alert`, `TextInput` ou `ActivityIndicator` crus.
- Não bloquear colar, nem usar `autoFocus` sem justificativa no plano; não remover foco sem substituto.
- Não declarar auditoria concluída sem evidência por tela; não pular o plano de design.

## 9. Riscos e dependências

| Tipo        | Descrição | Mitigação |
| ----------- | --------- | --------- |
| Dependência | `0019` mergeada (primitivos, tokens, lint) e `0018` (telas finas) | Não iniciar antes; se faltar primitivo, criar seguindo `DESIGN_SYSTEM.md` §4 |
| Dependência | ~~Q4 bloqueia R2~~ resolvida: abas inferiores | Wireframes de navegação do plano seguem a opção B |
| Risco       | Escopo grande (19 telas) | Entregar por PR de tela ou grupo, cada um com capturas e checklist; a ordem de §7 |
| Risco       | Abas inferiores alteram rotas, deep links, `typedRoutes` e destinos de notificação | Manter caminhos públicos; testar `reelix://auth/callback` e as notificações no APK |
| Risco       | Texto sobre imagem varia com o backdrop | Scrim calculado para o pior caso; fallback sem imagem |
| Risco       | Sem testes automatizados antes da `0022` e `0023` | Checklist por tela, capturas, `design:audit` e roteiro manual de `QA.md` |
| Risco       | Contrato de `0019` insuficiente para uma tela | Estender o primitivo em `components/ui` no mesmo PR, atualizando `DESIGN_SYSTEM.md` |

## 10. Resultado

Linha de base medida em 2026-11-19, em `mobile/` (antes da `0018` e da `0019`). Nas células, `\|` é escape de tabela: ao copiar, use `|`.

| Medida | Comando | Hoje | Meta |
| ------ | ------- | ---- | ---- |
| Hex, `fontSize`, `lineHeight`, padding/margin e dimensões literais em `src` fora de `theme` | greps de `0019` §10 | 2 / 1 / 18 / 14 / 85 | 0 em todos |
| `Alert.alert` | `grep -rn "Alert\.alert" src \| wc -l` | 2 | 0 |
| `<TextInput` e `<ActivityIndicator` fora de `ui` | `grep -rnE "<TextInput\|<ActivityIndicator" src --exclude-dir=ui \| wc -l` | 15 e 50 (`ActivityIndicator` em 26 arquivos) | 0 |
| "HTTP n" na interface | `grep -rn "HTTP \${" src \| wc -l` | 1 (`home.tsx:42`) | 0 |
| `ScrollView` / `FlatList` | `grep -rn "<ScrollView" src --exclude-dir=ui \| wc -l`; `grep -rn "<FlatList" src \| wc -l` | 26 / 3 | ≤ 3 justificadas / uma `FlatList` por lista identificada no plano |
| `eyebrow` e `·` em meta strings | `grep -rn "eyebrow" src --exclude-dir=theme \| wc -l`; `grep -rn "·" src \| wc -l` | 22 / 20 | removidos ou justificados no plano |
| Telas com safe area | hoje `grep -rl "SafeAreaView" src/app \| wc -l`; meta: `Screen` em toda tela (direto ou via `AuthScreen`), conferido por `npm run design:audit` | 5 de 19 | 19 de 19 |
| Telas com os 3 estados (capturas) | `npm run design:audit` | script inexistente | 19/19 |
| `Skeleton` / `EmptyState` / `ErrorState` em uso | `grep -rln "EmptyState" src --exclude-dir=ui \| wc -l` | 0 / 0 / 0 | ≥ 1 por tela com dados |
| Pôster com fallback | `<Image` fora de `ui`: `grep -rn "<Image" src --exclude-dir=ui \| wc -l` | 12 | 0 |
| Números tabulares e `Intl` | `grep -rn "fontVariant" src \| wc -l`; `toLocale`/`Intl` repetidos | 0 / 21 | ≥ 1 (primitivo `Text numeric`) / só via `lib/` |
| Aviso de sair com alterações | `grep -rnE "beforeRemove\|usePreventRemove" src \| wc -l` | 0 | ≥ 1 por formulário editável |

- **Gates:** `npm run lint`, `npx tsc --noEmit`, `npm run tokens:check`, `npm run design:audit`, `npm audit --audit-level=high`; backend não muda.
- **Roteiro manual (Android com fonte grande e TalkBack):** percorrer entrada, Home, busca, título, registrar episódio, lista, social, notificação, conquista, perfil e sair; cada passo sem corte, sem alvo < 44 e com leitura coerente.
- **Evidência esperada:** plano de design, capturas `antes`/`depois`, wireframe, checklist de R21, saídas dos gates e das contagens.
- **Comportamento observável:** identidade preservada com hierarquia clara e um traço memorável; nenhum estado em branco; erros dizem o que fazer; movimento mínimo e desligável.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| **Q4 (mapa):** modelo de navegação. **A)** manter drawer + `AppHeader` só na Home: menor custo, mas 9 destinos escondidos e telas internas sem cabeçalho comum. **B)** abas inferiores (recomendado: Início, Buscar, Minha Lista, Amigos, Perfil; sino e avatar no cabeçalho; configurações e conquistas dentro do Perfil): padrão de mobile, descoberta e "compartilhar" visíveis; custo maior (reestruturar `_layout`, grupo de rotas, destinos de notificação, drawer sai). O mood board mostra 4 abas (sem Amigos), mas não é especificação (`DESIGN.md` §1). **DECIDIDA em 2026-11-19 pelo usuário: opção B, cinco abas inferiores (Início, Buscar, Minha Lista, Amigos, Perfil).** | Estrutura de R2, wireframes do plano, fluxos de notificação e deep link | Não (resolvida) | Usuário |
| P1 (Q3): **confirmada em 2026-11-19** — só tema escuro e retrato; sem tablet nem paisagem | Sem tokens por tema; capturas em 768 servem para checar quebra, não para layout | Não | Usuário |
| P2 (Q6): **confirmada em 2026-11-19** — Expo web é ferramenta de desenvolvimento, captura e E2E, não plataforma suportada | Sem otimizações de web nem SEO | Não | Usuário |
| P3: candidatos do plano (a validar em R1): elemento memorável = tira de película do logo como marcador de progresso de episódios; momento orquestrado = entrada do hero do título | Estética; trocáveis no plano sem mudar a spec | Não | Produto/Design |
| P4: remover item de lista/favorito também usa `ConfirmDialog`; "Desfazer" no toast fica como Could | Número de toques em ações frequentes | Não | Produto/Design |
| P5: termo e tipo da busca vão nos params da rota; abas de Home e Social não | Deep links e voltar | Não | Tech lead |
| P6: `design:audit` valida manifesto, capturas e greps; a leitura por TalkBack é manual | Cobertura de acessibilidade até a `0023` | Não | Tech lead |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação em modo lote (`breakdown-specs`) como `rascunho`, com a Q4 registrada como bloqueante, recomendação e opções | Refatoração visual da fase Design; linha de base medida por grep |
| 2026-11-19 | Q4 decidida (abas inferiores, opção B) e P1/P2 confirmadas; status `rascunho` → `pronta` | Respostas do usuário ao quiz de decisões |
