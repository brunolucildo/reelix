# QA

> **Público:** quem vai validar uma mudança antes de declará-la pronta.
> **Regra de ouro:** só reporte o que você executou e viu. Não existe "deve passar".
> **Verificado contra o código em:** 2026-11-19, commit `1baace9` (`.github/workflows/ci.yml`, `mobile/package.json`, `mobile/eslint.config.js`, `backend/Reelix.UnitTests/`, `.specs/0001-authentication.md`, `.specs/0003-catalog.md`, `README.md`).
> **Leitura relacionada:** `TESTING.md` (como os testes funcionam e a estratégia-alvo), `DESIGN.md` (UX e padrões de tela), `../.specs/0001-authentication.md` (anexo de configuração: Supabase, variáveis e deep links), `../.specs/0003-catalog.md` (anexo de catálogo: provedores e Swagger).

## 1. Estado atual

| Recurso | Estado |
| --- | --- |
| Lint backend / formatação .NET | **Ausentes:** não há `.editorconfig`, `dotnet format` nem analisadores configurados no repositório; o build usa só os avisos padrão do compilador |
| Lint mobile (ESLint, `eslint-config-expo` flat, via `expo lint`) | Configurado; passa (último registro: 2026-11-04) |
| Formatação mobile | Ausente (sem Prettier nem equivalente em `devDependencies`) |
| Tipos | Backend: C# com `Nullable` habilitado. Mobile: `tsc --noEmit` com `strict: true`; passa (último registro: 2026-11-04) |
| Build | Backend: `dotnet build` / `dotnet test` em Release; passa (2026-11-19). Mobile: não há script de build; o bundle Android foi exportado com sucesso em 2026-11-05; o APK EAS de development concluiu |
| Testes backend | xUnit 2.9.3 em `backend/Reelix.UnitTests`: **339 testes, 339 aprovados** (2026-11-23, integração das specs `0013`/`0017`; eram 202 em 2026-11-19). 13 deles exigem Docker (`PostgresAuthorizationIntegrationTests` e `UserAccountRepositoryPostgresTests`, `postgres:17-alpine`); o restante roda sem Docker |
| Testes mobile | **Ausentes.** Não há runner (nem jest, jest-expo, Testing Library, Playwright, Detox ou Vitest) nem script de teste em `mobile/package.json` |
| Testes E2E | **Ausentes** |
| Testes de arquitetura | **Ausentes** (ver `TESTING.md` §7) |
| Cobertura | **Não medida.** `coverlet.collector` está referenciado, mas nenhum comando ou etapa de CI o usa |
| CI | `.github/workflows/ci.yml` (`pull_request`, `push`, `workflow_dispatch`): job `backend` (Gitleaks, restore com NU1903/NU1904 como erro, `dotnet test` em Release com TRX, script idempotente de migrations como artefato, audit NuGet informativo) e job `mobile` (Node 22, `npm ci`, lint, `tsc`, `npm audit --audit-level=high`). `main` exige PR e os checks `backend` e `mobile` |
| Acessibilidade | Só manual e **parcial**: há 103 `accessibilityLabel` e 164 `accessibilityRole` no código e 0 `testID`; nenhuma verificação com TalkBack/VoiceOver, contraste, Dynamic Type ou validação multiplataforma foi registrada (REELIX-015) |

> **Importante:** `lint` e `tsc` **não são testes de comportamento**. Para o mobile, a única verificação funcional hoje é a manual (§5).

### 1.1 Baseline verificado em 2026-11-19

Backend, na raiz do repositório, com Docker em execução (daemon ativo, versão 29.9.3):

| Gate | Comando | Resultado | Observação |
| --- | --- | --- | --- |
| Testes backend | `dotnet test Reelix.sln --configuration Release --no-restore` | Passa | 339 testes, 339 aprovados, 0 falhos, 0 ignorados; ~7 s de teste (2026-11-23; em 2026-11-19: 202 testes, ~21 s com build) |

Ambiente da máquina nessa data: SDKs .NET `10.0.404` e `11.0.100` (não há `global.json`, então o `dotnet` usa por padrão o SDK mais recente instalado; a CI fixa `10.0.x`; confirme com `dotnet --version` e registre), Node 24.20, npm 12.2.1, Docker 29.9.3. Playwright não está instalado. Aviso `NU1900` local por um feed privado do Azure DevOps na configuração NuGet da máquina; não afeta a CI, que restaura só de `nuget.org`.

**Não reexecutados em 2026-11-19** (último registro conhecido; ver §1.2): `npm run lint`, `npx tsc --noEmit` e `npm audit --audit-level=high` (todos em 2026-11-04). Reexecute-os antes de usar este baseline como referência de uma mudança no mobile.

### 1.2 Registros de validação anteriores

Registros migrados dos antigos `PROJECT_STATUS.md`, `ROADMAP.md`, `DEVELOPMENT_BACKLOG.md` e do `README.md` (os três primeiros foram removidos em 2026-11-19; este registro preserva o que é evidência de verificação e o histórico do Git guarda o texto original). Datas: a fotografia do código é de 2026-11-04; os itens do Samsung vão até 2026-11-05. Quando a fonte não registrou a data exata, está indicado. Nenhum segredo, token ou connection string é reproduzido.

| Data | Verificação | Resultado | Limite |
| --- | --- | --- | --- |
| 2026-11-04 | `dotnet test Reelix.sln --configuration Release` (SDK .NET 11 preview executando projetos `net10.0`) | 202 aprovados, 0 falhos, 0 ignorados; inclui 10 cenários HTTP com PostgreSQL efêmero; passou **sem** variáveis Supabase/PostgreSQL externas; build com 6 avisos | A CI fixa .NET 10 |
| 2026-11-04 | `npm run lint` e `npx tsc --noEmit` em `mobile/` | Passam | Não substituem teste funcional |
| 2026-11-04 | `npm audit --audit-level=high` | **21 advisories** (3 low, 18 moderate), **sem high nem critical**; passa no limiar configurado. Achados em `deepmerge`, `glob` e `minimatch` (dependências transitivas da toolchain Expo/ESLint e do transformer SVG) | `npm audit fix --force` propõe downgrades incompatíveis de Expo/ESLint/transformer e **não foi aplicado**; REELIX-019, bloqueado por release upstream compatível |
| 2026-11-04 | Audit NuGet via nuget.org | Nenhuma vulnerabilidade | O comando não falha o job da CI |
| 2026-11-04 | Gitleaks v8.30.1 no histórico local | 77 commits, **0 findings**; o job de secret scanning da CI também passou | Só histórico; não varre segredo futuro |
| 2026-11-04 | Histórico EF do banco de `Development` (consulta somente leitura, `dotnet-ef migrations list`) | **10 migrations**, até `20261103134321_ExpandFixedAvatars`, sem pendências; a cadeia completa também é aplicada em PostgreSQL 17 descartável pelos testes | **Só existe o ambiente `Development`.** Novo ambiente exige repetir inventário, aplicação e smoke test (REELIX-001) |
| 2026-11-04 | Banco Supabase de desenvolvimento: RLS e auditoria de dados de testes | RLS presente nas 24 tabelas do projeto; 6 contas internas sem vínculo em `auth.users` e seus registros identificados por marcadores foram removidos em transação; demais registros não foram alterados | Verificação pontual, não monitorada |
| Entre 2026-11-04 e 2026-11-05 (data exata não registrada) | PR #11 (head `4d8e770`) mergeado em `main` como `20145fa` | 4/4 checks aprovados (`backend` e `mobile` incluídos); Run #3 em `main` passou e publicou os artefatos `backend-test-results` e `reelix-idempotent-migrations` | A CI não aplica migrations |
| Mesmo período | Proteção de `main` | PR obrigatório, checks `backend` e `mobile` aprovados, branch atualizada, sem bypass administrativo; **não exige aprovação de reviewer** | Configuração fora do Git |

### 1.3 Validação Android no Samsung (2026-11-04 a 2026-11-05)

Build: APK EAS de **development** (perfil `development`, `developmentClient`, distribuição interna; build `b0c790c9-bc1c-4c76-82fa-33bf317c92b6`), commit `98be2da` da branch `build-mobile`, conectado ao Metro.

| Verificação | Resultado |
| --- | --- |
| Conexão ao Metro, login e navegação | Passou (confirmado pelo usuário) |
| Logout | Passou |
| Deep link `reelix://auth/callback` com erro simulado | Abriu a rota e apresentou o estado inválido/expirado esperado, sem credenciais |
| Recuperação de senha (2026-11-05) | Novo link recebido, redefinição aberta no Samsung, senha alterada e login no app: **passou** |
| Correção associada | **Site URL** do Supabase ajustado para `reelix://auth/callback` (a allowlist já continha o callback); o callback passou a observar `PASSWORD_RECOVERY`, aceitar `type=recovery` e aguardar a sessão antes de abrir o formulário; lint, `tsc` e export do bundle Android passaram |

**Não validado:** callback de **cadastro** / confirmação de e-mail, deep links internos adicionais, build de **release** Android (depende de variáveis EAS e de endpoint HTTPS de produção), push remoto (desligado por padrão; exige projeto EAS, APNs/FCM, segredo de servidor e hosting contínuo), widgets (não existe código), iOS (sem validação local e fora do escopo atual), TalkBack/VoiceOver, contraste, performance sob carga, disponibilidade dos workers. `adb` não está instalado na máquina. Se um link de recovery futuro chegar como `otp_expired`, investigar pré-validação do link pelo provedor de e-mail (ver `.specs/0001-authentication.md`).

A inspeção visual em navegador não foi possível naquela auditoria: a URL compartilhada (`localhost:8087`) recusou conexão. O Expo web ainda não foi verificado visualmente.

## 2. Pré-requisitos

| Item | Como obter |
| --- | --- |
| .NET 10 SDK | Instalação local; a CI usa `10.0.x`. A máquina atual tem também o `11.0.100` (preview); o resultado em SDK 11 não substitui o da CI |
| `dotnet-ef` 10.0.13 | Fixado em `.config/dotnet-tools.json`; `dotnet tool restore` (use `dotnet tool run dotnet-ef ...`) |
| Node e npm | CI em Node 22; a máquina atual tem Node 24.20 e npm 12.2.1. Dependências do mobile: `npm ci` em `mobile/` |
| Docker em execução | Só para os 10 testes `PostgresAuthorizationIntegrationTests` (imagem `postgres:17-alpine` baixada na primeira vez). Compilar, lint, `tsc` e os outros 192 testes não dependem dele. Teste com `docker info` |
| Projeto Supabase de **desenvolvimento** | E-mail e senha habilitados; Site URL e allowlist conforme `.specs/0001-authentication.md` (callback `reelix://auth/callback`). Use **só** o projeto de desenvolvimento |
| `mobile/.env` | `Copy-Item mobile\.env.example mobile\.env` e preencher `EXPO_PUBLIC_SUPABASE_URL`, `EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY` e `EXPO_PUBLIC_API_URL`. O app **lança erro no import** se qualquer uma faltar; `EXPO_PUBLIC_API_URL` precisa ser HTTPS fora de `__DEV__` |
| User Secrets da API | `Supabase:Url` e `ConnectionStrings:Reelix` (obrigatórios); `Catalog:Tmdb:AccessToken` e `Catalog:Omdb:ApiKey` (para o catálogo); `Supabase:JwtSecret` só se o projeto ainda emitir JWT HS256 legado. Comandos em §4 |
| Dispositivo ou emulador Android | APK EAS de development (perfil `development` em `mobile/eas.json`) ou Expo Go, quando as bibliotecas nativas permitirem. `adb` **não** está instalado na máquina atual |
| Navegador (opcional) | Expo web via `npm run web`; origens CORS de desenvolvimento liberadas: `localhost` e `127.0.0.1` nas portas 8081 e 8087 |
| Leitor de tela | TalkBack no Android (VoiceOver só se houver build iOS, hoje fora de escopo) |
| Playwright | **Não instalado** e sem uso no projeto (ver `TESTING.md` §7) |

**Como não usar credenciais reais**

- Use contas de teste do projeto Supabase de **desenvolvimento**, com e-mail descartável. Nunca a conta pessoal de alguém, nem a de produção.
- Só valores públicos vão para `mobile/.env`: URL do projeto e chave **publishable** (ou `anon` legada). **Nunca** `service_role`, chave secret ou a chave privada/segredo JWT no mobile.
- Segredos do backend ficam em **User Secrets** ou em variáveis de ambiente. Nada de valor real em `appsettings*.json`, `mobile/.env.example`, issues, prints, logs ou neste documento.
- Os testes automatizados **não** precisam de nenhum desses valores (usam JWT RSA em memória e Postgres em contêiner). **Nunca** exporte a connection string do Supabase ao rodar testes ou scripts de teste: a que está nos User Secrets pertence ao banco de desenvolvimento compartilhado.
- Nas evidências (§7), registre e-mail de teste apenas se for descartável; nunca senha, token, chave ou connection string.

## 3. Gates obrigatórios

Backend, na raiz do repositório; mobile, em `mobile/`. Execute nesta ordem e registre o que viu:

| # | Comando | O que valida | Critério |
| --- | --- | --- | --- |
| 1 | `dotnet restore Reelix.sln --source https://api.nuget.org/v3/index.json -warnaserror:NU1903,NU1904` | Restauração dos pacotes; vulnerabilidade NuGet alta/crítica (NU1903/NU1904) vira erro (é o mesmo comando da CI) | Sem erro. `NU1900` local (feed privado) é ruído conhecido |
| 2 | `dotnet build Reelix.sln --configuration Release --no-restore` | Compilação de todos os projetos | Sem erro; avisos novos devem ser explicados |
| 3 | `dotnet test Reelix.sln --configuration Release --no-restore` | xUnit: domínio, serviços, modelo/migration EF, HTTP in-memory e HTTP + Postgres (Docker) | Todos passam; **Docker ativo**, senão os 10 testes Postgres falham |
| 4 | `npm ci` (em `mobile/`) | Dependências e lockfile | Executar se `package.json` ou `package-lock.json` mudou |
| 5 | `npm run lint` (em `mobile/`) | `expo lint` (ESLint com `eslint-config-expo` flat) | Zero erro |
| 6 | `npx tsc --noEmit` (em `mobile/`) | Tipos, `strict: true` | Zero erro |
| 7 | `npm audit --audit-level=high` (em `mobile/`) | Vulnerabilidades em dependências de desenvolvimento e produção | Sem high/critical. Os 21 advisories moderados/baixos já conhecidos (REELIX-019) **não** reprovam; qualquer high/critical novo reprova |
| 8 | `npx expo-doctor` (em `mobile/`) — **opcional**, não roda na CI | Compatibilidade de dependências e configuração do Expo | Sem falha nova; compare com a execução anterior |
| 9 | Verificação manual | Ver §5 | Checklist aplicável à mudança |

Outros comandos úteis (todos existem hoje):

| Comando | Efeito |
| --- | --- |
| `dotnet test Reelix.sln --configuration Release --no-restore --filter "FullyQualifiedName~Reelix.UnitTests.Tracking"` | Roda só uma pasta/classe (não há `[Trait]`; filtre por nome) |
| `dotnet test Reelix.sln --configuration Release --no-restore --filter "FullyQualifiedName!~PostgresAuthorizationIntegrationTests"` | Pula os 10 testes que exigem Docker |
| `dotnet test Reelix.sln --configuration Release --no-restore --logger "trx;LogFileName=backend-tests.trx"` | Gera TRX como a CI (o arquivo é gravado em `backend/Reelix.UnitTests/TestResults/`) |
| `dotnet tool restore` | Restaura `dotnet-ef` 10.0.13 |
| `dotnet tool run dotnet-ef migrations script --idempotent --no-build --configuration Release --project backend/Reelix.Infrastructure/Reelix.Infrastructure.csproj --startup-project backend/Reelix.Api/Reelix.Api.csproj --output artifacts/reelix-migrations.sql` | Gera o script idempotente (a CI faz o mesmo; **não conecta ao banco**; usa `Supabase__Url=https://migration-script.invalid` e uma connection string fictícia, como o workflow) |
| `dotnet package list --project Reelix.sln --no-restore --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json` | Audit NuGet (informativo: **não falha** o job) |
| `npm run web` / `npm start` / `npm run android` / `npm run ios` (em `mobile/`) | Sobe o Expo (`expo start`, com a plataforma indicada) |
| `npm run brand:generate` (em `mobile/`) | Regenera os PNGs de plataforma a partir dos SVGs da marca |

### 3.1 Gates previstos (ainda não existem)

Alvo de **specs futuras**. Não rode nem cite estes comandos como existentes: **não há** script, configuração nem dependência para nenhum deles.

| Gate previsto | Estado hoje | Onde está o plano |
| --- | --- | --- |
| `npm test` (Jest + `jest-expo` + React Native Testing Library) | Não existe: `mobile/package.json` não tem script de teste nem runner | `TESTING.md` §7.3; REELIX-003 |
| `npm run test:e2e` (Playwright sobre o Expo web) | Não existe: Playwright não está instalado | `TESTING.md` §7.4 |
| Cobertura (backend com `coverlet`, mobile com Jest) | Não existe como gate: `coverlet.collector` está referenciado, mas ninguém o executa; nenhum piso definido | `TESTING.md` §7.6 |
| Testes de arquitetura (NetArchTest ou ArchUnitNET, projeto dedicado) | Não existe | `TESTING.md` §7.5 |
| Teste de contrato OpenAPI | Não existe | `DEVELOPMENT.md` §9 |
| `dotnet package list --vulnerable` falhando o job | Existe, mas **informativo** | §6 |

### 3.2 Se um gate falhar

1. Leia a mensagem completa; não mascare com `@ts-ignore`, `any`, `#pragma warning disable` ou desativação de regra.
2. Corrija a causa.
3. Rode novamente o gate que falhou.
4. Se o erro for pré-existente e fora do escopo, registre no relatório com evidência; não o esconda. "Baseline" não é desculpa para ignorar uma falha nova.

## 4. Subir o ambiente

### 4.1 API (porta 5035)

```powershell
# uma vez: segredos da API (valores do projeto Supabase de DESENVOLVIMENTO)
dotnet user-secrets set "Supabase:Url" "https://SEU-PROJETO.supabase.co" --project backend\Reelix.Api\Reelix.Api.csproj
dotnet user-secrets set "ConnectionStrings:Reelix" "Host=SEU_HOST;Port=5432;Database=postgres;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require" --project backend\Reelix.Api\Reelix.Api.csproj
# opcionais, para o catálogo
dotnet user-secrets set "Catalog:Tmdb:AccessToken" "<TMDB Read Access Token>" --project backend\Reelix.Api\Reelix.Api.csproj
dotnet user-secrets set "Catalog:Omdb:ApiKey" "<OMDb API key>" --project backend\Reelix.Api\Reelix.Api.csproj

dotnet restore Reelix.sln
dotnet run --project backend\Reelix.Api\Reelix.Api.csproj --launch-profile http
```

O perfil `http` roda em `Development` e escuta em `http://0.0.0.0:5035` (acessível pela rede, necessário para emulador e dispositivo físico). A API **não executa migration ao subir** (`Program.cs` não chama `Migrate`); aplique-as à parte (§4.4). A análise de spoilers por OpenAI e o push remoto vêm **desativados** por padrão; não os habilite para QA de rotina.

| Serviço | URL | Observação |
| --- | --- | --- |
| API | http://localhost:5035 | Só `Development` expõe o que segue |
| Swagger UI | http://localhost:5035/swagger | Botão **Authorize**: cole o access token, **sem** o prefixo `Bearer` |
| OpenAPI JSON | http://localhost:5035/openapi/v1.json | |
| Página de token de desenvolvimento | http://localhost:5035/dev-auth.html | Informe a chave publishable, e-mail e senha de uma conta de teste; o navegador fala direto com o Supabase e a API não recebe a senha. Nunca use `service_role` nem chave secret |
| Expo / Metro | http://localhost:8081 (padrão) | Para o Expo web com CORS liberado use 8081 ou 8087 (`npx expo start --web --port 8087`) |

Se a 5035 estiver ocupada por uma instância antiga, encerre-a **antes** (um processo esquecido atende no lugar e roda código antigo) ou inicie em outra porta com `--urls http://localhost:5036` e ajuste `EXPO_PUBLIC_API_URL`.

### 4.2 App mobile

```powershell
Copy-Item mobile\.env.example mobile\.env
# edite mobile\.env: EXPO_PUBLIC_SUPABASE_URL, EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY, EXPO_PUBLIC_API_URL
Set-Location mobile
npm ci
npx expo start
```

`EXPO_PUBLIC_API_URL` depende de onde o app roda:

| Onde roda | Valor |
| --- | --- |
| Navegador (Expo web) e simulador iOS | `http://localhost:5035` |
| Emulador Android | `http://10.0.2.2:5035` |
| Dispositivo físico | `http://<IP-da-máquina-na-rede-local>:5035` |
| Release | HTTPS obrigatório (o app recusa HTTP fora de `__DEV__`) |

Reinicie o Expo após alterar o `.env`. Para dispositivo físico ou emulador com bibliotecas nativas, use o APK EAS de development (`eas build --profile development`, ver `mobile/AGENTS.md`); o plugin `withDevelopmentCleartextTraffic` só permite HTTP quando `APP_VARIANT=development`.

### 4.3 Supabase (resumo do que a validação exige)

Em **Authentication → URL Configuration**: allowlist com `reelix://auth/callback`; Site URL `reelix://auth/callback` enquanto não houver site HTTPS (não deixe `http://localhost:3000`); template **Reset Password** com `{{ .ConfirmationURL }}` (ou `{{ .RedirectTo }}`), sem endereço fixo. Detalhes e armadilhas (link `otp_expired`, limites de e-mail) em `.specs/0001-authentication.md`.

### 4.4 Migrations

Estes comandos **escrevem no banco apontado pela connection string ativa**. Em `Development`, a dos User Secrets é a do banco **compartilhado** de desenvolvimento. Confirme o destino e tenha autorização antes de aplicar; nunca use connection string de produção ou de banco compartilhado sem autorização explícita.

```powershell
dotnet tool restore

# somente leitura: lista migrations e o que está aplicado
dotnet tool run dotnet-ef migrations list --project backend\Reelix.Infrastructure\Reelix.Infrastructure.csproj --startup-project backend\Reelix.Api\Reelix.Api.csproj

# gera o SQL idempotente para revisão (não conecta ao banco)
$env:Supabase__Url = "https://migration-script.invalid"
$env:ConnectionStrings__Reelix = "Host=localhost;Database=reelix_migration;Username=postgres;Password=not-used"
dotnet tool run dotnet-ef migrations script --idempotent --project backend\Reelix.Infrastructure\Reelix.Infrastructure.csproj --startup-project backend\Reelix.Api\Reelix.Api.csproj --output artifacts\reelix-migrations.sql

# aplica (escreve no banco!), a partir da connection string da sessão
$env:ConnectionStrings__Reelix = "Host=localhost;Database=reelix;Username=postgres;Password=<senha-local>"
dotnet tool run dotnet-ef database update --project backend\Reelix.Infrastructure\Reelix.Infrastructure.csproj --startup-project backend\Reelix.Api\Reelix.Api.csproj
```

O `README.md` usa a forma `dotnet ef database update ...` (ferramenta global); a forma `dotnet tool run dotnet-ef` usa a versão fixada pelo repositório. As 10 migrations atuais vão de `20261026133000_InitialUserAccounts` até `20261103134321_ExpandFixedAvatars`. Um **ambiente novo** exige: inventário do histórico, revisão do SQL idempotente, aplicação e smoke test (login, `GET /api/v1/auth/me`, uma rota de cada módulo) antes de uso.

## 5. Checklist manual

Aplique apenas o que a mudança afeta, mas registre o que foi verificado. O mobile não tem teste automatizado, então este checklist **é** a verificação funcional.

### 5.1 Geral

- [ ] Plataforma e build registrados: Android (APK EAS de development ou Expo Go), emulador, ou Expo web (e o navegador).
- [ ] A API sobe sem erro e o app conecta ao Metro/servidor sem erro de variável (`EXPO_PUBLIC_*`).
- [ ] Console do Metro/DevTools sem erro novo e sem warning novo relevante.
- [ ] Nenhuma chamada de rede falhando de forma inesperada (Network no web; log da API no mobile).
- [ ] Textos em pt-BR, sem `lorem ipsum`/placeholder e sem dados falsos de módulos não implementados.
- [ ] A mudança não expõe token, chave, e-mail ou dado pessoal em tela, log ou rede; nenhuma chave `service_role`/secret no app.
- [ ] Login, navegação principal e logout continuam funcionando (smoke de regressão).

### 5.2 UI e responsividade

- [ ] **Android:** dispositivo real (o Samsung da validação anterior) e, se possível, emulador com outra densidade.
- [ ] **Web (Expo):** larguras de **375 px** (mobile) e **768 px** (tablet) e uma desktop; sem scroll horizontal inesperado. Lembre que o tema do app é escuro (`userInterfaceStyle: dark`).
- [ ] Rotação: o app é `portrait` (`orientation` em `app.json`); confirme que nada quebra.
- [ ] Conteúdo não cortado, sobreposto ou ilegível; teclado virtual não cobre campo ou botão de ação.
- [ ] Estados de **hover** (web), **foco**, **pressionado**, **desabilitado**, **carregando** e **erro** revisados.
- [ ] Áreas de toque com pelo menos **44×44 px**.
- [ ] Toda cor, espaçamento e tamanho vem dos tokens de `src/theme/tokens.ts` (sem valor fixo novo).
- [ ] Mudança de tela: wireframe `.docs/design/wireframes/reelix-visual-wireframes.html` atualizado (regra de `mobile/AGENTS.md`).
- [ ] **Limitação do web:** `Alert.alert` não faz nada no RN Web (confirmação de "Desmarcar como assistido" em `tracking.tsx` e diálogo em `rating-review-section.tsx`). Valide esses fluxos no Android.

### 5.3 Acessibilidade

- [ ] **TalkBack** (Android) anuncia nome, papel e estado dos controles principais; rótulos em pt-BR; erros e carregando são anunciados.
- [ ] **VoiceOver** (iOS), somente se houver build iOS (hoje fora de escopo; registre "não verificado" se não houver).
- [ ] Navegação por teclado completa no web (Tab/Shift+Tab/Enter/Espaço/Esc); foco visível e em ordem lógica.
- [ ] Todo input tem rótulo; erros de campo aparecem **inline** e em texto; o botão Mostrar/Ocultar senha tem rótulo e área de 44 px.
- [ ] **Contraste** de texto e ícones validado (mínimo AA, 4,5:1 para texto normal; 3:1 para texto grande e componentes de UI), nos estados normal, desabilitado e de erro.
- [ ] **Tamanho de texto do sistema** aumentado (fonte grande no Android): nada é cortado nem sobreposto.
- [ ] Imagens informativas com rótulo; decorativas ocultas ao leitor de tela.
- [ ] Animações respeitam a preferência de redução de movimento do sistema.

### 5.4 Estados, erros e feedback

- [ ] Estado **vazio**, **carregando** e **erro** implementados em cada lista/tela de dados.
- [ ] **Toast** (`useToast().showError` ou `useErrorToast`) para falhas de API, carregamento e salvamento; nada de erro de rede inline.
- [ ] Validação por campo, limites de seleção e mensagens de **sucesso** ficam inline (padrão do projeto).
- [ ] Falha de rede e timeout tratados: desligue a API, ou o Wi-Fi, e repita a ação; o retry funciona e o app não trava.
- [ ] Sessão expirada ou inválida: o app volta ao login sem travar.
- [ ] Mensagens de erro do Supabase Auth não revelam se o e-mail existe.

### 5.5 Autenticação e deep links (Android nativo)

- [ ] Cadastro, login, logout e recuperação de senha com conta de teste do projeto de **desenvolvimento**.
- [ ] Deep link `reelix://auth/callback` com erro simulado abre a rota e mostra o estado inválido/expirado, sem credenciais.
- [ ] Link de recuperação válido abre o formulário de nova senha (e entra no app depois de redefinir).
- [ ] Callback de **cadastro / confirmação de e-mail** (**ainda não validado**, ver §1.3).
- [ ] Sessão persiste após fechar e reabrir o app (SecureStore) e some após o logout.
- [ ] O app não é afetado por `http://localhost:3000` nos e-mails (Site URL correto, §4.3).

### 5.6 Dados, integrações e permissões

- [ ] Um usuário **não** acessa dado de outro (perfil privado, listas, avaliações, tracking, inbox, preferências): confirme também com duas contas de teste.
- [ ] Privacidade: os consentimentos (histórico, avaliações, favoritos) de amizade são respeitados no feed e no perfil contextual.
- [ ] Catálogo: busca, detalhes e temporada respondem; com a credencial TMDB ausente a API responde 503 e o app mostra erro tratável; atribuição TMDB visível onde exigida.
- [ ] **Push remoto desativado:** o app não solicita nem exibe push quando `ExpoPush__Enabled` está desligado; a inbox de notificações continua funcionando.
- [ ] Análise automática de spoilers desativada; a sinalização manual de spoiler funciona.
- [ ] Migrations: o banco de destino está na última migration (`dotnet-ef migrations list`, §4.4); migration/rollback documentados quando a mudança altera schema.
- [ ] Nenhuma chamada externa em loop ou sem limite; limites de requisição (avaliações, busca/escritas sociais e notificações) respondem 429 de forma tratada.
- [ ] CORS (web): a origem do Expo web está na lista de `Cors:AllowedOrigins` (8081/8087 em `Development`).

## 6. Dívidas de QA conhecidas

| # | Problema | Impacto |
| --- | --- | --- |
| 1 | **Sem testes automatizados do app mobile** (nenhum runner; REELIX-003) | Auth, navegação, mutações, privacidade e estados de erro podem regredir sem aviso; a verificação é só manual |
| 2 | **Sem E2E** (Playwright não instalado) | A jornada app + API + banco só é exercitada à mão |
| 3 | **Cobertura não medida** (`coverlet.collector` referenciado e sem uso) | Não há número nem piso; 339 testes não dizem quanto código é exercitado |
| 4 | **Sem testes de arquitetura** nem de contrato OpenAPI | Regra de dependência entre camadas e contrato HTTP não são verificados por máquina |
| 5 | **Workers sem teste** (`CommentModerationWorker`, `SocialNotificationWorker`, `NotificationDeliveryWorker`) | Moderação, notificações sociais e entrega push só por verificação manual |
| 6 | 21 advisories npm moderados/baixos (3 low, 18 moderate) sem correção compatível (REELIX-019, bloqueado) | O gate `--audit-level=high` passa, mas o risco moderado segue aberto; não forçar downgrade |
| 7 | `dotnet package list --vulnerable` **não falha** o job da CI (só NU1903/NU1904 no restore) | Vulnerabilidade NuGet pode passar se não for NU1903/NU1904 |
| 8 | **Validação nativa incompleta:** callback de cadastro, build de release, push, widgets e iOS sem verificação (REELIX-005, 006, 013) | Fluxos podem falhar fora do que foi validado no Samsung |
| 9 | **Acessibilidade sem verificação real:** nenhum teste com TalkBack/VoiceOver, contraste, Dynamic Type ou multiplataforma (REELIX-015) | Falhas de semântica/contraste podem existir |
| 10 | Sem formatação/analisadores: nenhum `.editorconfig`, `dotnet format` nem Prettier; lint só no mobile | Estilo e avisos de compilador não são gates |
| 11 | Sem `global.json`: o `dotnet` local usa o SDK mais recente (hoje 11.0.100, preview), enquanto a CI usa .NET 10 | Um teste pode passar local e falhar na CI (ou o inverso) |
| 12 | 10 testes dependem de Docker, e as fábricas de teste alteram variáveis de ambiente (estado global) com as classes rodando em paralelo (nenhuma desativação de paralelismo configurada) | Falha confusa sem Docker; fábricas novas precisam sobrescrever a connection string por configuração em memória (`TESTING.md` §2.3) |
| 13 | Só existe o ambiente `Development` (migrations validadas apenas nele e em PostgreSQL descartável); sem staging | Novo ambiente exige repetir inventário, aplicação e smoke test |
| 14 | Sem observabilidade (métricas, tracing, crash reporting, alertas) e sem teste de carga/performance | Problemas em runtime só aparecem por log; capacidade não medida |
| 15 | Artefatos gerados versionados no Git: `backend/Reelix.UnitTests/TestResults/backend-tests.trx` e `artifacts/reelix-migrations.sql` | Ruído em diffs e risco de TRX/SQL desatualizado ser tomado como evidência |
| 16 | O Expo web não foi verificado visualmente; `Alert.alert` é no-op no RN Web | O web não serve para validar fluxos com confirmação nativa |
| 17 | Aviso `NU1900` local (feed privado do Azure DevOps na configuração NuGet da máquina) | Ruído no restore local; não afeta a CI |

## 7. Template de evidência

Copie para o relatório final da tarefa:

```markdown
## QA executado

**Data / commit:** AAAA-MM-DD / `<hash>`
**Ambiente:** SDK `dotnet --version`, Node, Docker (sim/não), plataforma (Android build X / emulador / web + navegador e largura)

| Verificação | Comando | Resultado |
| --- | --- | --- |
| Restore | `dotnet restore Reelix.sln --source https://api.nuget.org/v3/index.json -warnaserror:NU1903,NU1904` | Passou / Falhou |
| Build | `dotnet build Reelix.sln --configuration Release --no-restore` | Passou / Falhou |
| Testes backend | `dotnet test Reelix.sln --configuration Release --no-restore` | N aprovados / N falhos / N ignorados |
| Lint mobile | `npm run lint` | Passou / Falhou |
| Tipos mobile | `npx tsc --noEmit` | Passou / Falhou |
| Audit npm | `npm audit --audit-level=high` | Passou / Falhou (N advisories) |
| Expo Doctor (opcional) | `npx expo-doctor` | Passou / Falhou / Não rodado |
| Manual | 375 / 768 (web) ou dispositivo | OK / Pendência |

### Verificações manuais

- [ ] Fluxo alterado exercitado de ponta a ponta (passos e conta de teste, sem senha)
- [ ] Teclado/foco (web) e leitor de tela (TalkBack)
- [ ] Contraste e tamanho de texto
- [ ] Estados vazio, carregando e erro; toast
- [ ] Console/Network e log da API sem erro novo
- [ ] Textos em pt-BR e conteúdo final

### Não verificado

- Motivo e risco (por exemplo: iOS, release, push, widgets, leitor de tela).
```

Regras da evidência: registre só o que executou; informe contagens reais (não "passou tudo"); nunca inclua senha, token, chave ou connection string; diga quando algo **não** foi rodado.

## 8. Template de bug

```markdown
**Título:** [área] comportamento observado
**Ambiente:** plataforma (Android + modelo/versão, emulador ou web + navegador e largura), build (APK EAS / Metro), commit, API (local / Development)

**Passos:**

1.
2.

**Esperado:**
**Obtido:**
**Evidência:** print/vídeo/log (sem token, senha nem e-mail real)
**Impacto:** bloqueante / alto / médio / baixo
**Item do backlog relacionado:** REELIX-NNN (se houver)
```
