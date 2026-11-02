# Reelix

Aplicativo para descobrir e acompanhar filmes e séries, registrar o que você assistiu e compartilhar atividades autorizadas com amigos.

O repositório reúne o aplicativo mobile em React Native/Expo e uma API ASP.NET Core. A autenticação usa Supabase Auth; os dados da conta Reelix são armazenados em PostgreSQL.

## Estado atual do projeto

**Atualizado em 02/11/2026.** A tabela descreve implementação no repositório, não
confirma implantação em produção. Migrations e credenciais precisam ser verificadas
separadamente em cada ambiente.

| Módulo | Estado no código | Limites operacionais conhecidos |
|---|---|---|
| Autenticação, onboarding, perfil e catálogo | Implementados no mobile e na API. | Supabase, credenciais dos provedores e migrations devem estar configurados no ambiente. Upload de avatar não está implementado. |
| Home, descoberta e busca | Implementadas com dados reais, paginação automática, tracking e cache definidos por seção. | Datas e metadados dependem do catálogo; não há recomendação por IA. |
| Tracking e listas | Implementados, incluindo progresso da série inteira, listas privadas, edição e exclusão contextual. | O progresso total depende do inventário de temporadas do catálogo; consulte [tracking](docs/specs/05-tracking.md). |
| Avaliações e comentários | Implementados, com notas de meia estrela, audiência privada e proteção contra spoilers. | Classificação automática de spoilers está temporariamente desativada; denúncias e moderação administrativa não estão disponíveis. |
| Social | API e mobile implementados para amizades e compartilhamento autorizado. | A migration deve ser aplicada ao banco de cada ambiente antes do uso. |
| Notificações | Preferências, inbox, dispositivos e processamento de eventos sociais implementados. | Migration ainda precisa ser aplicada; push remoto está desativado e alertas de catálogo dependem de sincronização confiável. |
| Gamificação | API e mobile implementados para conquistas, estatísticas e ranking. | A migration precisa ser aplicada antes do uso em cada banco; reconciliação ocorre ao consultar o dashboard. |
| Widgets mobile | Spike de compatibilidade documentado; não implementados. | Build, integração e validação em iOS e Android pendentes. |

As especificações por módulo, incluindo decisões e limitações, estão em
[docs/specs](docs/specs/). As páginas de desenvolvimento são planos históricos;
use este quadro e as especificações como estado atual.

## Funcionalidades implementadas

O projeto já inclui autenticação, catálogo, onboarding, perfil, Home de descoberta,
acompanhamento e favoritos/listas:

- Cadastro, login, confirmação de e-mail, recuperação de senha e logout pelo Supabase Auth.
- Callback de autenticação por deep link, compatível com Expo.
- API .NET que valida tokens JWT do Supabase e expõe `GET /api/v1/auth/me` e endpoints autenticados de pesquisa e detalhes de catálogo.
- Integração com TMDB e OMDb, com referências externas persistidas no PostgreSQL.
- Onboarding retomável com preferências de gêneros e títulos favoritos validados pelo catálogo.
- Perfil próprio com nome, biografia e privacidade, consulta contextual de perfil e medalhas de conquistas. Upload de avatar não está implementado.
- Home autenticada com áreas separadas para filmes e séries, destaques e lançamentos do catálogo, favoritos, sugestões por gênero, pesquisa e detalhes de títulos.
- Acompanhamento de filmes/séries assistidos e gerenciamento de favoritos, watchlist e listas privadas.
- Avaliações de 0 a 5 em passos de meia estrela e comentários privados ou públicos, com sinalização e revelação explícita de spoilers para filmes, séries e episódios.
- Análise opcional de possíveis spoilers, desativada por padrão e executada no backend somente com consentimento explícito por comentário.
- Amizades, solicitações, busca protegida e feed paginado, com consentimentos separados para histórico, avaliações e favoritos.
- Caixa de entrada de notificações sociais com preferências, registro privado de dispositivos e processamento persistente de tickets/receipts Expo.
- Conquistas determinísticas, estatísticas com estimativa de runtime e ranking mensal entre amigos com opt-in e paginação.
- API autenticada para onboarding e perfil; veja [configuração e contratos](docs/onboarding-profile-setup.md).
- Regras e contratos da Home de descoberta estão em [docs/home-discovery-setup.md](docs/home-discovery-setup.md).
- Swagger para testar a API localmente e uma página exclusiva de desenvolvimento para obter um token de usuário diretamente pelo Supabase Auth.
- Migrations e testes automatizados para autenticação, catálogo, onboarding, perfil, notificações, gamificação e configuração CORS de desenvolvimento web.

Notificações sociais e gamificação estão implementadas no código, mas dependem de migrations aplicadas no banco de cada ambiente. Push remoto permanece desativado até configurar projeto EAS, credenciais APNs/FCM, segredo Expo e hosting continuamente ativo. Alertas de episódios/estreias ainda dependem de uma fonte confiável de atualização do catálogo. A análise automática de spoilers está temporariamente desativada; sinalização manual continua disponível. Widgets não estão implementados nem aprovados para iOS/Android. Consulte as especificações de [avaliações](docs/specs/07-ratings-comments.md), [Social](docs/specs/09-social.md), [Notificações](docs/specs/10-notifications.md), [Gamificação](docs/specs/11-gamification.md) e [Widgets](docs/specs/12-mobile-widgets.md).

## Tecnologias

- Mobile: React Native, Expo, TypeScript e Expo Router.
- API: ASP.NET Core / .NET 10.
- Persistência: PostgreSQL com Entity Framework Core.
- Identidade: Supabase Auth.

## Requisitos

- .NET 10 SDK.
- Node.js e npm.
- Um projeto Supabase com autenticação por e-mail habilitada e acesso ao PostgreSQL.

## Configuração local

### 1. Configure o Supabase

Siga [docs/authentication-setup.md](docs/authentication-setup.md) para configurar as chaves públicas do app, o callback de autenticação, as credenciais do backend e o PostgreSQL. Não coloque senhas, tokens ou chaves secretas em arquivos versionados.

Configure os segredos do backend com User Secrets:

```powershell
dotnet user-secrets set "Supabase:Url" "https://SEU-PROJETO.supabase.co" --project backend\Reelix.Api\Reelix.Api.csproj
dotnet user-secrets set "ConnectionStrings:Reelix" "Host=SEU_HOST;Port=5432;Database=postgres;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require" --project backend\Reelix.Api\Reelix.Api.csproj
```

Use os valores de conexão PostgreSQL exibidos no painel do seu projeto Supabase. A connection string precisa estar no formato `Host=...;Database=...;Username=...;Password=...`.

### 2. Configure e inicie a API

A análise automática de spoilers está temporariamente desativada. Para reativá-la futuramente, configure `OpenAI:SpoilerAnalysis:Enabled=true`, `OpenAI:SpoilerAnalysis:TemporarilyDisabled=false` e `OpenAI:SpoilerAnalysis:ApiKey` no servidor, usando User Secrets ou um gerenciador de segredos do ambiente de hospedagem. `Model` e `TimeoutSeconds` são opcionais. A sinalização manual e os controles de spoiler continuam disponíveis; comentários não dependem da integração. Nunca configure a chave OpenAI no app mobile.

```powershell
dotnet restore Reelix.sln
dotnet run --project backend\Reelix.Api\Reelix.Api.csproj --launch-profile http
```

No perfil local, a API fica disponível em `http://localhost:5035`. O Swagger UI e a especificação OpenAPI, habilitados apenas em `Development`, ficam em:

- Swagger UI: <http://localhost:5035/swagger>
- OpenAPI JSON: <http://localhost:5035/openapi/v1.json>

Os endpoints protegidos exigem um access token válido do mesmo projeto Supabase configurado na API. Para testar com sua conta, abra <http://localhost:5035/dev-auth.html>, informe a chave publishable (ou a chave `anon` legada), e-mail e senha, e cole o access token retornado no botão **Authorize** do Swagger, sem o prefixo `Bearer`. O navegador envia as credenciais diretamente ao Supabase; a API local não recebe nem armazena sua senha. Nunca use uma chave `service_role` ou secret nesse fluxo. Veja [as instruções detalhadas de teste do catálogo](docs/catalog-setup.md#iniciar-e-abrir-swagger).

### 3. Configure e inicie o app

```powershell
Copy-Item mobile\.env.example mobile\.env
```

Preencha `mobile/.env` com a URL do projeto e a chave **publishable** do Supabase. Para uso no navegador local, a URL da API deve ser `http://localhost:5035`. Depois:

```powershell
Set-Location mobile
npm ci
npx expo start
```

Nunca use `service_role`, chaves secretas ou a chave privada JWT no app mobile.

## Banco de dados

Para criar ou atualizar as tabelas no banco configurado, aplique as migrations:

```powershell
dotnet ef database update --project backend\Reelix.Infrastructure\Reelix.Infrastructure.csproj --startup-project backend\Reelix.Api\Reelix.Api.csproj
```

A tabela `public.user_accounts` e as tabelas de preferências/perfil têm RLS habilitado e não concedem acesso direto aos papéis da API Data; o backend acessa o PostgreSQL com sua própria connection string. Veja a [documentação de onboarding e perfil](docs/onboarding-profile-setup.md#banco-de-dados) antes de aplicar a nova migration.

Push remoto é opcional e vem desativado por padrão. Para habilitá-lo, configure `extra.eas.projectId` no app e as credenciais APNs/FCM em EAS; no backend, defina `ExpoPush__Enabled=true`, `ExpoPush__ProjectId`, `ExpoPush__AccessToken` e, opcionalmente, `ExpoPush__MaxAttempts` (1–10). `ExpoPush__AccessToken` é segredo de servidor e nunca deve ser colocado no app. A API e o worker de entrega precisam ficar continuamente ativos; sem essa configuração, a inbox social continua funcionando, mas o registro push responde indisponível.

## Verificações

Na raiz do repositório:

```powershell
dotnet test Reelix.sln
```

No diretório `mobile`:

```powershell
npx tsc --noEmit
npx expo lint
```

## Documentação

- [Configuração de autenticação e ambiente local](docs/authentication-setup.md)
- [Configuração do catálogo, provedores e teste no Swagger](docs/catalog-setup.md)
- [Avaliação de tecnologias adicionais](docs/technology-stack-evaluation.md)
- [Configuração e contratos de onboarding e perfil](docs/onboarding-profile-setup.md)
- [Regras e contratos da Home de descoberta](docs/home-discovery-setup.md)
- [Plano de Home e descoberta (histórico)](docs/plans/05-home-discovery-development-plan.html)
- [Especificação implementada de avaliações e comentários](docs/specs/07-ratings-comments.md)
- [Plano de avaliações, comentários e spoilers (histórico)](docs/plans/06-ratings-comments-development-plan.html)
- [Especificação implementada de amizades e compartilhamento](docs/specs/09-social.md)
- [Plano de notificações e integração de eventos (histórico)](docs/plans/08-notifications-development-plan.html)
- [Relatório dos cinco ajustes adicionais de UX](docs/implementation-report-additional-ux.md)
- Próxima validação técnica: aplicação de migrations pendentes nos ambientes e validação de builds nativos para widgets.
- [Arquitetura](docs/architecture.md)
- [Especificações do produto](docs/specs/)
- [Identidade visual e Brand Assets](mobile/assets/brand/README.md)
