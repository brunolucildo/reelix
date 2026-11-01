# Home e descoberta

Este documento registra como a Home monta as seções da primeira versão e quais dados
cada fluxo consulta. A Home não mantém recomendações próprias nem cria títulos fora do
catálogo.

## Seções

| Tela | Seção | Fonte e regra |
|---|---|---|
| Filmes | Em destaque | TMDB, filmes ordenados por popularidade. |
| Filmes | Seus filmes favoritos | Favoritos do usuário autenticado, com metadados consultados no catálogo. |
| Filmes | Sugestões para você | Filmes populares filtrados pelos gêneros de filmes salvos no onboarding. |
| Filmes | Próximos lançamentos | Filmes com data de lançamento entre hoje (UTC) e os próximos 12 meses, quando o provedor informar a data. |
| Séries | Em destaque | TMDB, séries ordenadas por popularidade. |
| Séries | Suas séries favoritas | Favoritos do usuário autenticado, com metadados consultados no catálogo. |
| Séries | Séries em acompanhamento | Resumo do total no endpoint autenticado de tracking, com acesso à tela de acompanhamento. |
| Séries | Sugestões para você | Séries populares filtradas pelos gêneros de séries salvos no onboarding. |
| Séries | Estreias de séries | Séries com primeira data de exibição entre hoje (UTC) e os próximos 12 meses. Não é um calendário de próximos episódios. |

Sem gêneros salvos, a seção personalizada é substituída por um atalho para editar preferências;
as demais seções continuam disponíveis. Favoritos são limitados aos oito primeiros títulos
da categoria nesta Home. O endpoint atual de tracking retorna IDs internos e contagens, mas não
nomes nem referências de catálogo; por isso a Home mostra o total e encaminha à tela própria,
sem exibir IDs ou fazer uma consulta de detalhes por série.

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
No app, TanStack Query mantém os feeds por 5 minutos e carrega novas páginas somente quando
o usuário solicita. Cada feed, favoritos e preferências é consultado separadamente: uma falha
não remove as demais seções. Dados privados usam os endpoints autenticados existentes e o
cache local é limpo ao encerrar a sessão.

## Pesquisa e detalhes

- `mobile/src/app/search.tsx` pesquisa filmes ou séries pela operação autenticada
  `GET /api/v1/catalog/search`.
- `mobile/src/app/title/[provider]/[contentType]/[externalId].tsx` consulta os detalhes
  normalizados do catálogo ao selecionar um cartão.
- O retorno da tela de detalhes usa a navegação existente e preserva o contexto da Home ou
  da busca.
