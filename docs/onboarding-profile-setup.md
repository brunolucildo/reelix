# Onboarding e Perfil — configuração e contratos

## Escopo implementado

O mobile oferece onboarding retomável após autenticação, seleção de gêneros oficiais TMDB, busca de filmes/séries no catálogo e edição do perfil próprio. O backend usa o `sub` do JWT Supabase validado para resolver a conta; o cliente não escolhe o proprietário dos dados.

As preferências guardam IDs de gênero TMDB e IDs internos de títulos já presentes em `catalog_external_references`. Metadados do catálogo não são duplicados. Upload/persistência de avatar continua fora do escopo atual; o perfil usa um avatar padrão local quando `avatarUrl` é `null`. Estatísticas e conquistas são fornecidas pelo módulo de Gamificação, descrito em [11-gamification.md](specs/11-gamification.md); histórico pertence ao Tracking.

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

`catalog_external_references` grava `provider` e `content_type` em minúsculas (`tmdb`, `movie`, `series`). A validação de favoritos compara o tipo nesse formato. uma comparação com `Movie`/`Series` rejeitava qualquer título como se ele não estivesse no catálogo; esse problema foi corrigido. As mensagens de `ProblemDetails` são exibidas no toast do app e estão em português, por exemplo: “Um ou mais filmes favoritos não estão no catálogo.”

No perfil, `displayName` aceita até 50 caracteres e `biography` até 280. Os campos são opcionais. `profileIsPublic`, `favoritesArePublic` e `watchHistoryIsPublic` começam como `false`. A rota de perfil público não retorna e-mail; favoritos só são incluídos quando `favoritesArePublic` está ligado. A existência de tracking não torna o histórico automaticamente público: o compartilhamento social respeita os consentimentos e as regras descritos em [09-social.md](specs/09-social.md).

O perfil apresenta as conquistas em uma grade somente de medalhas. Ao selecionar uma medalha, abre detalhes com nome, descrição/critério, progresso, estado e datas disponíveis. Conquistas bloqueadas têm distinção visual. O menu lateral exibe medalhas ativas junto ao avatar. Isso não altera as regras de privacidade do histórico ou das conquistas.

## Mobile e atribuição

O onboarding salva o rascunho no backend ao avançar ou pular uma etapa. A conclusão só é marcada depois da resposta bem-sucedida da API; o app consulta esse estado ao entrar e encaminha contas incompletas para retomar o fluxo.

- O fluxo tem 5 etapas (`ETAPA X DE 5`) com barra de progresso. Gêneros usam abas Filmes/Séries e contador `X de 20`.
- As etapas de favoritos buscam somente o tipo da etapa (`movie` ou `series`), exigem ao menos 2 caracteres, mostram `X de 10 selecionados` e oferecem “Remover seleções desta etapa”.
- A Home mostra as contagens reais de gêneros, filmes e séries e o atalho “Editar preferências”. O Perfil também leva a esse modo. `onboarding?edit=1` reabre o fluxo a partir de Gêneros e termina com “Salvar preferências”.
- O Perfil reúne Informações (nome até 50, biografia até 280), Privacidade, Preferências e Créditos em uma única tela.

### Feedback de erros

Erros de API, carregamento, busca, salvamento e logout aparecem em toast (`mobile/src/providers/toast-provider.tsx`, hooks `useToast` e `useErrorToast`). O toast aparece no topo, fecha automaticamente após 5 s e pode ser fechado pelo botão “Fechar aviso”. Telas bloqueantes mantêm “Tentar novamente”. Validação por campo, avisos de limite de seleção e mensagens de sucesso continuam inline, junto ao controle.

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
