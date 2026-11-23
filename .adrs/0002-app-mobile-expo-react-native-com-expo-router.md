# 0002 — App mobile com Expo, React Native e Expo Router

- **Status:** aceito
- **Data:** 2026-10-26 (aproximada, pelo histórico git)
- **Origem:** registro retroativo de decisão já implementada

## Contexto

O Reelix é um app de acompanhamento de filmes e séries com foco mobile (`.docs/ARCHITECTURE.md` §2 e §3). A equipe é pequena e precisa de build nativo, deep links (callbacks de autenticação e de notificações), notificações push e armazenamento seguro de sessão sem manter projetos nativos à mão. `.docs/ARCHITECTURE.md` §3 deixa em aberto o estilo (NativeWind ou `StyleSheet`) e o lint (Biome ou ESLint).

## Decisão

- App em **Expo SDK 58** (`expo ~58.0.23`), React Native 0.88.1, React 19.3.0 e TypeScript com `strict: true` (`mobile/tsconfig.json`).
- Navegação por **Expo Router** (`~58.0.30`), com rotas em `mobile/src/app/`, `typedRoutes` ligado em `app.json` e scheme `reelix` para deep links (`reelix://auth/callback`).
- Estilo com `StyleSheet` e tokens compartilhados em `mobile/src/theme/tokens.ts`, derivados do brand kit; NativeWind não foi adotado.
- Gerenciador de pacotes **npm** (`package-lock.json`); lint com ESLint via `expo lint` (`eslint-config-expo`). Biome e Bun não foram adotados.
- Builds nativos por **EAS** (`mobile/eas.json`: `development` com `developmentClient`, `preview` e `production`), com `expo-dev-client`. Expo Go não basta para push e widgets (ADR 0006 e 0010).
- A versão web (`react-native-web`, script `web`) existe, mas é superfície secundária (inferido: o alvo declarado é iOS/Android e a web só aparece em CORS de desenvolvimento).

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| React Native sem Expo (CLI) (inferido) | Controle total do projeto nativo | Manter Gradle/Xcode e configuração nativa à mão | Inferido: Expo cobre as necessidades e o EAS entrega os builds |
| Flutter ou nativo por plataforma (inferido) | Desempenho e UI nativos | Outra linguagem e duas bases de código | Inferido: `architecture.md` §2 já fixa React Native + TypeScript |
| NativeWind | Fluxo parecido com Tailwind | Dependência a mais | `architecture.md` §3 descreve `StyleSheet` como a opção de menor dependência |
| Biome no lugar de ESLint | Uma ferramenta só para lint e formato | Compatibilidade com Expo precisa ser confirmada | `architecture.md` §3 manda usar ESLint se faltar regra ou plugin; o motivo concreto da escolha não está registrado (inferido) |
| Bun como gerenciador | Mais rápido | Segundo lockfile ao lado do npm | `technology-stack-evaluation.md`: não adicionar |

## Consequências

- Positivas: um código só para iOS e Android; deep links, SecureStore e notificações resolvidos por módulos Expo; builds nativos pelo EAS, sem toolchain nativa local.
- Negativas: o app acompanha o ciclo de SDKs do Expo; push e widgets exigem development build e dispositivo físico; não há runner de testes mobile (`.docs/QA.md`); `npm audit` reporta 21 advisories moderados/baixos em dependências transitivas, e a correção forçada propõe downgrades incompatíveis (não aplicada); iOS ainda não tem `bundleIdentifier` configurado (ADR 0010).
- Impacto em código, operação e time: o código mobile vive em `mobile/src/` (rotas em `app/`, serviços em `services/`, tema em `theme/`); o CI roda `npm run lint` e `npx tsc --noEmit` no job `mobile` (`.github/workflows/ci.yml`). Telas novas seguem os padrões de UX de `.docs/ARCHITECTURE.md` §3 (toasts, tokens, 44 px de toque).
