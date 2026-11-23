# 0022 — Testes do mobile: Jest, jest-expo e React Native Testing Library

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** pronta
- **Slug:** testes-mobile-jest-rntl
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-19
- **Owner:** Tech lead (a confirmar)
- **Relacionados:** `.plans/arquitetura-design-testes-breakdown.md` (D6, D8, D9, D10; Q2, Q3); ADR 0008 (TanStack Query e zod) e ADR 0009; `.docs/TESTING.md` §7.3; `.docs/QA.md` §1, §3.1, §5, §6 (dívida 1, REELIX-003); `mobile/AGENTS.md`; depende de `0017` (`http-client`, `ApiError`, query keys, `lib/`), `0018` (`features/*`, hooks, formulários RHF + zod) e `0020` (primitivos finais, via `0019`); paralela de `0021`; `0023` cobre o navegador.

## 1. Escopo

### Dentro do escopo

- Infraestrutura de testes do app: `jest-expo`, Jest e React Native Testing Library (RNTL) em `mobile/`, scripts `npm test` e `npm run test:coverage`, configuração de alias `@/*`, SVG, variáveis `EXPO_PUBLIC_*` fictícias e mocks nativos.
- Utilitário de render com providers de teste, convenções (local, nomes, queries, `testID`) e escolha justificada da estratégia de mock de rede.
- Primeiros testes: `http-client` e `ApiError`, fábricas de query key, `lib/format`, `auth-errors`, schemas zod, hooks de tracking e listas, primitivos de UI, `ToastProvider`, formulários RHF + zod e os fluxos de tela críticos (R8).
- Piso de cobertura medido, passo `npm test` no job `mobile` do CI e atualização de `.docs/TESTING.md`, `.docs/QA.md` §3, `.docs/AGENTS.md` §3 e `mobile/AGENTS.md`.

### Fora do escopo

- Refatorar telas, hooks ou clientes HTTP (`0017`, `0018`) e criar ou redesenhar primitivos (`0019`, `0020`).
- E2E no navegador, axe e fluxo contra API real (`0023`); testes do backend (`0021`).
- Testes nativos em dispositivo (Detox, Maestro), TalkBack/VoiceOver, push, deep link e SecureStore reais: seguem no checklist manual (`QA.md` §5).
- Testes de snapshot visual e regressão de pixel; testes de widgets (`0012`).

## 2. Contexto

O mobile não tem runner nem script de teste (`mobile/package.json`: scripts `start`, `android`, `ios`, `web`, `lint`, `brand:generate`; sem Jest, jest-expo, Testing Library, Playwright, Detox ou Vitest). A verificação é `npm run lint`, `npx tsc --noEmit` e o roteiro manual de `QA.md` §5. O mapa (Q2) manteve a ordem pedida: os testes entram **depois** das refatorações `0017` a `0020`, então a trilha mobile fica protegida só por `tsc`, `lint` e checklist manual até aqui.

Fatos verificados no código: `src/services/supabase.ts` e `src/services/api-url.ts` **lançam erro no import** sem `EXPO_PUBLIC_SUPABASE_URL`, `EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY` e `EXPO_PUBLIC_API_URL`; `api-url.ts` exige HTTPS fora de `__DEV__` (HTTP só para localhost, `127.0.0.1`, `10.0.2.2` e IPv4 privado), e `supabase.ts` aceita HTTP só para `localhost`/`127.0.0.1`. Há 103 `accessibilityLabel`, 164 `accessibilityRole` e **0** `testID`. As telas importam SVG por caminho relativo (`../../assets/brand/...`) e o `tsconfig.json` define `@/*` e `@/assets/*`. Os três clientes usam o `fetch` global. `ToastProvider` fecha o aviso em 5000 ms, com papel `alert` (erro) e botão "Fechar aviso". `mobile/.env` existe localmente e **não** pode influenciar os testes.

Documentação oficial do Expo (consultada em 2026-11-19): `npx expo install jest-expo jest @types/jest --dev`, `preset: "jest-expo"`, `transformIgnorePatterns` recomendado, e para o SDK 58 o `jest-expo` exige Jest 30; com Expo Router, `@testing-library/react-native` 14 ou superior (substitui `react-test-renderer`, sem suporte a React 19). Registro npm em 2026-11-19: `jest-expo` dist-tag `latest` 58.0.15; `@testing-library/react-native` 14.1.0 (peers `react >=19`, `react-native >=0.78`, `test-renderer ^1.0.0`; Node `^22.13.0 || >=24`); MSW 3.0.7 (MIT, Node `>=22.12.0`). As versões a instalar são as que `npx expo install` e `npx expo-doctor` aceitarem para o SDK 58.

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Quem desenvolve e revisa o app; o usuário final, que se beneficia de menos regressão em login, tracking, listas e onboarding |
| Decisão ou ação que muda | Regressões de comportamento são detectadas no PR, não à mão; o `http-client`, os primitivos e os fluxos críticos passam a ter prova automática |
| Resultado esperado       | `npm test` roda no CI sem rede, sem `.env` e sem credencial; refatorações futuras têm rede de segurança |
| Métrica de sucesso       | Linha de base: 0 testes mobile. Meta: todos os itens de R5 a R8 com testes verdes; piso de cobertura medido e ativo no CI; 0 `testID` criado sem necessidade |

## 4. Requisitos

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | Instalar `jest-expo`, `jest`, `@types/jest` e `@testing-library/react-native` como `devDependencies` por `npx expo install`; `jest.config.js` com preset `jest-expo`, `transformIgnorePatterns`, `moduleNameMapper` e `collectCoverageFrom`; scripts `test` (`jest`, sem `--watchAll`), `test:watch` e `test:coverage` (`jest --coverage`) | Must | Given `mobile/` com `npm ci`, When `npm test` roda sem `mobile/.env` e sem rede, Then termina com código 0; `npx expo-doctor` não acusa falha nova; `npm audit --audit-level=high` segue passando |
| R2  | Alias e assets: `@/*` e `@/assets/*` resolvidos como no `tsconfig.json` (o mais específico primeiro); `.svg` resolvido para um componente falso; `tsconfig` inclui os tipos do Jest | Must | Given um teste que importa um módulo com `@/theme/tokens` e um componente que importa `.svg`, When roda, Then passa; `npx tsc --noEmit` e `npm run lint` passam com os arquivos de teste incluídos |
| R3  | Ambiente isolado: `jest.config.js` define `EXPO_NO_DOTENV=1` e `EXPO_PUBLIC_SUPABASE_URL=https://test.invalid`, `EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY` fictícia e `EXPO_PUBLIC_API_URL=http://localhost:5035` antes de qualquer import; `TZ` fixo para testes de data | Must | Given um `mobile/.env` com valores sentinela, When `npm test` roda, Then um teste confirma que `apiUrl` é `http://localhost:5035` e que nenhuma chamada sai para fora (fetch falso falha em URL não registrada) |
| R4  | Setup comum: mocks de `expo-secure-store` (memória), `expo-notifications`, `expo-router` (ou `renderRouter` de `expo-router/testing-library` quando o teste depende de navegação ou `Stack.Protected`, a verificar no SDK 58), `react-native-safe-area-context` e `supabase`; utilitário `renderWithProviders` com `QueryClient` novo por teste (`retry: false`), `ToastProvider` real (para assertar o toast por papel e texto) e sessão controlável por teste | Must | Given `renderWithProviders(<Componente />)`, When o componente usa `useToast` e `useAuthSession`, Then renderiza sem rede nem `supabase-js` real, e o teste liga/desliga sessão |
| R5  | Mock de rede: `fetch` falso com tabela de rotas (método, caminho, resposta), sem dependência nova; falha por padrão em rota não registrada; MSW registrado como alternativa a reavaliar se as rotas crescerem | Must | Given uma rota registrada, When o cliente chama, Then recebe a resposta; Given rota não registrada, Then o teste falha com a URL; `package.json` não ganha biblioteca de mock |
| R6  | Convenções: `*.test.ts(x)` ao lado do código em `src/`, exceto `src/app` (arquivos ali viram rota do Expo Router; testes de tela ficam em `src/__tests__/screens/`); queries por `getByRole`, `getByLabelText`, `getByText`; `testID` só onde não houver nome acessível natural, no padrão `<feature>-<elemento>` em minúsculas; nomes `Sujeito_Comportamento` ou frase de comportamento em pt-BR | Must | Given o repositório, When `find mobile/src/app -name "*.test.*"` roda, Then retorna 0 arquivos; When `grep -rn "testID" mobile/src` roda, Then cada ocorrência tem justificativa em comentário de uma linha no teste que a usa ou em `TESTING.md` |
| R7  | Unitários: `http-client` e `ApiError` (token no `Authorization`, 401 global que encerra a sessão, `ProblemDetails` com `detail`/`title`, corpo não JSON, 204, schema zod inválido, erro de rede); fábricas de query key (estáveis, sem colisão entre módulos); `lib/format` (datas e números pt-BR); `auth-errors` (rede, 429, `weak_password`, `same_password`, códigos de expiração, fallback); schemas de `schemas/auth-forms.ts` | Must | Given cada módulo, When `npm test -- <arquivo>` roda, Then cobre os ramos listados e passa; cada ramo de `getAuthFailureMessage` tem um caso |
| R8  | Componentes e telas: hooks de tracking e listas (consulta, mutação, invalidação por query key, erro como `ApiError`); primitivos Button, Input, EmptyState e ConfirmDialog (papel, rótulo, estado desabilitado/ocupado, alvo mínimo de 44, confirmar e cancelar); `ToastProvider` (aparece, some em 5000 ms com relógio falso, "Fechar aviso", papel `alert`); formulários RHF + zod; fluxos: entrada e cadastro (validação **inline** x falha de API em **toast**), tracking (marcar e desmarcar), listas (criar, editar, excluir com confirmação), onboarding retomável (abre no passo salvo). Os nomes finais vêm das specs `0018` a `0020`; vale o equivalente | Must | Given cada fluxo com `fetch` falso, When o usuário age pela interface (`userEvent`), Then o teste afirma a requisição enviada, o texto em pt-BR e o estado final (vazio, carregando, erro com toast) |
| R9  | Cobertura: `test:coverage` gera `coverage/` (já ignorado pelo Git) e falha abaixo do piso (`coverageThreshold`) igual ao valor medido na implementação, arredondado para baixo, sobre `src/**/*.{ts,tsx}` sem `.d.ts`, `src/types` e os próprios testes; meta (não piso) de 80% de linhas em `src/services`, `src/lib`, hooks e `components/ui` | Must | Given o piso, When um teste é removido e a cobertura cai, Then `npm run test:coverage` retorna código diferente de 0 com a métrica abaixo do piso |
| R10 | CI: o job `mobile` (Node 22) executa `npm run test:coverage -- --ci` depois de `npx tsc --noEmit` e antes do `npm audit`, publica `coverage/` como artefato (7 dias) e mantém o nome do job | Must | Given um PR com teste falhando, When o CI roda, Then o job `mobile` reprova no passo de testes; a versão do Node 22 do runner atende ao mínimo `^22.13.0` do RNTL |
| R11 | Documentação: `TESTING.md` §7.3 vira "como funciona" (comandos, convenções, mocks) e sai de "proposta"; `QA.md` §1, §3 e §3.1 passam a listar `npm test` e a cobertura mobile; `AGENTS.md` §3 troca "não existe `npm test`" pelo comando real; `mobile/AGENTS.md` ganha as convenções de R6 | Must | Given os docs, When `grep -rn "npm test" .docs mobile/AGENTS.md` roda, Then o comando aparece como existente e nenhum doc o chama de futuro |

## 5. Fluxos e estados

- **Caminho feliz:** o desenvolvedor roda `npm test` em `mobile/`, tudo passa em segundos sem rede; o CI repete com cobertura.
- **Vazio:** componentes de lista são testados sem itens (EmptyState com texto e ação em pt-BR).
- **Carregando:** consultas pendentes mostram o estado de carregamento; botões ocupados ficam desabilitados e expõem o estado por acessibilidade.
- **Erro:** falha de API produz toast (papel `alert`), nunca erro inline; validação de campo produz mensagem inline; resposta fora do schema vira erro tratável.
- **Sucesso parcial:** mutação com erro mantém o estado anterior da lista (sem atualização otimista falsa).
- **Cancelamento/retry:** cancelar o `ConfirmDialog` não envia requisição; "Tentar novamente" refaz a consulta e limpa o erro.

## 6. Acceptance Criteria

- [ ] Given `mobile/` sem `.env` e sem rede, When `npm test` roda, Then passa com código 0 (linha de base: 0 testes; meta: todos os de R7 e R8).
- [ ] Given `npm run test:coverage`, When roda, Then gera `coverage/` e respeita o piso registrado em `QA.md` §1.
- [ ] Given o job `mobile` do CI, When roda, Then executa lint, `tsc`, testes com cobertura e audit nessa ordem, e passa.
- [ ] Given um `.env` com valores sentinela, When `npm test` roda, Then os testes usam apenas os valores fictícios de R3.
- [ ] Given `npx tsc --noEmit`, `npm run lint` e `npm audit --audit-level=high`, Then todos passam com os arquivos de teste incluídos.
- [ ] Given o fluxo de listas, When o usuário exclui uma lista e cancela a confirmação, Then nenhuma requisição `DELETE` é enviada; ao confirmar, é enviada uma vez.
- [ ] Given um erro 401 no `http-client`, When uma consulta falha, Then a sessão é encerrada uma vez e o usuário volta à entrada.

## 7. O que fazer

1. Rodar a linha de base: `npm ci`, `npm run lint`, `npx tsc --noEmit`, `npm audit --audit-level=high` (a última execução registrada é de 2026-11-04) e registrar em `QA.md` §1.1.
2. Instalar as dependências por `npx expo install` e conferir `npx expo-doctor`.
3. Criar `jest.config.js`, o setup, os mocks e `renderWithProviders` (R1 a R5).
4. Escrever os testes de R7 e R8 em fatias verticais (skill `tdd`): um comportamento por teste, no seam público (função, hook, componente, tela).
5. Medir a cobertura, fixar o piso (R9) e ligar o CI (R10).
6. Atualizar a documentação (R11) e criar `testID` apenas onde a consulta por papel ou rótulo não bastar (R6).

## 8. O que não fazer

- Não testar detalhes de implementação (estado interno, estilos privados, número de renders); não usar snapshot como prova.
- Não usar `.env` real, URL real nem credencial; não deixar `console.error` de teste sem tratar.
- Não criar `testID` por conveniência quando houver `accessibilityLabel` ou papel.
- Não colocar arquivos de teste em `src/app`; não alterar `app.json`, `babel`/`metro` de produção para acomodar teste sem justificar.
- Não rodar `npm audit fix --force` (REELIX-019) nem adicionar dependência fora de R1.

## 9. Riscos e dependências

| Tipo        | Descrição | Mitigação |
| ----------- | --------- | --------- |
| Dependência | Nomes e caminhos finais de `http-client`, hooks e primitivos vêm de `0017` a `0020` | Spec escrita por comportamento; a implementação mapeia para os nomes finais |
| Risco       | Versões: Jest 30 e RNTL 14 com SDK 58, `test-renderer` como peer | Instalar só por `npx expo install`; validar com `npx expo-doctor` e CI antes de fixar |
| Risco       | `.env` local vaza para o bundle de teste (o `@expo/env` instalado só não sobrescreve variável já definida) | `EXPO_NO_DOTENV=1` e valores definidos em `jest.config.js`; teste sentinela (R3) |
| Risco       | `expo-router/testing-library`, `Animated` e `react-native-reanimated` em Jest exigem mocks | Mock mínimo no setup; relógio falso nos testes de toast |
| Risco       | Testes frágeis por texto ou estilo | Consultas por papel/rótulo; asserção de comportamento |
| Risco       | Novos `devDependencies` trazem advisories | `npm audit --audit-level=high` como critério; sem `--force` |
| Risco       | Tempo do job `mobile` | Cobertura e testes em um passo só; medir e registrar a duração |

## 10. Resultado

Como validar que a spec foi atendida:

- **Comando/verificação:** em `mobile/`: `npm ci`, `npm test`, `npm run test:coverage`, `npm run lint`, `npx tsc --noEmit`, `npm audit --audit-level=high`, `npx expo-doctor`.
- **Evidência esperada:** linha de base de hoje: 0 testes mobile (202 xUnit no backend; 0 E2E; 0 de arquitetura). Meta: testes de R7 e R8 verdes, relatório de cobertura gerado, piso medido e registrado, job `mobile` com passo de testes verde no PR.
- **Comportamento observável:** quebrar o `http-client`, um primitivo ou um fluxo crítico faz o PR reprovar; o desenvolvedor roda os testes localmente sem configurar nada.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| Premissa: `fetch` falso sem dependência (o app usa o `fetch` global); MSW 3.0.7 (Node `>=22.12`) só se a compatibilidade com o ambiente RN do Jest for provada | Mock de rede | Não | Tech lead |
| Premissa: testes de tela em `src/__tests__/screens/` porque `src/app` vira rota; a confirmar no SDK 58 | Estrutura | Não | Tech lead |
| `renderRouter` (`expo-router/testing-library`) x mock de `useRouter` | Fidelidade dos testes de navegação | Não | Tech lead |
| Premissa: `ToastProvider` real no render de teste (assertar por papel `alert`); fake só quando o teste não depende do aviso | Convenção | Não | Tech lead |
| Premissa: piso = medição da implementação, arredondada para baixo | Gate | Não | Tech lead |
| Premissa (Q2): testes depois das refatorações; sem testes de caracterização do código antigo | Risco de regressão em `0017`/`0018` | Não | Usuário |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação da spec (modo lote do `breakdown-specs`); versões conferidas no registro npm e na doc do Expo | Camada de testes do mobile do épico `arquitetura-design-testes` |
