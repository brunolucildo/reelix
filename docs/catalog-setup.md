# Catálogo — configuração e teste local

## Pré-requisitos

- .NET SDK compatível com o `TargetFramework` da solução.
- PostgreSQL local para desenvolvimento e a ferramenta `dotnet-ef` da mesma versão major do EF Core do projeto.
- Uma credencial de leitura da API TMDB; a chave OMDb é opcional para usar o adaptador OMDb.
- Uma sessão autenticada válida do projeto Supabase configurado para a API.

As credenciais dos provedores ficam somente no backend. Configure-as com User Secrets durante o desenvolvimento:

```powershell
dotnet user-secrets set "Catalog:Tmdb:AccessToken" "<TMDB Read Access Token>" --project backend\Reelix.Api\Reelix.Api.csproj
dotnet user-secrets set "Catalog:Omdb:ApiKey" "<OMDb API key>" --project backend\Reelix.Api\Reelix.Api.csproj
```

O adaptador OMDb pode ser usado sem credencial TMDB e vice-versa; a credencial ausente gera erro explícito `503` quando aquele provedor é solicitado. Não salve chaves em `appsettings*.json`, no aplicativo mobile ou no controle de versão.

## Banco local

O catálogo persiste apenas referências estáveis (`provider`, `external_id`, `content_type`) para títulos, temporadas e episódios, associando IDs internos Reelix à origem. Não há cache de respostas ou armazenamento de sinopses/imagens. A migração cria `catalog_external_references`, com unicidade por provedor, identificador externo e tipo de conteúdo.

**Estado atual:** a migração `20261026193000_AddExternalCatalogReferences` foi aplicada ao Supabase configurado nas User Secrets e verificada. Para outros ambientes, configure a conexão do ambiente de destino e aplique as migrações conforme o processo de release aprovado.

Configure primeiro `ConnectionStrings:Reelix` para apontar a um PostgreSQL **local**. Por exemplo, no PowerShell da mesma sessão em que executará a migração e iniciará a API:

```powershell
$env:ConnectionStrings__Reelix = "Host=localhost;Database=reelix;Username=postgres;Password=<senha-local>"
```

Não execute a migração contra produção ou contra um banco compartilhado sem revisão e autorização explícitas. O repositório fixa o `dotnet-ef` compatível com EF Core 10.x; restaure a ferramenta e, com o banco local disponível, aplique as migrações:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend\Reelix.Infrastructure\Reelix.Infrastructure.csproj --startup-project backend\Reelix.Api\Reelix.Api.csproj
```

A API também requer `Supabase:Url` para validar os tokens de acesso. Use o ambiente de desenvolvimento e as configurações locais existentes para esse valor.

## Iniciar e abrir Swagger

Na raiz do repositório:

```powershell
dotnet run --project backend\Reelix.Api\Reelix.Api.csproj --launch-profile http
```

- Swagger UI: <http://localhost:5035/swagger>
- OpenAPI JSON: <http://localhost:5035/openapi/v1.json>

Swagger e OpenAPI são habilitados apenas em `Development`. Os endpoints de catálogo exigem autenticação. Na interface, use **Authorize** e informe um JWT válido emitido pelo projeto Supabase configurado, sem acrescentar o prefixo `Bearer`.

Para autorizar chamadas com sua própria conta:

1. Abra <http://localhost:5035/dev-auth.html>.
2. Informe a chave publishable (ou a chave `anon` legada) do mesmo projeto Supabase configurado na API, junto com seu e-mail e senha.
3. Copie o access token retornado e cole-o em **Authorize** no Swagger, sem acrescentar `Bearer`.

O navegador envia as credenciais diretamente ao Supabase Auth; a API local não recebe nem armazena sua senha. O token de acesso é temporário e expira conforme a configuração de sessão do Supabase. A página auxiliar e seu endpoint de configuração existem somente em `Development`; não use chaves `service_role` ou secret.

Se a porta `5035` já estiver ocupada, encerre/reinicie a instância antiga da API ou inicie esta instância em outra porta com `--urls http://localhost:5036`.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/v1/catalog/search?query=matrix&provider=tmdb&type=movie&page=1&language=pt-BR` | Pesquisa paginada; `type` é opcional (`movie` ou `series`). |
| `GET` | `/api/v1/catalog/{provider}/{contentType}/{externalId}?language=pt-BR` | Detalhes normalizados; provedor `tmdb` ou `omdb`. |
| `GET` | `/api/v1/catalog/{provider}/series/{externalSeriesId}/seasons/{seasonNumber}?language=pt-BR` | Temporada e episódios disponíveis. |

O identificador externo deve pertencer ao provedor indicado (por exemplo, ID numérico TMDB ou ID IMDb `tt...` para OMDb). Erros de validação retornam `400`, título/temporada inexistente retorna `404`, limite do provedor retorna `429` e indisponibilidade/configuração externa retorna `502`, `503` ou `504` com `ProblemDetails`. As credenciais nunca fazem parte do contrato de resposta.

## Provedores, atribuição e direitos

- TMDB usa os endpoints documentados de pesquisa, detalhes e temporadas, autenticados com o Read Access Token no cabeçalho Bearer.
- OMDb usa operações documentadas de pesquisa (`s`), detalhes (`i`) e temporadas (`i` + `Season`). Não há fallback automático entre provedores.
- A resposta OMDb não expõe imagens: a documentação do serviço informa que sua Poster API é restrita a patronos. Para TMDB, a aplicação deve cumprir a atribuição vigente; a documentação oficial exige o logo TMDB e o aviso “This product uses the TMDB API but is not endorsed or certified by TMDB.” em uma área de créditos/sobre. O backend retorna a origem para permitir essa identificação no cliente.
- A API TMDB gratuita é destinada a uso não comercial e exige atribuição. Confirme licença comercial com TMDB antes de usar em um produto cujo objetivo principal seja gerar receita.
- Este módulo não adiciona cache de respostas nem cópia dos metadados. O banco conserva somente os identificadores externos para dar identidade interna estável aos títulos. Reavalie retenção, termos e direitos de imagem antes de adicionar cache ou redistribuir imagens.

Documentação oficial consultada:

- [TMDB API — Getting Started](https://developer.themoviedb.org/reference/intro/getting-started)
- [TMDB API — Movie search](https://developer.themoviedb.org/reference/search-movie)
- [TMDB API — TV search](https://developer.themoviedb.org/reference/search-tv)
- [TMDB API — Movie details](https://developer.themoviedb.org/reference/movie-details)
- [TMDB API — TV details](https://developer.themoviedb.org/reference/tv-series-details)
- [TMDB API — TV season details](https://developer.themoviedb.org/reference/tv-season-details)
- [TMDB FAQ — attribution and commercial use](https://developer.themoviedb.org/docs/faq)
- [OMDb API](https://www.omdbapi.com/)
