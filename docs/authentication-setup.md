# Configuração local — autenticação

O app usa Supabase Auth para cadastro, login, confirmação de e-mail, recuperação de senha e logout. A API ASP.NET Core valida os access tokens JWT usando os metadados/chaves públicas do projeto Supabase e mantém o registro interno do usuário no PostgreSQL.

## Aplicativo mobile

1. Copie `mobile/.env.example` para `mobile/.env`.
2. Preencha `EXPO_PUBLIC_SUPABASE_URL` e `EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY` com os dados públicos do projeto Supabase.
3. Configure `EXPO_PUBLIC_API_URL` para a URL alcançável pela instalação do app. No navegador e no simulador iOS, use `http://localhost:5035`; em um emulador Android, use `http://10.0.2.2:5035`; em um dispositivo físico, use o endereço local da máquina de desenvolvimento. Para emuladores e dispositivos, inicie a API escutando na rede com `--urls http://0.0.0.0:5035`. Não use HTTP fora do desenvolvimento.
4. Instale as dependências e inicie o Expo:

   ```powershell
   Set-Location mobile
   npm install
   npx expo start
   ```

Nas telas de entrada, cadastro e redefinição, os campos de senha têm o botão “Mostrar/Ocultar”, com área de toque de 44 px e rótulos acessíveis. Erros por campo aparecem inline. Falhas do Supabase Auth, como credenciais inválidas e links expirados, aparecem em toast sem revelar se o e-mail existe. Mensagens de sucesso continuam inline.

A chave `publishable` (ou a antiga chave `anon`) é pública e pode compor o bundle do app. Nunca use `service_role`, chaves secretas ou a chave privada de assinatura JWT no mobile. Os tokens de sessão são mantidos no `expo-secure-store` em iOS/Android; a versão web, se utilizada, recorre ao armazenamento local do navegador e não deve ser tratada como limite de segurança.

## Projeto Supabase

- Habilite o provedor de e-mail e senha.
- Defina se a confirmação de e-mail será obrigatória; o app trata ambos os casos.
- Configure e monitore os limites de requisição do Supabase Auth para cadastro, recuperação de senha e envio de e-mails.
- Em **Authentication → URL Configuration**, permita o callback exato `reelix://auth/callback` em **Additional Redirect URLs** para builds do app. Para desenvolvimento com Expo Go, permita também a URL de callback gerada pelo Expo para a máquina/sessão atual.
- Configure **Site URL** para um destino válido do produto. Enquanto o projeto for nativo e não houver site HTTPS publicado, use `reelix://auth/callback` como fallback; não deixe `http://localhost:3000`, pois links inválidos/expirados podem voltar para esse endereço em vez de abrir o app. Se houver cliente web publicado, use sua origem HTTPS real como Site URL e mantenha o callback nativo na allowlist.
- O formulário mobile envia `redirectTo` para `reelix://auth/callback?flow=recovery` ao solicitar reset. Confira o template **Reset Password**: use `{{ .ConfirmationURL }}` ou, em um template de link próprio, `{{ .RedirectTo }}`; não fixe `localhost:3000` no template. O callback do app aceita PKCE (`code`), OTP (`token_hash` + `type`) e sessão direta (`access_token` + `refresh_token`).
- Links de recuperação são de uso único e expiram. Após ajustar Site URL, allowlist e template, solicite um link novo. Se ele já chegar como `otp_expired`, o provedor de e-mail pode estar pré-validando/consumindo o link; consulte as opções de OTP por código ou página de confirmação intermediária antes de reenviar repetidamente.
- Para a validação por JWKS da API, prefira chaves de assinatura assimétricas ES256/RS256. Se o projeto ainda emitir JWTs HS256 com o segredo legado, configure `Supabase__JwtSecret` somente no ambiente seguro do backend. Nunca é a `service_role`.

## API e PostgreSQL

Configure estas variáveis no ambiente da API, no gerenciador de segredos da hospedagem ou com o User Secrets do .NET:

| Variável | Obrigatória | Uso |
| --- | --- | --- |
| `Supabase__Url` | Sim | URL base do projeto; usada para verificar emissor e metadados JWKS |
| `Supabase__Audience` | Não | Audiência validada; padrão `authenticated` |
| `Supabase__JwtSecret` | Apenas legado HS256 | Segredo de assinatura legado, exclusivamente no backend |
| `ConnectionStrings__Reelix` | Sim | Connection string PostgreSQL |
| `Cors__AllowedOrigins__0` | Para clientes web | Origem HTTPS exata do frontend; origens locais do Expo web já estão em `appsettings.Development.json` |

O app nativo não depende de CORS. Para web, a API aceita somente as origens explicitamente configuradas; em produção, defina `Cors__AllowedOrigins__0` (e índices adicionais, se necessário) com a origem HTTPS exata do frontend. Não use curingas.

Para desenvolvimento, inicie a API em HTTP local e aponte o app para a URL correspondente:

```powershell
$env:Supabase__Url = "https://your-project-ref.supabase.co"
$env:ConnectionStrings__Reelix = "Host=localhost;Port=5432;Database=reelix;Username=postgres;Password=<senha-local>"
dotnet run --project backend\Reelix.Api\Reelix.Api.csproj --launch-profile http
```

Não salve valores reais em `appsettings.json`, `mobile/.env.example`, no Git ou em logs. O backend nunca usa um identificador enviado pelo cliente como identidade autenticada; ele obtém `sub` do JWT validado.

Para testar endpoints autenticados pelo Swagger local, use a página auxiliar de desenvolvimento em `http://localhost:5035/dev-auth.html` para fazer login diretamente no Supabase e obter um access token temporário. A API não recebe a senha. Consulte [como iniciar o Swagger e autorizar as chamadas](catalog-setup.md#iniciar-e-abrir-swagger). Esse recurso está disponível somente em `Development`.

## Banco e verificações

Para revisar as alterações antes de qualquer aplicação, gere um script idempotente.
O comando compila/consulta o modelo, mas não conecta ao banco:

```powershell
dotnet tool restore
$env:Supabase__Url = "https://migration-script.invalid"
$env:ConnectionStrings__Reelix = "Host=localhost;Database=reelix_migration;Username=postgres;Password=not-used"
dotnet tool run dotnet-ef migrations script --idempotent --project backend\Reelix.Infrastructure\Reelix.Infrastructure.csproj --startup-project backend\Reelix.Api\Reelix.Api.csproj --output artifacts\reelix-migrations.sql
```

Revise o SQL gerado e confirme o ambiente de destino antes de executar qualquer
migration. Para desenvolvimento local autorizado, com o banco iniciado e a connection
string apontando explicitamente para ele, aplique as migrations:

```powershell
dotnet tool run dotnet-ef database update --project backend\Reelix.Infrastructure\Reelix.Infrastructure.csproj --startup-project backend\Reelix.Api\Reelix.Api.csproj
```

Nunca use connection string de produção ou de banco compartilhado sem autorização
explícita. O CI também publica o script idempotente como artefato temporário para revisão;
isso não aplica migrations automaticamente.

Verificações disponíveis:

```powershell
dotnet restore Reelix.sln --source https://api.nuget.org/v3/index.json
dotnet test Reelix.sln --no-restore
Set-Location mobile
npx tsc --noEmit
npx expo lint
```

A suíte backend inclui um teste de integração que exige Docker Desktop em execução.
Ele sobe um PostgreSQL descartável, aplica as migrations e remove o container ao final;
não usa nem altera o banco Supabase configurado nos User Secrets.
