# 0001 — Autenticação

> Spec retroativa: documenta o que foi definido e implementado antes da adoção do fluxo `.specs/`, no formato anterior ao `_template.md` (seções de escopo, requisitos, restrições e critérios). Specs novas, a partir da `0013`, seguem o template.

- **Status:** concluída (implementada; limites de validação em `.docs/ARCHITECTURE.md` §2 e §6 e em `.docs/QA.md`)
- **Slug:** authentication
- **Data:** 2026-10-25
- **Atualizado em:** 2026-11-19
- **Owner:** Produto Reelix
- **Relacionados:** `.docs/ARCHITECTURE.md`, `.docs/BUSINESS.md`, `.docs/QA.md`

## Escopo

Front-End, Back-End, integração com Supabase Auth e banco de dados PostgreSQL.

## Contexto

Precisamos implementar os fluxos de autenticação do Reelix para permitir que os usuários criem suas contas, acessem a plataforma e recuperem o acesso quando necessário.

O Supabase Auth será responsável pela autenticação. O backend ASP.NET Core será responsável pela validação dos tokens e pela autorização das operações da aplicação.

A aplicação não deverá implementar um sistema próprio de armazenamento de senhas.

## Requisitos Funcionais

1. Implementar uma página de cadastro com e-mail e senha.
2. Implementar uma página de login com e-mail e senha.
3. Validar os formulários no cliente antes de enviar as solicitações.
4. Utilizar o SDK oficial do Supabase para os fluxos de autenticação suportados.
5. Tratar os estados de carregamento, sucesso e erro dos formulários.
6. Exibir mensagens genéricas de erro de autenticação, sem revelar desnecessariamente se determinado e-mail existe.
7. Implementar logout e limpar os dados de sessão mantidos no cliente conforme as capacidades do SDK.
8. Implementar recuperação de senha utilizando o fluxo do Supabase Auth.
9. Implementar confirmação de e-mail quando essa opção estiver habilitada no projeto.
10. Restaurar a sessão válida quando o usuário reabrir o aplicativo.
11. Encaminhar usuários não autenticados para as telas públicas apropriadas.
12. Proteger as telas privadas do aplicativo.
13. Validar os tokens recebidos nos endpoints protegidos do backend.
14. Criar ou localizar o registro interno do usuário na primeira autenticação válida.
15. Tratar contas desabilitadas, tokens expirados e sessões inválidas.
16. Disponibilizar um endpoint protegido para consultar o estado básico da conta autenticada.
17. Não exigir conclusão do onboarding para autenticar. O onboarding será tratado pelo módulo próprio.

## Requisitos Não Funcionais

- Não armazenar senhas na base de dados do Reelix.
- Não expor chaves privadas do Supabase no aplicativo.
- Não utilizar a chave `service_role` no mobile.
- Validar criptograficamente os tokens no backend, verificando assinatura, emissor, audiência e validade conforme a configuração do provedor.
- Não confiar apenas na decodificação de um JWT.
- Armazenar informações de sessão de acordo com as recomendações de segurança da plataforma.
- Utilizar HTTPS em ambientes não locais.
- Aplicar limites de requisições em operações sensíveis.
- Não registrar senhas, tokens ou dados sensíveis nos logs.
- Manter mensagens de erro consistentes e compreensíveis.

## Você Deve

- Utilizar Supabase Auth como provedor de identidade.
- Implementar telas acessíveis de cadastro, login e recuperação de senha.
- Criar middleware ou configuração de autenticação no ASP.NET Core.
- Associar o identificador estável do usuário no Supabase ao registro interno do Reelix.
- Aplicar autorização no backend, não apenas na navegação do aplicativo.
- Criar testes de autenticação e autorização.
- Documentar as variáveis de ambiente necessárias.
- Tratar adequadamente erros de rede e indisponibilidade do provedor.
- Garantir que o logout não deixe a interface aparentando estar autenticada.
- Manter os fluxos de autenticação independentes das regras específicas dos demais módulos.

## Você Não Deve

- Implementar autenticação paralela usando ASP.NET Identity.
- Criar armazenamento próprio de senhas.
- Inventar endpoints do Supabase.
- Confiar em identificadores de usuário enviados pelo cliente para definir a identidade autenticada.
- Armazenar tokens em locais inseguros.
- Expor informações que permitam enumerar contas.
- Implementar funcionalidades de perfil, amigos ou preferências neste módulo.
- Permitir acesso a dados privados apenas porque a tela correspondente está protegida no frontend.

## Tecnologias (Stack)

- React Native
- Expo
- TypeScript
- Expo Router
- Supabase Auth e SDK oficial
- ASP.NET Core
- C#
- JWT Bearer Authentication
- PostgreSQL
- Entity Framework Core
- xUnit

## Critérios de aceitação

- Um usuário consegue criar uma conta.
- Um usuário consegue autenticar e encerrar a sessão.
- A recuperação de senha segue o fluxo configurado no Supabase.
- Sessões válidas podem ser restauradas de forma segura.
- Tokens inválidos, expirados ou adulterados são rejeitados pela API.
- Endpoints protegidos rejeitam solicitações não autenticadas.
- Um usuário não consegue se passar por outro alterando um identificador enviado pelo cliente.
- O aplicativo trata erros de autenticação e indisponibilidade de rede.
- Nenhuma chave privada é incluída no aplicativo.
- Os testes automatizados relevantes passam.

## Resultado

Fluxos de autenticação implementados e integrados ao backend, com sessões tratadas de forma segura e identidade consistente para os demais módulos do Reelix.

## Estado atual · 02/11/2026

Cadastro, login, confirmação de e-mail conforme configuração do Supabase, recuperação,
callback/deep link, redefinição de senha, restauração de sessão e logout estão presentes
no mobile. A API valida o JWT e associa `sub` à conta interna; telas protegidas não
substituem autorização no servidor.

O uso depende das chaves públicas no app, configuração segura do backend, URLs de
callback e migrations aplicadas no banco do ambiente. A página `dev-auth.html` e Swagger
estão limitados a `Development`. Consulte [authentication-setup.md](0001-authentication.md).

---

## Anexo — Configuração local de autenticação, API e banco

> Migrado de `docs/authentication-setup.md` em 2026-11-19, sem alteração de conteúdo além dos links.


O app usa Supabase Auth para cadastro, login, confirmação de e-mail, recuperação de senha e logout. A API ASP.NET Core valida os access tokens JWT usando os metadados/chaves públicas do projeto Supabase e mantém o registro interno do usuário no PostgreSQL.

### Aplicativo mobile

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

### Projeto Supabase

- Habilite o provedor de e-mail e senha.
- Defina se a confirmação de e-mail será obrigatória; o app trata ambos os casos.
- Configure e monitore os limites de requisição do Supabase Auth para cadastro, recuperação de senha e envio de e-mails.
- Em **Authentication → URL Configuration**, permita o callback exato `reelix://auth/callback` em **Additional Redirect URLs** para builds do app. Para desenvolvimento com Expo Go, permita também a URL de callback gerada pelo Expo para a máquina/sessão atual.
- Configure **Site URL** para um destino válido do produto. Enquanto o projeto for nativo e não houver site HTTPS publicado, use `reelix://auth/callback` como fallback; não deixe `http://localhost:3000`, pois links inválidos/expirados podem voltar para esse endereço em vez de abrir o app. Se houver cliente web publicado, use sua origem HTTPS real como Site URL e mantenha o callback nativo na allowlist.
- O formulário mobile envia `redirectTo` para `reelix://auth/callback?flow=recovery` ao solicitar reset. Confira o template **Reset Password**: use `{{ .ConfirmationURL }}` ou, em um template de link próprio, `{{ .RedirectTo }}`; não fixe `localhost:3000` no template. O callback do app aceita PKCE (`code`), OTP (`token_hash` + `type`) e sessão direta (`access_token` + `refresh_token`).
- Links de recuperação são de uso único e expiram. Após ajustar Site URL, allowlist e template, solicite um link novo. Se ele já chegar como `otp_expired`, o provedor de e-mail pode estar pré-validando/consumindo o link; consulte as opções de OTP por código ou página de confirmação intermediária antes de reenviar repetidamente.
- Para a validação por JWKS da API, prefira chaves de assinatura assimétricas ES256/RS256. Se o projeto ainda emitir JWTs HS256 com o segredo legado, configure `Supabase__JwtSecret` somente no ambiente seguro do backend. Nunca é a `service_role`.

### API e PostgreSQL

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

Para testar endpoints autenticados pelo Swagger local, use a página auxiliar de desenvolvimento em `http://localhost:5035/dev-auth.html` para fazer login diretamente no Supabase e obter um access token temporário. A API não recebe a senha. Consulte [como iniciar o Swagger e autorizar as chamadas](0003-catalog.md#iniciar-e-abrir-swagger). Esse recurso está disponível somente em `Development`.

### Banco e verificações

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
