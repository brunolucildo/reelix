# Reelix

Aplicativo para acompanhar filmes e séries, registrar o que você assistiu e, nas próximas etapas, descobrir títulos e compartilhar atividade com amigos.

O repositório reúne o aplicativo mobile em React Native/Expo e uma API ASP.NET Core. A autenticação usa Supabase Auth; os dados da conta Reelix são armazenados em PostgreSQL.

## Primeira etapa

Esta etapa entrega o fluxo inicial de autenticação e a estrutura para consultar a conta:

- Cadastro, login, confirmação de e-mail, recuperação de senha e logout pelo Supabase Auth.
- Callback de autenticação por deep link, compatível com Expo.
- API .NET que valida tokens JWT do Supabase e expõe `GET /api/v1/auth/me`.
- Criação do registro da conta no PostgreSQL, com migration inicial e RLS habilitado.
- Testes automatizados dos endpoints e da configuração CORS para desenvolvimento web.

O catálogo, descoberta, acompanhamento de títulos e recursos sociais ainda não fazem parte desta etapa.

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
dotnet run --project backend\Reelix.Api\Reelix.Api.csproj
```

No perfil local, a API fica disponível em `http://localhost:5035`. A especificação OpenAPI de desenvolvimento fica em `http://localhost:5035/openapi/v1.json`.

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

Para criar/atualizar as tabelas no banco configurado, execute a migration inicial:

```powershell
dotnet ef database update --project backend\Reelix.Infrastructure\Reelix.Infrastructure.csproj --startup-project backend\Reelix.Api\Reelix.Api.csproj
```

A tabela `public.user_accounts` tem RLS habilitado e não concede acesso direto aos papéis da API Data; o backend acessa o PostgreSQL com sua própria connection string.

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
- [Arquitetura](docs/architecture.md)
- [Especificações do produto](docs/specs/)
