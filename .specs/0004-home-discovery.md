# 0004 — Home / Discovery

> Spec retroativa: documenta o que foi definido e implementado antes da adoção do fluxo `.specs/`, no formato anterior ao `_template.md` (seções de escopo, requisitos, restrições e critérios). Specs novas, a partir da `0013`, seguem o template.

- **Status:** concluída (implementada; limites de validação em `.docs/ARCHITECTURE.md` §2 e §6 e em `.docs/QA.md`)
- **Slug:** home-discovery
- **Data:** 2026-10-25
- **Atualizado em:** 2026-11-19
- **Owner:** Produto Reelix
- **Relacionados:** `.docs/ARCHITECTURE.md`, `.docs/BUSINESS.md`, `.docs/QA.md`

## Escopo

Front-End e Back-End, com integração ao catálogo, preferências do usuário e histórico de visualização quando disponível.

## Contexto

A Home será a principal área de navegação do Reelix.

O usuário deverá encontrar filmes e séries em áreas separadas, visualizar conteúdo relevante e descobrir novos títulos com base nas preferências e atividades disponíveis.

A primeira versão deverá priorizar uma experiência útil e previsível, sem exigir um sistema complexo de inteligência artificial.

## Requisitos Funcionais

1. Criar a tela inicial do aplicativo.
2. Separar a navegação de filmes e séries.
3. Exibir seções de conteúdo adequadas ao tipo selecionado.
4. Para séries, permitir seções como:
   - Próximos episódios.
   - Séries favoritas.
   - Séries em acompanhamento.
   - Sugestões personalizadas.
5. Para filmes, permitir seções como:
   - Filmes populares ou em destaque.
   - Próximos lançamentos, quando disponíveis.
   - Filmes favoritos.
   - Sugestões personalizadas.
6. Exibir pôster, título e metadados relevantes para cada item.
7. Permitir abrir a página de detalhes de um título.
8. Permitir que o usuário acesse a pesquisa do catálogo.
9. Considerar preferências do onboarding quando disponíveis.
10. Considerar o histórico de visualização quando disponível e autorizado.
11. Exibir estados de carregamento, ausência de conteúdo e erro.
12. Permitir atualização das seções quando apropriado.
13. Implementar paginação ou carregamento progressivo quando necessário.
14. Exibir recomendações básicas baseadas em gêneros e preferências.
15. Permitir que o usuário retome a navegação após retornar de uma página de detalhes.
16. Tratar usuários sem preferências ou histórico.
17. Exibir informações de lançamento conforme a disponibilidade dos dados do catálogo.

## Requisitos Não Funcionais

- Evitar consultas redundantes ao backend.
- Usar cache no cliente com invalidação adequada.
- Carregar imagens de forma eficiente.
- Manter a interface responsiva e acessível.
- Evitar que a indisponibilidade de uma seção bloqueie toda a Home.
- Não exibir conteúdo privado de outros usuários.
- Não fazer chamadas à OpenAI para cada renderização da tela.
- Não prometer datas de lançamento que não estejam disponíveis ou confirmadas na fonte utilizada.

## Você Deve

- Criar componentes reutilizáveis para listas horizontais e cartões de títulos.
- Utilizar TanStack Query para dados remotos.
- Definir contratos próprios para os dados exibidos na Home.
- Implementar estados de carregamento, erro e conteúdo vazio.
- Manter os dados de recomendação separados da apresentação visual.
- Permitir que a Home funcione com preferências vazias.
- Utilizar o catálogo como fonte dos metadados.
- Documentar as regras utilizadas para montar cada seção.
- Respeitar as regras de cache e os limites das APIs externas.

## Você Não Deve

- Implementar regras de acompanhamento de episódios neste módulo.
- Implementar o sistema de avaliações.
- Implementar relações de amizade.
- Criar um algoritmo complexo de recomendação sem necessidade comprovada.
- Gerar títulos fictícios para preencher listas vazias.
- Consultar a OpenAI continuamente para produzir as mesmas recomendações.
- Exibir como confirmada uma data estimada sem indicar sua natureza.

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
- TMDB API
- OMDb API

## Critérios de aceitação

- A Home apresenta áreas de filmes e séries.
- As seções exibem dados reais disponíveis no catálogo.
- O usuário consegue acessar os detalhes de um título.
- As preferências podem influenciar as sugestões.
- Usuários sem histórico conseguem utilizar a Home.
- Estados de carregamento, erro e conteúdo vazio são tratados.
- A falha de uma seção não impede o funcionamento das demais.
- As consultas respeitam as regras de cache e os limites dos provedores.
- Os testes relevantes passam.

## Resultado

Página inicial funcional, organizada por filmes e séries, com descoberta de conteúdo e recomendações iniciais baseadas em dados disponíveis.

## Estado atual · 02/11/2026

Home, busca e detalhes mobile estão integrados à API autenticada e ao catálogo. A Home
separa Filmes e Séries; recomendações são filtros determinísticos por gêneros salvos.
Feeds horizontais de descoberta deduplicam IDs e carregam páginas automaticamente,
com indicador de carregamento e retentativa explícita em caso de falha.

Continuar Assistindo mostra o progresso de todas as temporadas válidas da série,
não somente da temporada do episódio mais recente. Temporadas/episódios futuros são
excluídos; sem inventário, o total continua desconhecido. Veja
[home-discovery-setup.md](0004-home-discovery.md) para seções, cache e regras.

---

## Anexo — Home e descoberta: seções, regras e contratos

> Migrado de `docs/home-discovery-setup.md` em 2026-11-19, sem alteração de conteúdo além dos links.


Este documento registra como a Home monta as seções da primeira versão e quais dados
cada fluxo consulta. A Home não mantém recomendações próprias nem cria títulos fora do
catálogo.

### Seções

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

### Menu pessoal

#### Navegação e Ações Compactas · 03/11/2026

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

### Gamificação e conquistas

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

### Gerenciamento de listas

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

### API de descoberta

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

### Pesquisa e detalhes

- `mobile/src/app/search.tsx` pesquisa filmes ou séries pela operação autenticada
  `GET /api/v1/catalog/search`.
- `GET /api/v1/catalog/{provider}/series/{externalSeriesId}/seasons` lista as temporadas
  disponíveis no provedor, incluindo temporada especial quando informada.
- `mobile/src/app/title/[provider]/[contentType]/[externalId].tsx` consulta os detalhes
  normalizados do catálogo ao selecionar um cartão.
- O retorno da tela de detalhes usa a navegação existente e preserva o contexto da Home ou
  da busca.

---

## Anexo — Relatório dos cinco ajustes adicionais de UX

> Migrado de `docs/implementation-report-additional-ux.md` em 2026-11-19, sem alteração de conteúdo além dos links.


### Escopo e critérios de classificação

Este relatório cobre os cinco ajustes complementares de acompanhamento, conquistas e listas. Alterações anteriores presentes no mesmo worktree não são consideradas novas entregas deste relatório.

- **Concluída:** implementação com comportamento central verificado pelos testes indicados.
- **Implementada, mas não validada:** implementação com verificações automatizadas parciais, ainda sem validação integral da experiência/persistência real.
- Nenhum teste com mocks é apresentado como execução autenticada contra a API, PostgreSQL ou dispositivo.

| Ajuste | Classificação | Validação ainda necessária |
|---|---|---|
| 1. Episódios restantes da série inteira | Concluída | Conferência integrada com catálogo real e Home autenticada |
| 2. Medalhas no perfil e menu | Implementada, mas não validada | Layout, acessibilidade e modal sobre o drawer em Android/iOS |
| 3. Voltar no topo de `/lists` | Concluída | Conferência da pilha no aplicativo em execução |
| 4. Criação de listas sob demanda | Implementada, mas não validada | Teclado, modal e criação autenticada persistida após recarregar |
| 5. Card único, edição e exclusão contextual | Implementada, mas não validada | Operações autenticadas e isolamento dos dados em banco real |

### 1. Episódios restantes da série inteira

**Problema e causa:** o card de Continuar Assistindo consultava progresso da temporada identificada pelo episódio mais recente. O contrato de progresso da série não possuía inventário completo nem quantidade de restantes.

**Solução:**

- O serviço consulta as temporadas do catálogo e agrega episódios de todas as temporadas válidas, inclusive temporada 0/especiais.
- Temporadas negativas e datas de lançamento futuras são ignoradas. Episódios com data futura também não entram no denominador, mesmo quando já possuem registro de tracking.
- A agregação utiliza IDs únicos, evitando duplicatas dentro e entre temporadas. A contagem assistida usa a interseção com o tracking da conta autenticada.
- Episódios sem número convencional continuam identificados por GUID. Episódios sem data seguem a política já existente de disponibilidade: são incluídos. Isso preserva consistência, mas não permite garantir a data real de lançamento quando o provedor não a informa.
- Sem inventário conhecido, totais, restantes e percentual continuam desconhecidos; inventário vazio retorna zero disponíveis/restantes sem declarar conclusão.
- Uma temporada que não pode ser carregada gera erro, em vez de um total parcial apresentado como completo.
- O contrato inclui `pendingEpisodeCount`; o card utiliza progresso da série inteira. A temporada identificada continua apenas na legenda.
- Marcar/desmarcar episódios invalida o prefixo de tracking existente, incluindo o novo resumo. O provedor faz parte da chave de cache.

**Arquivos/componentes:**

- [TrackingContracts.cs](../backend/Reelix.Application/Tracking/TrackingContracts.cs), [TrackingService.cs](../backend/Reelix.Application/Tracking/TrackingService.cs).
- [TrackingCatalog.cs](../backend/Reelix.Infrastructure/Persistence/Tracking/TrackingCatalog.cs).
- [TrackingEndpoints.cs](../backend/Reelix.Api/Tracking/TrackingEndpoints.cs), [Tracking.http](../backend/Reelix.Api/Tracking.http).
- [tracking-api.ts](../mobile/src/services/tracking-api.ts), [use-tracking.ts](../mobile/src/hooks/use-tracking.ts), [tracked-series-section.tsx](../mobile/src/components/discovery/tracked-series-section.tsx).
- [TrackingServiceTests.cs](../backend/Reelix.UnitTests/Tracking/TrackingServiceTests.cs).

**Resultado verificado:** inventário de 10 + 12 + 8 episódios com 15 assistidos retorna **15 assistidos · 15 restantes**, com 50% de progresso. Os testes também cobrem marcar/desmarcar, conclusão em 100%, outra conta, especiais, duplicatas, metadados ausentes, futuros, inventário desconhecido/vazio e falha de temporada.

**Classificação:** **Concluída** quanto à regra de cálculo e integração no código, com testes automatizados aprovados. A aparência e atualização na Home autenticada não foram verificadas em execução real.

### 2. Conquistas somente como medalhas

**Problema e causa:** o perfil renderizava cards com título e descrição ao lado da medalha; o menu não mostrava as conquistas junto ao avatar.

**Solução:**

- Grade compartilhada exibe somente medalhas clicáveis; nomes permanecem nos rótulos de acessibilidade, não como texto visual na grade.
- Medalhas bloqueadas mantêm a distinção visual do componente existente.
- Selecionar abre modal com nome, descrição, critério, progresso, estado e datas disponíveis de desbloqueio/reativação.
- O contrato não possui um campo independente para critério. A descrição real do catálogo, que informa a regra, é reutilizada; não foi criada uma regra fictícia.
- O menu mostra uma grade compacta de conquistas ativas ao lado do avatar, com carregamento e retentativa em caso de erro.
- O modal compartilhado possui fechamento, rolagem, área segura e tratamento de teclado. O viewport de toast foi reutilizado dentro dos modais para manter erros visíveis.

**Arquivos/componentes:**

- [achievement-grid.tsx](../mobile/src/components/gamification/achievement-grid.tsx), reutilizando [achievement-medal.tsx](../mobile/src/components/gamification/achievement-medal.tsx).
- [profile.tsx](../mobile/src/app/profile.tsx), [user-drawer.tsx](../mobile/src/components/navigation/user-drawer.tsx).
- [modal-dialog.tsx](../mobile/src/components/modal-dialog.tsx), [toast-provider.tsx](../mobile/src/providers/toast-provider.tsx).

**Resultado e validação:** TypeScript e lint aprovados. Um script de sessão transpila o componente real e exercita handlers com hooks/elementos simulados: ausência de textos fora do modal, conquistas desbloqueadas/bloqueadas, abertura, conteúdo e fechamento. O wireframe abriu no navegador, carregou os SVGs e apresentou os detalhes simulados.

**Classificação:** **Implementada, mas não validada** integralmente. O teste de handlers não renderiza React Native. Permanecem pendentes proporções em telas pequenas, leitor de tela e apresentação de um modal de detalhes sobre o modal do drawer em Android/iOS.

### 3. Voltar no topo de `/lists`

**Problema e causa:** o botão compartilhado de voltar estava ao final do conteúdo rolável.

**Solução:** o mesmo `BackButton` usado nas demais telas foi colocado no início do cabeçalho, antes do título, dentro da área segura. Mantém ícone e alvo de toque do Design System. O componente usa `router.back()`, sem redirecionamento fixo para Home.

Não foi encontrado um cabeçalho genérico de telas internas que precisasse ser conectado. O `AppHeader` existente é específico da Home; seus atalhos não foram duplicados em listas.

**Arquivos/componentes:** [lists.tsx](../mobile/src/app/lists.tsx), reutilizando [back-button.tsx](../mobile/src/components/navigation/back-button.tsx), sem novo contrato de navegação.

**Resultado e validação:** inspeção do componente compartilhado confirmou retorno pela pilha. O script de handlers verificou que o botão aparece antes dos campos; TypeScript e lint aprovados.

**Classificação:** **Concluída** quanto à posição, reutilização e ligação com a navegação existente. Não houve teste de navegação E2E entre telas reais.

### 4. Criação de listas somente sob demanda

**Problema e causa:** nome, descrição e ações de criação ficavam permanentemente na tela.

**Solução:**

- O formulário inicia oculto e é aberto pelo ícone existente de adicionar lista.
- Criação/edição usam o mesmo modal, com cancelar, fechar e campos preservados em caso de falha.
- Nome obrigatório normalizado de 1 a 80 caracteres e descrição de até 500 seguem as regras existentes.
- Durante a persistência, campos e fechamento ficam bloqueados para evitar interrupção e envio concorrente pela interface.
- Sucesso fecha o formulário, atualiza os caches e seleciona a lista salva. Erros de API permanecem explícitos em toast sobre o modal.
- Nenhum endpoint ou mecanismo de persistência novo foi introduzido.

**Arquivos/componentes:** [lists.tsx](../mobile/src/app/lists.tsx), [modal-dialog.tsx](../mobile/src/components/modal-dialog.tsx), [toast-provider.tsx](../mobile/src/providers/toast-provider.tsx).

**Resultado e validação:** script de handlers aprovado para estado inicialmente oculto, abertura, validação, loading, falha mantendo o rascunho, callback de criação e invalidação de cache. TypeScript e lint aprovados.

**Classificação:** **Implementada, mas não validada** de ponta a ponta. Criação autenticada, recarga posterior e interação com teclado no dispositivo não foram executadas.

### 5. Lista privada em uma única área visual

**Problema e causa:** seleção/ações e apresentação dos conteúdos eram renderizadas em áreas separadas, fazendo uma mesma lista parecer dois cards.

**Solução:**

- Cada lista possui um card com nome, descrição disponível, quantidade combinada de filmes/séries e menu de três pontos.
- Ao selecionar, conteúdos e paginação são exibidos dentro do próprio card, mantendo remoção e reordenação.
- O menu abre edição preenchida ou confirmação de exclusão nomeando explicitamente a lista.
- Editar modifica apenas nome/descrição pelo endpoint existente; não substitui os itens.
- Cancelar não exclui. Falha de exclusão mantém a confirmação; sucesso atualiza seleção, paginação e caches.
- A exclusão usa apenas o endpoint da lista personalizada. Não dispara operações de Favoritos, Watchlist ou catálogo.
- O backend existente filtra a lista pelo proprietário. A configuração de persistência aplica cascade aos vínculos da lista e restrict às referências do catálogo.

**Arquivos/componentes:** [lists.tsx](../mobile/src/app/lists.tsx) e modal compartilhado. Não foi necessário alterar os endpoints nem a configuração de banco das listas.

**Resultado e validação:** script de handlers aprovado para card unificado, edição preservando os conteúdos simulados, confirmação nomeada, cancelamento, falha, exclusão e invalidação. Testes existentes de serviço e configuração de listas aprovados, incluindo `Apply_CascadesOwnedListItemsButPreservesCatalogReferences`.

**Classificação:** **Implementada, mas não validada** integralmente. Os testes verificam regras/configuração e callbacks; não substituem uma exclusão em PostgreSQL real seguida da conferência de Favoritos, Watchlist e catálogo.

### Validações executadas

- **Backend:** 44 testes aprovados, zero falhas, nos filtros `TrackingServiceTests`, `UserListsServiceTests`, `ListsPersistenceConfigurationTests` e `AuthenticationEndpointTests`. O projeto da API também foi compilado pelas referências dos testes.
- **Mobile:** `npx tsc --noEmit` e `npm run lint` aprovados.
- **UX com mocks:** script de sessão aprovado para listas e medalhas; não é uma suíte E2E nem teste de renderização React Native.
- **Wireframes:** sintaxe JavaScript verificada e estados de detalhes/criação/edição/exclusão exercitados no navegador, com assets de medalhas carregados.
- **Patch:** `git diff --check` sem erros.
- Avisos de SDK .NET preview e de consulta indisponível ao feed NuGet (`NU1900`) não impediram compilação/testes. A consulta de vulnerabilidades não foi validada por esse feed.

### Documentação e pendências operacionais

- Atualizados [especificação de tracking](0005-tracking.md), [guia da Home](0004-home-discovery.md) e [wireframes](../.docs/design/wireframes/reelix-visual-wireframes.html).
- **Sincronização documental em 02/11/2026:** README, arquitetura, especificações dos módulos, guias de configuração e páginas de planos foram revisados para separar código presente, dependências por ambiente e funcionalidades ainda não implementadas. O wireframe central agora cobre 31 telas em 11 módulos, incluindo Social, Notificações, Gamificação e a lacuna explícita de Widgets.
- **Recompilar e reiniciar a API antes de testar o mobile:** o novo contrato contém `pendingEpisodeCount`. Uma instância antiga não entrega o novo contrato. Não foram interrompidos processos do usuário.
- A agregação consulta temporadas sequencialmente, respeitando o contexto de persistência scoped. Séries longas podem exigir muitas chamadas ao provedor; não foi introduzido cache adicional nem realizado teste de carga.
- Nenhuma migração, dependência ou dado artificial foi adicionado à aplicação. Não houve commit.
- Para homologar os itens pendentes: abrir perfil/menu em Android e iOS; criar, editar e excluir uma lista autenticada; recarregar; conferir manutenção dos conteúdos e dos relacionamentos independentes; marcar/desmarcar episódios em temporadas diferentes e conferir a Home.
