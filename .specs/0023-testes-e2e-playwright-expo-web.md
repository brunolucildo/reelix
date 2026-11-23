# 0023 — Testes E2E com Playwright sobre o Expo web

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** pronta
- **Slug:** testes-e2e-playwright-expo-web
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-19
- **Owner:** Tech lead (a confirmar)
- **Relacionados:** `.plans/arquitetura-design-testes-breakdown.md` (D8, D9, D10; Q5, Q6); ADR 0003 (JWT Supabase), ADR 0006 (push e OpenAI desativados), ADR 0009; `.docs/TESTING.md` §7.4 (hipótese e riscos 1 a 8, validados aqui); `.docs/QA.md` §1, §3.1, §5, §6 (dívidas 2, 9 e 16); depende de `0020` (telas e primitivos finais, com `0018` e `0019`) e de `0021` (host de teste R13 e stub de catálogo R14); complementa `0022` (unidade e componente).

## 1. Escopo

### Dentro do escopo

- `@playwright/test` em `e2e/` na raiz, com `package.json` próprio, `playwright.config.ts` e `webServer` para a API de teste (host da `0021`) e para o Expo web.
- Autenticação por sessão injetada: JWT HS256 assinado com o segredo de teste e sessão em `localStorage['reelix-auth-session']`, com a premissa de não-refresh verificada (R3).
- Usuários fictícios únicos por teste, catálogo pelo stub da `0021`, seletores por papel e rótulo (`testID` só onde faltar).
- Fluxos de R5 a R10, verificação de acessibilidade com `@axe-core/playwright` por tela principal e capturas de tela como artefato.
- Job `e2e` no CI (Docker, cache dos browsers, artefatos, tempo-limite, `retries` no máximo 1, sem `sleep`) e atualização de `.docs/TESTING.md`, `.docs/QA.md` §3 e `.docs/AGENTS.md`.

### Fora do escopo

- Backend e host de teste (`0021`); testes de unidade e componente (`0022`); refatoração visual (`0019`, `0020`).
- Tudo que é nativo: push, `SecureStore`, deep link (`reelix://auth/callback`), TalkBack, build de release. Seguem no QA manual no dispositivo Android (`QA.md` §5.5).
- Login e cadastro contra um Supabase real; regressão visual por comparação de pixels; matriz de dispositivos; tornar o Expo web plataforma do produto (premissa Q6: **web só como ambiente de teste**).
- Alterar `app.json`, `web.output` ou o comportamento do app para acomodar o teste, salvo o ajuste mínimo previsto na pergunta da §11.

## 2. Contexto

Não há E2E: Playwright não está instalado, e a jornada app + API + banco só é exercitada à mão (`QA.md` dívida 2). O Expo web existe (`react-native-web ~0.21.4`, script `npm run web`), e `accessibilityLabel`/`accessibilityRole` (103 e 164 usos; 0 `testID`) viram `aria-label`/`role`, o que permite seletores semânticos. A skill `playwright` do repositório é CLI-first (`playwright-cli`) e proíbe `@playwright/test` sem pedido explícito; aqui o pedido é explícito, e a skill continua útil para exploração e depuração.

Validado no código em 2026-11-19 (resolve os riscos de `TESTING.md` §7.4):

- **Sessão injetada:** `src/services/supabase.ts` usa `storageKey: 'reelix-auth-session'` e, no web, `localStorage`. Em `@supabase/auth-js` 2.118.0 (`GoTrueClient.js`), uma sessão é válida se tiver `access_token`, `refresh_token` e `expires_at`; é renovada pela rede se `expires_at * 1000 - Date.now()` for menor que `EXPIRY_MARGIN_MS` (90 s, 3 x 30 s); se faltar `user`, o cliente chama `_getUser` (rede). Logo a sessão injetada precisa de `user`, de `refresh_token` qualquer e de `expires_at` bem no futuro.
- **Logout fala com a rede:** `signOut({ scope: 'local' })` chama `admin.signOut` em `{EXPO_PUBLIC_SUPABASE_URL}/auth/v1/logout` (erros 401/403/404 são ignorados). O E2E intercepta essa chamada no navegador.
- **`.env` não vaza:** em `@expo/env` instalado, variável já definida no ambiente **não** é sobrescrita pelo `.env`; `EXPO_NO_DOTENV` desativa a leitura.
- **Modo de execução:** `api-url.ts` rejeita HTTP quando `__DEV__` é falso. Logo o E2E usa `expo start --web` (modo de desenvolvimento), não `expo export`; `supabase.ts` aceita HTTP só em `localhost`/`127.0.0.1`.
- **CORS:** `appsettings.Development.json` libera `localhost`/`127.0.0.1` nas portas 8081 e 8087; a API de teste usa porta própria (a 5035 é a do desenvolvedor).
- **Alert:** `Alert.alert` ainda existe em `src/app/tracking.tsx:190` e `src/components/reviews/rating-review-section.tsx:185` (no-op no RN Web); a `0018` o substitui por diálogo próprio.
- **Logout no web:** `AuthProvider.signOut` chama `revokeRegisteredPushDevice`, que lê `SecureStore`; o comportamento no web não foi verificado (pergunta da §11).

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Quem desenvolve e revisa; o usuário final, via jornadas críticas protegidas |
| Decisão ou ação que muda | A jornada completa (app + API + PostgreSQL) passa a ser provada no PR, e a acessibilidade ganha uma verificação automática por tela |
| Resultado esperado       | `npm run test:e2e` sobe banco, API e Expo web de teste, executa as jornadas e produz relatório, traces e capturas |
| Métrica de sucesso       | Linha de base: 0 E2E (202 xUnit, 0 mobile, 0 arquitetura). Meta: jornadas de R5 a R9 verdes; 0 violação axe serious/critical nas telas de R10; flakiness medida em 20 execuções seguidas |

## 4. Requisitos

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | `@playwright/test` em `e2e/` (raiz) com `package.json` e lockfile próprios; scripts `test:e2e`, `test:e2e:ui`, `test:e2e:report`, `install:browsers`. Justificativa: isola versão, `npm audit` e `expo-doctor` do app (o job `mobile` não instala Playwright), o Metro não enxerga os arquivos e o job `e2e` cacheia à parte | Must | Given `e2e/` com `npm ci`, When `npm run test:e2e` roda, Then sobe os servidores, executa a suíte e termina com código 0; `mobile/package.json` não ganha dependência |
| R2  | `playwright.config.ts` com `webServer` em array: (1) `dotnet run --project backend/Reelix.E2EHost --urls http://127.0.0.1:5135`, pronto em `GET /e2e/ready`; (2) `npx expo start --web --port 8087` em `mobile/`, com `EXPO_NO_DOTENV=1`, `EXPO_PUBLIC_SUPABASE_URL=http://127.0.0.1:5135`, `EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY` fictícia e `EXPO_PUBLIC_API_URL=http://localhost:5135`; origem `http://localhost:8087` (já liberada); `reuseExistingServer` só fora do CI; porta da API configurável por `E2E_API_PORT`; um único `Supabase__JwtSecret` aleatório por execução (`process.env.E2E_JWT_SECRET ??= ...`, pois a configuração é carregada também nos workers) | Must | Given nenhuma porta ocupada, When a suíte inicia, Then a API de teste (nunca a 5035) e o Expo web respondem; um `mobile/.env` com valores reais não altera as URLs do bundle |
| R3  | Fixture de autenticação: assina JWT HS256 com `node:crypto` (`iss = {Supabase__Url}/auth/v1`, `aud = authenticated`, `sub` = GUID do teste) e injeta por `addInitScript` a sessão (`access_token`, `refresh_token`, `expires_at` a 24 h, `token_type`, `user`) em `localStorage['reelix-auth-session']`; a premissa "sessão aceita sem refresh" é um teste: nenhuma requisição a `/auth/v1/token` nem a `/auth/v1/user` durante a Home | Must | Given um usuário novo, When a Home abre, Then exibe a Home autenticada e o contador de chamadas a `**/auth/v1/**` é 0; Given o segredo trocado, When a sessão é injetada, Then a API responde 401 e o app volta à entrada |
| R4  | Dados: cada teste cria usuário único (`sub` novo, e-mail `@example.invalid`); a conta nasce na primeira chamada autenticada; catálogo vem do stub da `0021` (ids `900001`, `900002`, série `900101`); nenhuma credencial, e-mail ou dado real; nenhum teste depende de ordem | Must | Given dois testes em paralelo, When rodam, Then não compartilham `sub` e passam em qualquer ordem (`--shard`/`--workers` diferentes) |
| R5  | Jornada: sessão restaurada abre a Home; busca "e2e" lista os 3 títulos; abrir o detalhe de um filme; marcar como assistido; o título aparece no histórico de tracking | Must | Given o usuário autenticado, When busca, abre o detalhe e marca como assistido, Then a tela de tracking lista o filme e a API responde com o registro (verificado pela UI, não por SQL) |
| R6  | Jornada: favoritar, adicionar à watchlist, criar uma lista e adicionar o título; cada coleção mostra o item; remover desfaz | Must | Given o detalhe de um título, When favorita, adiciona à watchlist e à lista criada, Then as três telas exibem o título e, ao remover, ele some |
| R7  | Jornada: perfil e privacidade: editar nome e biografia, alternar perfil público/privado; perfil de outra conta privado não é exibido | Must | Given dois usuários, When A torna o perfil privado, Then B, ao abrir o perfil de A, recebe a mensagem de indisponibilidade e não vê os dados |
| R8  | Jornada: logout e rota protegida: sair da conta volta à entrada; acesso direto a `/home` sem sessão não mostra conteúdo autenticado; a chamada `/auth/v1/logout` é interceptada no navegador (204) e qualquer outra chamada a `/auth/v1/**` reprova o teste | Must | Given o usuário na Home, When aciona "Sair da conta", Then a entrada aparece, `localStorage['reelix-auth-session']` some e `/home` não exibe a Home |
| R9  | Estados de erro e isolamento: com `page.route` abortando `**/api/v1/**`, a Home mostra toast (papel `alert`) e "Tentar novamente", que refaz a consulta ao restabelecer a rota; usuário B não vê histórico, listas nem favoritos de A | Must | Given a API indisponível, When a Home carrega, Then o toast e "Tentar novamente" aparecem e, ao liberar a rota e clicar, a Home carrega; Given A com dados, When B acessa as mesmas telas, Then estão vazias |
| R10 | Acessibilidade: `@axe-core/playwright` nas telas principais (Home, busca, detalhe, tracking, listas, perfil, configurações, social, notificações, conquistas) sem violação `serious` ou `critical`; exceções só em lista nomeada (regra, tela, motivo, spec responsável); captura de cada tela anexada ao relatório | Must | Given cada tela, When o axe roda, Then 0 violações serious/critical fora da lista; o relatório HTML contém as capturas |
| R11 | Login pela tela contra Supabase simulado no navegador (`page.route` devolvendo sessão assinada); cadastro e recuperação de senha ficam fora | Should | Given o formulário de entrada, When envia e-mail e senha fictícios, Then o app entra na Home; com resposta 400 mostra o toast de erro |
| R12 | Estabilidade: `retries` 0 local e no máximo 1 no CI; sem `waitForTimeout`; esperas só por asserção com auto-espera; `trace` e `video` retidos em falha; `workers` inicial 2 no CI, a medir; aquecimento do bundle do Metro no `globalSetup` | Must | Given a suíte, When `grep -rn "waitForTimeout" e2e --include=*.ts` roda, Then 0 ocorrências; Given o cache do Metro limpo (`--clear`), When a suíte roda, Then não há falha por tempo-limite de partida |
| R13 | Job `e2e` no CI (`ubuntu-latest`, Docker disponível): setup de .NET 10 e Node 22, `npm ci` em `mobile/` e `e2e/`, cache de `~/.cache/ms-playwright` por versão do pacote, `npx playwright install --with-deps chromium`, `timeout-minutes` 20, artefatos `playwright-report` e `test-results` (`if: always()`, 7 dias); os jobs `backend` e `mobile` mantêm o nome | Must | Given um PR, When o CI roda, Then o job `e2e` executa a suíte e publica os artefatos mesmo na falha; a segunda execução reutiliza o cache dos browsers |
| R14 | Documentação: `TESTING.md` §7.4 vira "como funciona" (com os riscos resolvidos), `QA.md` §1, §3, §3.1 e dívidas 2 e 16, `AGENTS.md` §3 com `npm run test:e2e` e a pasta `e2e/`, `DEVELOPMENT.md` §9 e `ARCHITECTURE.md` §3 | Must | Given os docs, When `grep -rn "test:e2e" .docs` roda, Then o comando aparece como existente com o caminho `e2e/` |

## 5. Fluxos e estados

- **Caminho feliz:** `npm run test:e2e` sobe o host (contêiner PostgreSQL, API) e o Expo web; cada teste injeta uma sessão, navega por papel e rótulo, afirma pela interface e, ao fim, o contêiner é descartado.
- **Vazio:** usuário novo vê Home, histórico, listas e favoritos vazios com o texto de estado vazio em pt-BR.
- **Carregando:** os testes esperam o estado final por asserção; nunca por tempo fixo.
- **Erro:** API abortada produz toast e "Tentar novamente"; porta ocupada, Docker ausente ou host sem `/e2e/ready` reprovam a partida com mensagem clara.
- **Sucesso parcial:** falha de uma tela no axe não impede o relatório das demais; `retries` não esconde falha (o trace da primeira tentativa é retido).
- **Cancelamento/retry:** SIGINT derruba Expo, API e contêiner; reexecução parte de banco novo.

## 6. Acceptance Criteria

- [ ] Given `e2e/` e Docker ativo, When `npm run test:e2e` roda, Then API de teste, banco e Expo web sobem e as jornadas R5 a R9 passam (linha de base: 0 E2E).
- [ ] Given a sessão injetada, When a Home abre, Then 0 requisições a `/auth/v1/token` e `/auth/v1/user`.
- [ ] Given `mobile/.env` com valores reais, When a suíte roda, Then nenhuma requisição sai para o Supabase real nem para a 5035.
- [ ] Given usuários A e B, When B acessa as telas de A, Then não vê dados de A.
- [ ] Given cada tela de R10, When o axe roda, Then 0 violações serious/critical fora da lista nomeada.
- [ ] Given o CI, When uma jornada falha, Then trace, vídeo e relatório estão no artefato; `grep -rn "waitForTimeout" e2e` retorna 0.
- [ ] Given 20 execuções seguidas no CI, When medidas, Then a taxa de falha por flakiness é 0 com no máximo 1 retry (registrar o resultado em `QA.md`).

## 7. O que fazer

1. Confirmar `0020` e `0021` integradas (host `Reelix.E2EHost`, `/e2e/ready`, stub de catálogo) e a linha de base (`QA.md` §1.1).
2. Criar `e2e/` com Playwright, `playwright.config.ts` e a fixture de autenticação (R1 a R4); verificar a premissa de não-refresh e `expo start --web` com `web.output: "server"` (renderização no servidor sem `localStorage`).
3. Escrever as jornadas em fatias, uma por vez (skill `tdd`; seams: os fluxos de R5 a R9), com seletores por papel e rótulo e `testID` apenas onde faltar.
4. Adicionar o axe e as capturas (R10) e o login simulado (R11).
5. Criar o job `e2e` e medir tempo e flakiness (R12, R13).
6. Atualizar documentação (R14).

## 8. O que não fazer

- Não rodar contra o Supabase compartilhado, contra a API de desenvolvimento (5035) ou com a connection string dos User Secrets.
- Não habilitar push remoto nem OpenAI; não usar TMDB ou OMDb reais.
- Não usar `waitForTimeout`, seletor por CSS ou posição quando houver papel, rótulo ou texto.
- Não criar `testID` quando houver nome acessível; não mudar `app.json` ou o app para o teste passar.
- Não commitar `playwright-report/`, `test-results/` nem o segredo JWT (já ignorados no `.gitignore` os dois primeiros).
- Não ampliar o escopo para nativo: o que depende de Android real fica no checklist manual.

## 9. Riscos e dependências

| Tipo        | Descrição | Mitigação |
| ----------- | --------- | --------- |
| Dependência | `0020` (telas finais) e `0021` (host e stub de catálogo) | Só iniciar após ambas; ids do stub registrados na `0021` (R14) |
| Risco       | Flakiness: primeira compilação do bundle do Metro, animações, rate limit (limites por `sub`) | Aquecimento no `globalSetup`, esperas por asserção, usuários únicos, `retries` no máximo 1, medição em 20 execuções |
| Risco       | Docker e tempo do CI (contêiner, `dotnet run`, Metro, browsers) | Cache dos browsers, `timeout-minutes` 20, job separado e não bloqueante até estabilizar |
| Risco       | `web.output: "server"` e `localStorage` ausente na renderização no servidor | Verificar no passo 2; se falhar, abrir decisão (Q6) em vez de mudar `app.json` |
| Risco       | `Alert.alert` no-op no web, se a `0018` não o substituir | Fluxo afetado fica fora do E2E e registrado em `QA.md` |
| Risco       | `@axe-core/playwright` 4.14.0 é MPL-2.0 (registro npm em 2026-11-19) | Dependência de desenvolvimento em `e2e/`, não vai ao bundle; registrar a licença no relatório |
| Risco       | Segredo JWT de teste vazar em log | Gerado por execução, nunca impresso nem commitado |
| Risco       | Versões: Playwright 1.65.1 (Node `>=20`) e Expo SDK 58 | Fixar pelo lockfile; validar no CI antes de tornar o job obrigatório |

## 10. Resultado

Como validar que a spec foi atendida:

- **Comando/verificação:** em `e2e/`: `npm ci`, `npx playwright install chromium`, `npm run test:e2e`, `npm run test:e2e:report`; CI: job `e2e` verde com artefatos.
- **Evidência esperada:** linha de base de hoje: 0 E2E, 0 testes mobile, 202 xUnit. Meta: jornadas R5 a R9 verdes, axe sem serious/critical nas telas de R10, flakiness medida em 20 execuções e registrada em `QA.md` §1.
- **Comportamento observável:** um PR que quebra busca, tracking, listas, perfil, logout ou a acessibilidade principal reprova o job `e2e`; o relatório traz trace e capturas.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| Premissa (Q6): o Expo web é só ambiente de teste; nada de layout ou suporte de produto para web | Escopo | Não | Usuário |
| Logout no web: `revokeRegisteredPushDevice` lê `SecureStore`, não verificado no web; se falhar, um ajuste mínimo `Platform.OS === 'web'` é aceito nesta spec como correção de testabilidade, caso contrário o fluxo reprova | R8 | Não | Tech lead |
| Premissa: viewport 390 x 844 (app retrato, Q3); só Chromium | Cobertura de navegadores | Não | Tech lead |
| Tornar o job `e2e` check obrigatório de `main` depois de estabilizar (configuração fora do Git; hoje só `backend` e `mobile`) | Gate | Não | Usuário |
| Premissa: sessão injetada com `user`, `expires_at` +24 h e `refresh_token` fictício é aceita sem rede (verificado no código de `auth-js` 2.118.0; confirmar pelo teste de R3) | R3 | Não | Tech lead |
| `expo start --web` com `web.output: "server"` funciona com `localStorage` (a verificar no passo 2) | Viabilidade | Não (há plano em §9) | Tech lead |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação da spec (modo lote do `breakdown-specs`); riscos 1, 3, 4, 5 e 8 de `TESTING.md` §7.4 verificados no código | Camada E2E do épico `arquitetura-design-testes` |
