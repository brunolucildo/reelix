# Home e descoberta

Este documento registra como a Home monta as seções da primeira versão e quais dados
cada fluxo consulta. A Home não mantém recomendações próprias nem cria títulos fora do
catálogo.

## Seções

Séries aparece antes de Filmes e é a seleção inicial. Na aba Séries, acompanhamento
vem antes de destaques, favoritos, sugestões e estreias. Filmes começa por Quero assistir,
acima de Filmes em destaque.

| Tela | Seção | Fonte e regra |
| --- | --- | --- |
| Filmes | Quero assistir | Até oito filmes reais da watchlist, por inclusão recente; percorre páginas mistas até reunir filmes ou chegar ao fim. Atalho para a watchlist completa. |
| Filmes | Em destaque | TMDB, filmes ordenados por popularidade. |
| Filmes | Seus filmes favoritos | Favoritos do usuário autenticado, com metadados consultados no catálogo. |
| Filmes | Sugestões para você | Filmes populares filtrados pelos gêneros de filmes salvos no onboarding. |
| Filmes | Próximos lançamentos | Filmes com data de lançamento entre hoje (UTC) e os próximos 12 meses, quando o provedor informar a data. |
| Séries | Em destaque | TMDB, séries ordenadas por popularidade. |
| Séries | Suas séries favoritas | Favoritos do usuário autenticado, com metadados consultados no catálogo. |
| Séries | Continuar Assistindo | Até oito séries, por registro mais recente, com episódio mais recente como legenda e progresso/episódios restantes calculados sobre todas as temporadas válidas. |
| Séries | Começar a Assistir | Favoritos e watchlist ainda não acompanhados; abre os detalhes da série sem criar tracking fictício. |
| Séries | Calendário | Próximos episódios datados das 12 séries acompanhadas mais recentes; oferece filtros Todos, Estreia e Final. |
| Séries | Sugestões para você | Séries populares filtradas pelos gêneros de séries salvos no onboarding. |
| Séries | Estreias de séries | Séries com primeira data de exibição entre hoje (UTC) e os próximos 12 meses. Não é um calendário de próximos episódios. |

Ao reunir páginas de descoberta, a Home exibe cada ID de título uma única vez por seção,
preservando a ordem da primeira ocorrência. Isso evita cards repetidos quando as páginas
do catálogo se sobrepõem, sem alterar a paginação do provedor.
As listas horizontais carregam a próxima página automaticamente ao se aproximar do fim,
sem botão “Mostrar mais”. Durante a consulta, exibem um indicador no fim da lista.
Uma falha mantém os títulos carregados, mostra toast e oferece “Tentar novamente”,
sem iniciar novas tentativas automáticas até a ação do usuário.

Sem gêneros salvos, a seção personalizada é substituída por um atalho para editar preferências;
as demais seções continuam disponíveis. Favoritos são limitados aos oito primeiros títulos
da categoria nesta Home. O resumo de tracking inclui, de forma opcional, `provider`,
`externalId`, `latestEpisodeId` e `knownSeasonNumbers`, preservando IDs e contagens existentes.
As temporadas já conhecidas são cruzadas com o episódio registrado mais recentemente para
identificar a temporada do último episódio, usada somente na legenda. Pôster e nome vêm
dos detalhes do catálogo; assistidos, restantes e percentual vêm do progresso da série
inteira, consultando todas as temporadas válidas do provedor, inclusive especiais.
IDs repetidos contam uma vez; temporadas/episódios datados no futuro são excluídos.
Episódios sem data seguem a regra existente: são disponíveis, sem inventar uma estreia.
Sem inventário, exibe somente a contagem real do histórico e informa que o total é desconhecido.
Registrar ou remover episódios invalida todas as consultas de acompanhamento e gamificação.
O calendário consulta os metadados de temporada do catálogo e considera as duas temporadas regulares
mais recentes, além de temporadas com estreia futura informada. Datas não fornecidas pelo catálogo
não são estimadas; temporadas especiais só entram quando o provedor as identifica como futuras.

## Menu pessoal

### Navegação e Ações Compactas · 03/11/2026

- O componente compartilhado de Voltar apresenta Home ao lado, com área de toque de 44 px e rótulos acessíveis. Home usa a rota autenticada quando existe sessão e a página inicial pública quando não existe, sem contornar as proteções do Expo Router ou o onboarding.
- O retorno sem histórico tem fallback para a página inicial. Autenticação e primeira etapa do onboarding também reutilizam a navegação; a Home não recebe um botão de voltar para ela mesma.
- Em atividades sociais, pôster e conteúdo de filmes/séries abrem os detalhes diretamente; o botão separado Ver título foi removido. Episódios continuam sem link presumido quando falta a referência da série.
- Os detalhes do catálogo têm ícones para Quero assistir, favoritos, lista pessoal e acompanhamento. Os ícones apresentam tooltip no hover/foco, rótulo acessível, estado selecionado e bloqueio durante salvamento.
- Watchlist e favoritos usam `POST /api/v1/title-status` para consultar estado e os `PUT`/`DELETE` existentes em `/api/v1/watchlist/{titleId}` e `/api/v1/favorites/{titleId}`. Alterações invalidam o cache das coleções e da Home.
- O seletor de listas consulta `GET /api/v1/lists`, permite paginação e usa `PUT /api/v1/lists/{listId}/titles/{titleId}`. Também permite criar uma lista com `POST /api/v1/lists` e adicionar o título, mantendo feedback de sucesso inline e falhas no toast.
- Filmes mantêm marcar/desmarcar como assistido e a elegibilidade de avaliações, agora por ícone. Para séries, o atalho leva às temporadas/episódios; não chama o endpoint de filmes nem marca uma série inteira sem confirmação.
- A imagem real de destaque (ou pôster quando não existe backdrop) dissolve no fundo com `expo-linear-gradient` compatível com SDK 58. O degradê usa os tokens de fundo, sem transformar a imagem em um card arredondado.

Verificações web: seção Quero assistir antes de Filmes em destaque, adição temporária
à watchlist com resposta 200, exibição do título na Home e remoção dessa inclusão de
teste com resposta 204. Estado original preservado. Seletor de listas vazio validado
com dados reais; criação de lista temporária respondeu 201, inclusão do título 200 e
exclusão somente da lista de teste 204. Clique direto da atividade e controles de filme/série conferidos.
TypeScript e lint fazem parte dos gates; não representa validação de builds nativos.

O avatar abre o menu lateral com perfil, medalhas de conquistas ativas junto ao avatar,
favoritos, listas, watchlist, acompanhamento, avaliações, conquistas, amigos, configurações e saída.
Tocar numa medalha abre os detalhes da conquista. Busca e notificações ficam no header
da Home; o sino mostra a contagem real de itens não lidos. Os atalhos redundantes e a saída
direta foram removidos da Home. Favoritos, listas e watchlist usam a mesma tela com
`section=favorites`, `section=lists` ou `section=watchlist`. O menu usa áreas seguras,
rolagem, botão de fechar, toque fora e retorno do dispositivo; sair exige confirmação.

Configurações organiza links para os controles reais existentes. Minhas avaliações consulta
`GET /api/v1/ratings/mine?page=1&pageSize=20`, filtrado pela conta autenticada, incluindo notas
privadas, com paginação de até 50 itens. Salvar ou remover notas atualiza essa coleção.

## Gamificação e conquistas

O JWT `sub` identifica o usuário Supabase, não a chave interna de `user_accounts`.
`GamificationService` resolve a conta com `IUserAccountRepository.GetOrCreateAsync` antes
das operações pessoais de dashboard, estatísticas, preferências e ranking. Badges públicos
continuam recebendo o ID interno do perfil consultado. Tratar os dois IDs como iguais
causava falha no dashboard e impedia também as conquistas do perfil.

Dashboard e conquistas usam os mesmos dados reais, com contrato validado por Zod. Falha de
atualização preserva dados anteriores; falha inicial mostra nova tentativa e toast. Estatísticas
do período e ranking falham independentemente. Sessão expirada solicita novo login.
As migrações existentes até `AddGamification` precisam estar aplicadas em cada ambiente antes
do uso. Reinicie a API recompilada para carregar alterações de contrato/código.

Meu Perfil apresenta uma grade apenas de medalhas, incluindo bloqueadas com opacidade
reduzida. Tocar abre detalhes com nome, descrição/critério, progresso, estado e datas
disponíveis. O menu lateral reutiliza essa grade, em tamanho compacto e somente com
conquistas ativas, ao lado do avatar; a visibilidade pública não limita o próprio titular.

## Gerenciamento de listas

`/lists` usa o botão compartilhado de voltar no topo e respeita a pilha do Expo Router.
O header da Home não é reutilizado aqui: seus atalhos de descoberta não pertencem a
esta tela. A área segura e os tokens seguem as demais telas.

Na seção Minhas listas, o ícone de adicionar abre um modal; o formulário fica oculto
na entrada. Criar/editar valida nome (1–80 caracteres após normalização) e descrição
(até 500), bloqueia envios durante a persistência e permite cancelar antes de salvar.
Falhas preservam o formulário e aparecem no toast compartilhado, inclusive sobre o modal.

Cada lista tem um único card com nome, descrição, total de filmes/séries e menu de
três pontos. Selecionar o card expande seus conteúdos paginados no mesmo local.
Editar altera somente nome e descrição. Excluir exige confirmação com o nome da lista
e remove apenas a lista e seus vínculos; catálogo, Favoritos e Watchlist não são removidos.
Os endpoints existentes continuam responsáveis pela autorização e persistência.
Após alterações, resumos, conteúdos e estados de títulos são invalidados.

## API de descoberta

`GET /api/v1/catalog/discover` exige autenticação e aceita:

- `type`: `movie` ou `series`.
- `section`: `popular` ou `upcoming`.
- `genreIds`: zero ou mais IDs oficiais do TMDB; no máximo 20 IDs positivos e distintos.
- `page`: página entre 1 e 100.
- `provider`: `tmdb` nesta operação.
- `language`: idioma dos metadados; o app envia `pt-BR`.

A resposta usa `CatalogDiscoveryPage`, com IDs internos Reelix e referências do provedor
necessárias para abrir detalhes. Resultados populares usam popularidade decrescente.
Resultados futuros usam a data real do provedor dentro de uma janela de 12 meses; datas
ausentes não são estimadas. Consultas de `upcoming` em séries significam estreias, não
episódios pendentes.

O adaptador TMDB mantém respostas públicas de descoberta em cache em memória por 10 minutos.
No app, TanStack Query mantém os feeds por 5 minutos e carrega novas páginas automaticamente
perto do fim da lista horizontal. Cada feed, favoritos e preferências é consultado separadamente: uma falha
não remove as demais seções. Dados privados usam os endpoints autenticados existentes e o
cache local é limpo ao encerrar a sessão.

## Pesquisa e detalhes

- `mobile/src/app/search.tsx` pesquisa filmes ou séries pela operação autenticada
  `GET /api/v1/catalog/search`.
- `GET /api/v1/catalog/{provider}/series/{externalSeriesId}/seasons` lista as temporadas
  disponíveis no provedor, incluindo temporada especial quando informada.
- `mobile/src/app/title/[provider]/[contentType]/[externalId].tsx` consulta os detalhes
  normalizados do catálogo ao selecionar um cartão.
- O retorno da tela de detalhes usa a navegação existente e preserva o contexto da Home ou
  da busca.
