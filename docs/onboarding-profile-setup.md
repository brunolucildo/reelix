# Onboarding e Perfil — configuração e contratos

## Escopo implementado

O mobile oferece onboarding retomável após autenticação, seleção de gêneros oficiais TMDB, busca de filmes/séries no catálogo e edição do perfil próprio. O backend usa o `sub` do JWT Supabase validado para resolver a conta; o cliente não escolhe o proprietário dos dados.

As preferências guardam IDs de gênero TMDB e IDs internos de títulos já presentes em `catalog_external_references`. Metadados do catálogo não são duplicados. Upload de avatar, estatísticas, conquistas e histórico continuam fora deste incremento. O perfil usa um avatar padrão local; `avatarUrl` permanece `null`.

## Banco de dados

A migration `20261031014300_AddUserOnboardingAndProfiles` cria `user_profiles`, `user_preferences`, `user_genre_preferences` e `user_favorite_titles`. As tabelas têm RLS habilitado e privilégios revogados para os papéis do Supabase Data API; somente a API acessa o PostgreSQL com a connection string do servidor.

Em 30/10/2026, a migration foi aplicada ao PostgreSQL Supabase selecionado por `ConnectionStrings:Reelix` nas User Secrets e confirmada no histórico EF. Outros ambientes continuam independentes. Para desenvolvimento local, configure `ConnectionStrings:Reelix` para um PostgreSQL local e aplique usando o procedimento de [autenticação](authentication-setup.md#banco-e-verificações). Não execute migrations contra outros ambientes sem revisão e autorização.

A seleção de gêneros consulta TMDB usando `Catalog:Tmdb:AccessToken`, somente no backend. O resultado é cacheado em memória por seis horas; configure a credencial conforme [catálogo](catalog-setup.md).

## API

Todas as rotas abaixo exigem `Authorization: Bearer <access-token>` válido do Supabase.

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/v1/catalog/genres?type=movie&provider=tmdb&language=pt-BR` | Gêneros oficiais de filmes ou séries (`type=series`). |
| `GET` | `/api/v1/onboarding` | Preferências e etapa persistida da conta autenticada. |
| `PUT` | `/api/v1/onboarding/progress` | Salva a etapa atual, gêneros e títulos favoritos. |
| `POST` | `/api/v1/onboarding/complete` | Registra a conclusão; gêneros e favoritos são opcionais. |
| `GET` | `/api/v1/profile` | Consulta o perfil próprio. Não retorna e-mail nem identidade de autenticação. |
| `PUT` | `/api/v1/profile` | Atualiza nome, biografia e privacidade. |
| `GET` | `/api/v1/profile/{accountId}` | Consulta perfil de outra conta apenas se ela o tornou público. Perfil privado ou inexistente retorna `404`. |

Exemplo de progresso:

```json
{
  "currentStep": "favoriteMovies",
  "movieGenreIds": [28, 18],
  "seriesGenreIds": [18],
  "favoriteMovieIds": ["972f0e2f-ae08-4514-9c46-9e45ea4be8b6"],
  "favoriteSeriesIds": []
}
```

As etapas aceitas são `welcome`, `genres`, `favoriteMovies`, `favoriteSeries` e `review`. Cada lista aceita IDs únicos: até 20 gêneros e até 10 filmes/séries favoritos. IDs de gêneros são validados contra a lista TMDB; títulos devem existir no catálogo e corresponder ao tipo indicado. Dados inválidos retornam `400` com `ProblemDetails`.

No perfil, `displayName` aceita até 50 caracteres e `biography` até 280. Os campos são opcionais. `profileIsPublic`, `favoritesArePublic` e `watchHistoryIsPublic` começam como `false`. A rota de perfil público não retorna e-mail; favoritos só são incluídos quando `favoritesArePublic` está ligado. A configuração de histórico é salva, mas histórico não é exposto enquanto o módulo Tracking não existir.

## Mobile e atribuição

O onboarding salva o rascunho no backend ao avançar ou pular uma etapa. A conclusão só é marcada depois da resposta bem-sucedida da API; o app consulta esse estado ao entrar e encaminha contas incompletas para retomar o fluxo.

O Perfil inclui o logo aprovado e o aviso de atribuição oficial do TMDB na seção Créditos. A API TMDB gratuita é destinada a uso não comercial; confirme licença com TMDB antes de usar em produto cujo objetivo principal seja gerar receita.

## Verificações

Na raiz do repositório:

```powershell
dotnet test Reelix.sln
```

Em `mobile`:

```powershell
npx tsc --noEmit
npx expo lint
```
