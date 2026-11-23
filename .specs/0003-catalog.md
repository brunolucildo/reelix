# 0003 — Catálogo

> Spec retroativa: documenta o que foi definido e implementado antes da adoção do fluxo `.specs/`, no formato anterior ao `_template.md` (seções de escopo, requisitos, restrições e critérios). Specs novas, a partir da `0013`, seguem o template.

- **Status:** concluída (implementada; limites de validação em `.docs/ARCHITECTURE.md` §2 e §6 e em `.docs/QA.md`)
- **Slug:** catalog
- **Data:** 2026-10-25
- **Atualizado em:** 2026-11-19
- **Owner:** Produto Reelix
- **Relacionados:** `.docs/ARCHITECTURE.md`, `.docs/BUSINESS.md`, `.docs/QA.md`

## Escopo

Back-End, integração com provedores externos, persistência de referências de catálogo e contratos consumidos pelo aplicativo mobile.

## Contexto

O Reelix precisa consultar filmes, séries, temporadas, episódios e informações de lançamento.

O TMDB será o provedor principal proposto para essas informações, complementado pela OMDb quando houver necessidade concreta.

A aplicação deverá manter uma camada de abstração para que as regras de negócio não dependam diretamente dos formatos de resposta dos provedores externos.

## Requisitos Funcionais

1. Criar uma abstração para consultar dados de catálogo.
2. Implementar um adaptador para o TMDB.
3. Implementar um adaptador para a OMDb, utilizando somente operações documentadas e permitidas pelo serviço.
4. Permitir pesquisar filmes e séries.
5. Permitir consultar detalhes de um filme.
6. Permitir consultar detalhes de uma série.
7. Permitir consultar temporadas e episódios de uma série.
8. Permitir consultar datas de lançamento disponíveis.
9. Permitir consultar gêneros e metadados necessários para a interface.
10. Normalizar os dados recebidos para um modelo interno consistente.
11. Identificar cada título pelo provedor e pelo identificador externo correspondente.
12. Evitar duplicação de registros para o mesmo título e provedor.
13. Tratar títulos não encontrados, respostas incompletas, limites de requisição e indisponibilidade dos provedores.
14. Permitir paginação quando suportada pelo provedor.
15. Expor endpoints próprios do Reelix para pesquisa e consulta de catálogo.
16. Manter a origem dos dados externos identificável.
17. Implementar cache somente conforme as condições permitidas pelos provedores.
18. Permitir que os módulos de Tracking, Avaliações, Listas e Home referenciem títulos de maneira estável.
19. Normalizar fusos horários e datas sem inventar informações ausentes.
20. Tratar metadados não disponíveis sem interromper a experiência inteira.

### Contratos internos esperados

Os modelos normalizados devem contemplar, quando disponíveis:

- Identificador interno.
- Provedor e identificador externo.
- Tipo de conteúdo: filme ou série.
- Título e título original.
- Sinopse.
- Gêneros.
- Data de lançamento ou estreia.
- Imagem de pôster e imagem de fundo.
- Idioma original.
- Duração, quando aplicável.
- Temporadas e episódios, para séries.
- Datas de lançamento de episódios.
- Metadados de origem.

Os campos opcionais deverão permanecer opcionais quando o provedor não fornecer a informação.

## Requisitos Não Funcionais

- Proteger as credenciais dos provedores.
- Respeitar limites de requisição e condições contratuais.
- Aplicar timeout e tratamento de falhas de rede.
- Evitar consultas redundantes.
- Utilizar cache apenas quando autorizado.
- Registrar erros de integração sem incluir credenciais.
- Manter os adaptadores isolados das regras de negócio.
- Evitar dependência obrigatória de um único provedor quando houver alternativa viável.
- Não garantir atualização em tempo real sem suporte técnico do provedor.

## Você Deve

- Consultar a documentação oficial atual do TMDB e da OMDb antes de implementar cada integração.
- Utilizar variáveis de ambiente para as credenciais.
- Criar DTOs específicos para respostas externas e modelos internos normalizados.
- Implementar mapeamentos explícitos entre os provedores e o modelo do Reelix.
- Criar testes com respostas simuladas.
- Tratar rate limits e indisponibilidade de forma previsível.
- Documentar as regras de cache, atribuição e armazenamento aplicáveis.
- Garantir que um erro de um provedor não seja convertido em um título fictício.
- Definir índices para os identificadores externos utilizados nas consultas.
- Preservar a origem dos dados.
- Verificar os direitos de uso das imagens antes de armazená-las ou redistribuí-las.

## Você Não Deve

- Inventar endpoints ou parâmetros das APIs externas.
- Expor as chaves do TMDB ou da OMDb no aplicativo.
- Presumir que todas as informações estarão disponíveis nos dois provedores.
- Tratar a resposta de um provedor como se todos os campos fossem obrigatórios.
- Criar microserviços separados para os provedores.
- Armazenar indefinidamente todos os dados externos sem verificar as condições de uso.
- Substituir informações ausentes por dados inventados.
- Fazer chamadas externas desnecessárias a cada renderização de tela.

## Tecnologias (Stack)

- ASP.NET Core
- C#
- HttpClient e IHttpClientFactory
- PostgreSQL
- Entity Framework Core
- Npgsql
- TMDB API
- OMDb API
- xUnit
- Testes de integração

## Critérios de aceitação

- O backend consegue pesquisar filmes e séries por meio do provedor configurado.
- Os detalhes disponíveis podem ser consultados por endpoints próprios do Reelix.
- Temporadas, episódios e datas são retornados quando suportados pelo provedor.
- Os dados externos são normalizados para contratos internos.
- A origem e o identificador externo de cada título são preservados.
- Títulos inexistentes não são inventados.
- Falhas de integração e limites de requisição são tratados.
- As credenciais não aparecem nas respostas ou nos logs.
- As políticas de cache e atribuição aplicáveis são documentadas.
- Os testes com respostas simuladas passam.

## Resultado

Camada de catálogo implementada, com integrações isoladas, dados normalizados e contratos reutilizáveis pelos módulos do Reelix.

## Estado atual · 02/11/2026

Pesquisa, gêneros, detalhes, temporadas e episódios estão expostos pela API e usados
pelas telas mobile de busca, detalhes e tracking. TMDB é o provedor principal e OMDb
é opcional; não há fallback automático. O banco conserva referências externas estáveis,
não uma cópia integral de respostas/imagens. Descoberta pública e gêneros têm caches
temporários em memória descritos em [catalog-setup.md](0003-catalog.md) e
[home-discovery-setup.md](0004-home-discovery.md).

Provedores exigem credenciais de servidor válidas. Atribuição e direitos de uso devem
ser confirmados para o ambiente e finalidade de distribuição; não inferir datas ausentes.

---

## Anexo — Catálogo: configuração e teste local

> Migrado de `docs/catalog-setup.md` em 2026-11-19, sem alteração de conteúdo além dos links.


### Pré-requisitos

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

### Banco local

O catálogo persiste apenas referências estáveis (`provider`, `external_id`, `content_type`) para títulos, temporadas e episódios, associando IDs internos Reelix à origem. A busca e os detalhes não são armazenados como snapshots no banco. A descoberta pública usa cache em memória por 10 minutos e os gêneros TMDB por seis horas; esses caches são temporários e não equivalem a persistência de metadados. A migração cria `catalog_external_references`, com unicidade por provedor, identificador externo e tipo de conteúdo.

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

### Iniciar e abrir Swagger

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

### Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/v1/catalog/genres?type=movie&provider=tmdb&language=pt-BR` | Lista oficial de gêneros de filmes ou séries (`type=series`). |
| `GET` | `/api/v1/catalog/search?query=matrix&provider=tmdb&type=movie&page=1&language=pt-BR` | Pesquisa paginada; `type` é opcional (`movie` ou `series`). |
| `GET` | `/api/v1/catalog/{provider}/{contentType}/{externalId}?language=pt-BR` | Detalhes normalizados; provedor `tmdb` ou `omdb`. |
| `GET` | `/api/v1/catalog/{provider}/series/{externalSeriesId}/seasons/{seasonNumber}?language=pt-BR` | Temporada e episódios disponíveis. |

O identificador externo deve pertencer ao provedor indicado (por exemplo, ID numérico TMDB ou ID IMDb `tt...` para OMDb). Erros de validação retornam `400`, título/temporada inexistente retorna `404`, limite do provedor retorna `429` e indisponibilidade/configuração externa retorna `502`, `503` ou `504` com `ProblemDetails`. As credenciais nunca fazem parte do contrato de resposta.

### Provedores, atribuição e direitos

- TMDB usa os endpoints documentados de pesquisa, detalhes e temporadas, autenticados com o Read Access Token no cabeçalho Bearer.
- A lista oficial de gêneros TMDB também é consultada pelo backend e mantida em cache em memória por seis horas para alimentar o onboarding.
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
- [TMDB API — Movie genre list](https://developer.themoviedb.org/reference/genre-movie-list)
- [TMDB API — TV genre list](https://developer.themoviedb.org/reference/genre-tv-list)
- [TMDB FAQ — attribution and commercial use](https://developer.themoviedb.org/docs/faq)
- [OMDb API](https://www.omdbapi.com/)
