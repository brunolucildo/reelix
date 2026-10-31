# Reelix

Aplicativo para descobrir e acompanhar filmes e séries, registrar o que você assistiu e, em etapas futuras, compartilhar atividade com amigos.

O repositório reúne o aplicativo mobile em React Native/Expo e uma API ASP.NET Core. A autenticação usa Supabase Auth; os dados da conta Reelix são armazenados em PostgreSQL.

## Etapas entregues

O projeto já inclui autenticação, consulta de catálogo, onboarding e perfil:

- Cadastro, login, confirmação de e-mail, recuperação de senha e logout pelo Supabase Auth.
- Callback de autenticação por deep link, compatível com Expo.
- API .NET que valida tokens JWT do Supabase e expõe `GET /api/v1/auth/me` e endpoints autenticados de pesquisa e detalhes de catálogo.
- Integração com TMDB e OMDb, com referências externas persistidas no PostgreSQL.
- Onboarding retomável com preferências de gêneros e títulos favoritos validados pelo catálogo.
- Perfil próprio com nome, biografia e privacidade, além de consulta de perfil público conforme as permissões do usuário.
- API autenticada para onboarding e perfil; veja [configuração e contratos](docs/onboarding-profile-setup.md).
- Swagger para testar a API localmente e uma página exclusiva de desenvolvimento para obter um token de usuário diretamente pelo Supabase Auth.
- Migrations e testes automatizados para autenticação, catálogo, onboarding, perfil e configuração CORS de desenvolvimento web.

Home de descoberta, acompanhamento de títulos, avaliações, listas e recursos sociais permanecem para etapas futuras. A migration de onboarding/perfil está no repositório, mas precisa ser aplicada a um banco local/de desenvolvimento antes de usar esses recursos nesse ambiente.

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
- [Arquitetura](docs/architecture.md)
- [Especificações do produto](docs/specs/)
- [Identidade visual e Brand Assets](mobile/assets/brand/README.md)
