# Reelix

Aplicativo para descobrir e acompanhar filmes e séries, registrar o que você assistiu, organizar títulos em listas e compartilhar atividades autorizadas com amigos.

O repositório reúne o app mobile (React Native + Expo, em `mobile/`) e uma API ASP.NET Core (.NET 10, em `backend/`). A autenticação usa Supabase Auth; os dados da conta Reelix ficam em PostgreSQL.

> **Fonte de verdade:** a documentação canônica está em [`.docs/`](.docs/) — comece por [`.docs/AGENTS.md`](.docs/AGENTS.md). Este README é só um resumo de entrada.

## Estado atual

Fotografia em 2026-11-19. Implementação no código **não** comprova implantação: migrations e credenciais são verificadas por ambiente (hoje só existe `Development`). Detalhes e limites em [`.docs/QA.md`](.docs/QA.md) e [`.docs/ARCHITECTURE.md`](.docs/ARCHITECTURE.md).

| Módulo | Estado no código | Limites conhecidos |
|---|---|---|
| Autenticação, onboarding, perfil, catálogo | Implementados no mobile e na API | Upload de avatar e exclusão/exportação de conta não existem |
| Home, descoberta, busca | Implementadas com dados reais | Sem recomendação por IA |
| Tracking, favoritos, watchlist, listas | Implementados | Progresso depende do inventário do provedor |
| Avaliações e comentários | Implementados, com proteção contra spoilers | Análise automática desativada; sem denúncias/moderação |
| Social, notificações, gamificação | Implementados (inbox; ranking opt-in) | Push remoto desativado; sem alertas de catálogo |
| Widgets Android | Escopo aprovado; **não implementado** | iOS adiado |
| Testes | Backend: 202 testes xUnit | **Sem testes do mobile e sem E2E** |

## Tecnologias

- Mobile: React Native, Expo SDK 58, TypeScript, Expo Router, TanStack Query, zod.
- API: ASP.NET Core / .NET 10, EF Core + Npgsql, PostgreSQL.
- Identidade: Supabase Auth (a API valida o JWT).
- Catálogo: TMDB (principal) e OMDb (opcional), chamados pelo backend.

## Requisitos

- .NET 10 SDK, Node.js e npm.
- Docker (apenas para os testes que usam Testcontainers).
- Um projeto Supabase de **desenvolvimento** com e-mail e senha habilitados.

## Começando

Configure os segredos da API (nunca versione valores reais; veja [`.docs/QA.md`](.docs/QA.md) §2 e §4 e [`.specs/0001-authentication.md`](.specs/0001-authentication.md)):

```powershell
dotnet user-secrets set "Supabase:Url" "https://SEU-PROJETO.supabase.co" --project backend\Reelix.Api\Reelix.Api.csproj
dotnet user-secrets set "ConnectionStrings:Reelix" "Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require" --project backend\Reelix.Api\Reelix.Api.csproj
dotnet user-secrets set "Catalog:Tmdb:AccessToken" "<TMDB Read Access Token>" --project backend\Reelix.Api\Reelix.Api.csproj
```

Suba a API (`http://localhost:5035`, Swagger em `/swagger`):

```powershell
dotnet restore Reelix.sln
dotnet run --project backend\Reelix.Api\Reelix.Api.csproj --launch-profile http
```

Suba o app:

```powershell
Copy-Item mobile\.env.example mobile\.env
Set-Location mobile
npm ci
npx expo start
```

Preencha `mobile/.env` com `EXPO_PUBLIC_SUPABASE_URL`, `EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY` e `EXPO_PUBLIC_API_URL`. Nunca use `service_role` nem chaves secretas no app.

## Verificações

```powershell
dotnet test Reelix.sln --configuration Release
Set-Location mobile
npm run lint
npx tsc --noEmit
```

O CI ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) roda os testes do backend, lint e type-check do mobile e auditorias de dependências em pull requests; ele gera o script idempotente de migrations como artefato e **não** aplica schema.

## Documentação

| Documento | Conteúdo |
|---|---|
| [`.docs/AGENTS.md`](.docs/AGENTS.md) | Guia operacional: comandos, regras, skills |
| [`.docs/BUSINESS.md`](.docs/BUSINESS.md) | Produto, conceitos, escopo e roadmap |
| [`.docs/ARCHITECTURE.md`](.docs/ARCHITECTURE.md) | Estrutura, contratos, decisões e dívidas |
| [`.docs/DEVELOPMENT.md`](.docs/DEVELOPMENT.md) | Padrões de código e convenções de API |
| [`.docs/DESIGN.md`](.docs/DESIGN.md), [`.docs/DESIGN_SYSTEM.md`](.docs/DESIGN_SYSTEM.md) | Identidade visual e design system do mobile |
| [`.docs/TESTING.md`](.docs/TESTING.md), [`.docs/QA.md`](.docs/QA.md) | Estratégia de testes, gates e evidências |
| [`.docs/RULES.md`](.docs/RULES.md) | Fluxo de trabalho, templates e limites de atuação |
| [`.specs/`](.specs/), [`.plans/`](.plans/), [`.adrs/`](.adrs/) | Specs, planos e decisões arquiteturais |
| [`mobile/assets/brand/README.md`](mobile/assets/brand/README.md) | Brand kit |
