# 0002 — Onboarding

> Spec retroativa: documenta o que foi definido e implementado antes da adoção do fluxo `.specs/`, no formato anterior ao `_template.md` (seções de escopo, requisitos, restrições e critérios). Specs novas, a partir da `0013`, seguem o template.

- **Status:** concluída (implementada; limites de validação em `.docs/ARCHITECTURE.md` §2 e §6 e em `.docs/QA.md`)
- **Slug:** onboarding
- **Data:** 2026-10-25
- **Atualizado em:** 2026-11-19
- **Owner:** Produto Reelix
- **Relacionados:** `.docs/ARCHITECTURE.md`, `.docs/BUSINESS.md`, `.docs/QA.md`

## Escopo

Front-End, Back-End e persistência das preferências iniciais do usuário.

## Contexto

Após o primeiro acesso, o Reelix deverá permitir que o usuário indique seus gostos para personalizar a página inicial e as recomendações.

O onboarding deverá aproveitar os dados disponíveis no módulo de catálogo. Não deverá criar uma segunda fonte de dados para filmes, séries ou gêneros.

## Requisitos Funcionais

1. Identificar se o usuário concluiu o onboarding.
2. Apresentar uma sequência de telas de boas-vindas e configuração inicial.
3. Permitir selecionar gêneros de interesse.
4. Permitir pesquisar e selecionar filmes favoritos.
5. Permitir pesquisar e selecionar séries favoritas.
6. Permitir avançar e retornar entre as etapas sem perder as seleções já realizadas.
7. Salvar as preferências no backend.
8. Permitir concluir o onboarding sem preencher todas as opções, desde que as regras mínimas de negócio sejam atendidas.
9. Permitir pular etapas opcionais.
10. Registrar a conclusão do onboarding.
11. Permitir editar as preferências posteriormente pelo módulo de perfil.
12. Utilizar as preferências para alimentar o módulo de Home e Discovery.
13. Exibir estados de carregamento, falha de comunicação e sucesso ao salvar.
14. Evitar que o usuário seja obrigado a repetir o onboarding após reabrir o aplicativo.
15. Permitir retomar o fluxo caso a aplicação seja encerrada antes da conclusão.

## Requisitos Não Funcionais

- Salvar as preferências de forma consistente.
- Evitar requisições desnecessárias ao catálogo.
- Manter a interface responsiva e acessível.
- Aplicar autorização no backend.
- Não expor as preferências de um usuário a outros usuários sem autorização.
- Garantir que falhas de comunicação não resultem em uma conclusão falsa do onboarding.
- Evitar dependência de chamadas à OpenAI para concluir o fluxo.

## Você Deve

- Consultar o estado do onboarding no backend.
- Reutilizar as telas e componentes de pesquisa do catálogo quando disponíveis.
- Criar contratos para leitura e atualização das preferências.
- Persistir as escolhas associadas ao usuário autenticado.
- Implementar uma experiência de seleção simples e visual.
- Tratar o onboarding como um fluxo retomável.
- Garantir que a conclusão seja persistida no servidor.
- Testar usuários novos, usuários com onboarding incompleto e usuários que já concluíram o fluxo.
- Permitir que preferências vazias sejam tratadas adequadamente pelos módulos consumidores.

## Você Não Deve

- Implementar cadastro ou login neste módulo.
- Duplicar os dados do catálogo no banco de preferências.
- Exigir seleção de um grande número de títulos.
- Bloquear permanentemente o uso do aplicativo caso o usuário pule etapas opcionais.
- Criar recomendações complexas nesta etapa.
- Enviar preferências a serviços externos sem necessidade ou transparência.
- Tornar públicas as preferências automaticamente.

## Tecnologias (Stack)

- React Native
- Expo
- TypeScript
- Expo Router
- TanStack Query
- ASP.NET Core
- C#
- PostgreSQL
- Entity Framework Core
- xUnit

## Critérios de aceitação

- Um usuário novo consegue iniciar o onboarding.
- Os gêneros e títulos selecionados podem ser salvos.
- O usuário consegue retornar a uma etapa sem perder as escolhas persistidas.
- O estado do onboarding permanece correto após reiniciar o aplicativo.
- O backend rejeita alterações feitas por usuários não autorizados.
- Títulos inexistentes ou referências inválidas são rejeitados.
- O usuário consegue concluir o fluxo sem selecionar favoritos, quando permitido pelas regras definidas.
- Os dados ficam disponíveis para os módulos que dependem das preferências.
- Os testes automatizados relevantes passam.

## Resultado

Fluxo inicial de personalização implementado, com preferências persistidas e disponíveis para a experiência personalizada do Reelix.

## Estado atual · 02/11/2026

O fluxo mobile retomável, gêneros oficiais TMDB, seleção de filmes/séries do catálogo,
rascunho/conclusão no backend e modo de edição de preferências estão implementados.
O perfil usa essas mesmas preferências, sem duplicar referências do catálogo.

A operação requer migrations aplicadas no banco e credenciais válidas do Supabase/TMDB.
Configuração e limites de payload estão em
[onboarding-profile-setup.md](0002-onboarding.md).

---

## Anexo — Onboarding e perfil: configuração e contratos

> Migrado de `docs/onboarding-profile-setup.md` em 2026-11-19, sem alteração de conteúdo além dos links.


### Escopo implementado

O mobile oferece onboarding retomável após autenticação, seleção de gêneros oficiais TMDB, busca de filmes/séries no catálogo e edição do perfil próprio. O backend usa o `sub` do JWT Supabase validado para resolver a conta; o cliente não escolhe o proprietário dos dados.

As preferências guardam IDs de gênero TMDB e IDs internos de títulos já presentes em `catalog_external_references`. Metadados do catálogo não são duplicados. Avatares fixos são escolhidos em uma faixa horizontal rolável com nove desenhos originais e opção de iniciais; a chave é persistida no perfil. Upload continua fora do escopo. Estatísticas e conquistas são fornecidas pelo módulo de Gamificação, descrito em [11-gamification.md](0011-gamification.md); histórico pertence ao Tracking.

### Banco de dados

`20261103133240_AddFixedAvatars` acrescenta `avatar_key` opcional e validado;
`20261103134321_ExpandFixedAvatars` amplia a restrição para aceitar também `robot`,
`vampire` e `maestro`, preservando as seis escolhas anteriores e os dados existentes.
Ambas foram aplicadas ao banco configurado nesta validação. Outros ambientes precisam
receber essas migrations antes de usar os novos avatares. RLS e privilégios não foram alterados.

`GET/PUT /api/v1/profile` inclui `avatarKey`. Omitir esse campo preserva a seleção;
`initials` limpa a escolha. URLs e chaves desconhecidas são rejeitadas. A faixa horizontal
tem cartões de tamanho estável com avatar, nome/tema e check de seleção, com rótulos de rádio acessíveis.
Catálogo de opções: [avatares Reelix](../mobile/assets/brand/avatars/README.md).

A migration `20261031014300_AddUserOnboardingAndProfiles` cria `user_profiles`, `user_preferences`, `user_genre_preferences` e `user_favorite_titles`. As tabelas têm RLS habilitado e privilégios revogados para os papéis do Supabase Data API; somente a API acessa o PostgreSQL com a connection string do servidor.

Em 30/10/2026, a migration foi aplicada ao PostgreSQL Supabase selecionado por `ConnectionStrings:Reelix` nas User Secrets e confirmada no histórico EF. Outros ambientes continuam independentes. Para desenvolvimento local, configure `ConnectionStrings:Reelix` para um PostgreSQL local e aplique usando o procedimento de [autenticação](0001-authentication.md#banco-e-verificações). Não execute migrations contra outros ambientes sem revisão e autorização.

A seleção de gêneros consulta TMDB usando `Catalog:Tmdb:AccessToken`, somente no backend. O resultado é cacheado em memória por seis horas; configure a credencial conforme [catálogo](0003-catalog.md).

### API

Todas as rotas abaixo exigem `Authorization: Bearer <access-token>` válido do Supabase.

| Método | Rota | Descrição |
| --- | --- | --- |
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

No perfil, `displayName` aceita até 50 caracteres e `biography` até 280. Os campos são opcionais. `profileIsPublic`, `favoritesArePublic` e `watchHistoryIsPublic` começam como `false`. A rota de perfil público não retorna e-mail; favoritos só são incluídos quando `favoritesArePublic` está ligado. A existência de tracking não torna o histórico automaticamente público: o compartilhamento social respeita os consentimentos e as regras descritos em [09-social.md](0009-social.md).

O perfil apresenta as conquistas em uma grade somente de medalhas. Ao selecionar uma medalha, abre detalhes com nome, descrição/critério, progresso, estado e datas disponíveis. Conquistas bloqueadas têm distinção visual. O menu lateral exibe medalhas ativas junto ao avatar. Isso não altera as regras de privacidade do histórico ou das conquistas.

### Mobile e atribuição

O onboarding salva o rascunho no backend ao avançar ou pular uma etapa. A conclusão só é marcada depois da resposta bem-sucedida da API; o app consulta esse estado ao entrar e encaminha contas incompletas para retomar o fluxo.

- O fluxo tem 5 etapas (`ETAPA X DE 5`) com barra de progresso. Gêneros usam abas Filmes/Séries e contador `X de 20`.
- As etapas de favoritos buscam somente o tipo da etapa (`movie` ou `series`), exigem ao menos 2 caracteres, mostram `X de 10 selecionados` e oferecem “Remover seleções desta etapa”.
- A Home mostra as contagens reais de gêneros, filmes e séries e o atalho “Editar preferências”. O Perfil também leva a esse modo. `onboarding?edit=1` reabre o fluxo a partir de Gêneros e termina com “Salvar preferências”.
- O Perfil reúne Informações (nome até 50, biografia até 280), Privacidade, Preferências e Créditos em uma única tela.

#### Feedback de erros

Erros de API, carregamento, busca, salvamento e logout aparecem em toast (`mobile/src/providers/toast-provider.tsx`, hooks `useToast` e `useErrorToast`). O toast aparece no topo, fecha automaticamente após 5 s e pode ser fechado pelo botão “Fechar aviso”. Telas bloqueantes mantêm “Tentar novamente”. Validação por campo, avisos de limite de seleção e mensagens de sucesso continuam inline, junto ao controle.

O Perfil inclui o logo aprovado e o aviso de atribuição oficial do TMDB na seção Créditos. A API TMDB gratuita é destinada a uso não comercial; confirme licença com TMDB antes de usar em produto cujo objetivo principal seja gerar receita.

### Verificações

Na raiz do repositório:

```powershell
dotnet test Reelix.sln
```

Em `mobile`:

```powershell
npx tsc --noEmit
npx expo lint
```
