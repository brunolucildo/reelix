# Social

## Escopo

Front-End, Back-End e banco de dados, com integração aos módulos de Perfil, Tracking, Avaliações e Listas.

## Contexto

O Reelix deverá permitir que os usuários se conectem com amigos e compartilhem atividades relacionadas a filmes e séries.

O objetivo é criar uma experiência social inspirada em plataformas de acompanhamento de entretenimento, sem tornar automaticamente públicos os dados pessoais ou o histórico de visualização.

A primeira versão deverá priorizar amizades e atividades entre usuários autorizados. Seguidores públicos poderão ser adicionados posteriormente, caso sejam aprovados como requisito do produto.

## Requisitos Funcionais

1. Permitir pesquisar usuários por nome de exibição.
2. Restringir os resultados aos campos públicos e permitidos.
3. Permitir enviar solicitações de amizade.
4. Permitir aceitar ou recusar solicitações recebidas.
5. Permitir cancelar solicitações enviadas.
6. Permitir consultar a lista de amigos.
7. Permitir remover um amigo.
8. Permitir consultar solicitações pendentes.
9. Impedir solicitações duplicadas.
10. Impedir que um usuário envie solicitações de amizade para si mesmo.
11. Permitir consultar atividades compartilhadas por amigos.
12. Exibir atividades como filmes assistidos ou avaliações publicadas somente quando autorizadas.
13. Respeitar as configurações de privacidade de cada usuário.
14. Permitir acessar o perfil de um amigo respeitando as informações compartilháveis.
15. Tratar contas bloqueadas ou relações removidas, caso o bloqueio seja implementado nesta etapa.
16. Exibir estados vazios quando o usuário ainda não possuir amigos ou atividades disponíveis.
17. Permitir paginação da lista de amigos e do feed de atividades.
18. Garantir que a remoção de uma amizade não exclua o histórico de nenhum dos usuários.
19. Preparar os eventos necessários para o módulo de Notificações.
20. Evitar exibir atividades duplicadas no feed.

## Requisitos Não Funcionais

- Aplicar autorização no backend.
- Proteger dados pessoais.
- Evitar enumeração excessiva de usuários.
- Aplicar limites de requisições em pesquisas e solicitações de amizade.
- Garantir consistência das relações entre usuários.
- Evitar consultas N+1 no feed.
- Respeitar as regras de privacidade em todas as consultas.
- Não presumir que uma amizade concede acesso irrestrito ao histórico do outro usuário.

## Você Deve

- Criar um modelo de relacionamento entre usuários.
- Definir estados válidos para solicitações de amizade.
- Implementar regras para evitar relações duplicadas.
- Validar todas as operações no servidor.
- Consultar os módulos de Perfil, Tracking e Avaliações para compor atividades.
- Implementar paginação.
- Criar testes de autorização e isolamento de dados.
- Definir como a privacidade é aplicada a cada tipo de atividade.
- Documentar as regras de transição de estado das amizades.

## Você Não Deve

- Tornar o histórico público automaticamente.
- Permitir que um usuário altere a lista de amigos de terceiros.
- Expor e-mails ou outros identificadores privados em pesquisas públicas.
- Criar um sistema de mensagens privadas nesta etapa.
- Implementar seguidores públicos sem decisão explícita de produto.
- Permitir acesso a dados privados apenas porque existe uma amizade.
- Criar notificações completas neste módulo; ele deve fornecer os eventos necessários ao módulo próprio.

## Tecnologias (Stack)

- React Native
- Expo
- TypeScript
- TanStack Query
- ASP.NET Core
- C#
- PostgreSQL
- Entity Framework Core
- xUnit

## Critérios de aceitação

- O usuário consegue enviar, aceitar, recusar e cancelar solicitações.
- A lista de amigos é exibida corretamente.
- Não existem solicitações ou amizades duplicadas.
- O usuário não consegue alterar relações de terceiros.
- O feed apresenta somente atividades autorizadas.
- A remoção de uma amizade não exclui os dados dos envolvidos.
- Listas e feeds possuem paginação.
- Os testes de autorização e consistência passam.

## Resultado

Módulo social implementado, com amizades, solicitações e compartilhamento controlado de atividades.

## Decisões implementadas na versão inicial

- A busca não diferencia maiúsculas/minúsculas, exige ao menos dois caracteres e procura nomes que começam pelo termo informado. Ela retorna somente nome e ID Reelix de perfis públicos ou que habilitaram descoberta; e-mail, ID Supabase, biografia e contagens não são retornados.
- Uma relação canônica por par impede pedidos duplicados. Pedidos inversos enquanto existe uma solicitação pendente retornam conflito e exigem resposta explícita do destinatário. Solicitações recusadas, canceladas ou amizades removidas podem ser iniciadas novamente, respeitando o limite de escrita da API.
- Perfil público não ativa compartilhamento social automaticamente. O histórico e as avaliações só entram no feed entre amigos quando o respectivo consentimento está ativo; a consulta usa a data de ativação para não importar atividades antigas. Desativar a opção remove imediatamente as atividades das consultas seguintes.
- Favoritos de amigos só aparecem com consentimento próprio, com limite de 100 itens. Biografia só aparece no perfil contextual se o perfil também for público. Histórico, comentários privados, watchlist e listas privadas nunca são expostos pelo vínculo de amizade.
- O feed usa cursor composto por data/ID e lote fixo de 20 atividades. Amigos e solicitações também usam cursor; os cartões recebem dados de catálogo em lote, sem uma chamada por atividade.
- Eventos `friend.requested` e `friend.accepted` são gravados na mesma transação que a mudança de relação, com ID, versão, ator, destinatário e horário. A etapa Social não entrega notificações nem push.
- Busca e mutações de amizade têm limites por usuário. A tabela de relações e a tabela de eventos ficam com RLS habilitado e sem privilégios para os papéis da Data API; o backend usa sua conexão PostgreSQL.
- Bloqueio, seguidores públicos, chat e histórico retroativo não fazem parte desta entrega.

Os endpoints autenticados estão em `/api/v1/social`: `GET /users`, `POST /friend-requests`, `GET /friend-requests`, ações `PUT .../{id}/accept|decline`, cancelamento `DELETE /friend-requests/{id}`, `GET /friends`, perfil contextual `GET /friends/{accountId}/profile`, remoção `DELETE /friends/{relationshipId}` e `GET /feed`. A migration EF Core precisa ser aplicada ao banco antes de usar os endpoints.

## Estado atual · 02/11/2026

API, persistência/modelo, cliente mobile e telas de busca, solicitações, amizades,
perfil contextual e feed estão presentes no código. A migration deve ser aplicada e
validada em cada PostgreSQL alvo; a presença de endpoints não significa que o schema
do ambiente já foi atualizado. Busca/feed respeitam autorização e consentimentos.
Chat, bloqueio e seguidores públicos continuam fora do escopo.

## Redesign mobile · 03/11/2026

- A tela Amigos abre no feed cronológico; Feed, Solicitações, Amigos, Descobrir e Atividades são destinos exclusivos de navegação compacta por ícones.
- O cabeçalho oferece atalhos para adicionar e pesquisar pessoas. Busca por nome mantém os estados do relacionamento e as ações existentes; não há sugestões artificiais.
- Solicitações separa recebidas e enviadas. O indicador conta recebidas carregadas, com `+` quando existe outra página, sem afirmar um total que a API não retorna.
- O feed usa lista virtualizada, paginação por cursor, atualização por gesto e acesso ao perfil e aos detalhes de filmes/séries. Cartões visíveis consultam o catálogo com cache por título para nome e pôster reais. Episódios não possuem metadados de série suficientes nesse contrato e não geram um link presumido.
- Atividades apresenta o mesmo feed autorizado em formato compacto; filtros de assistidos e avaliações operam sobre as páginas carregadas, com opção de carregar mais.
- Carregamento, vazio e nova tentativa são distintos; erros permanecem no toast. Remoção de amizade usa confirmação no modal compartilhado, inclusive na versão web.
- O contrato atual não fornece nota, texto de comentário, avatar do autor ou conquistas no feed. Curtidas, comentários em publicações e salvamento de posts não foram simulados. O avatar do feed usa iniciais; o nome e o pôster vêm do catálogo quando disponíveis.
- Nenhuma regra de autorização, consentimento ou persistência foi alterada.

## Perfil social contextual · 03/11/2026

- O perfil usa cabeçalho com avatar/iniciais, nome, biografia permitida, estado de amizade e quatro abas: Visão geral, Atividades, Favoritos e Conquistas. Não é uma página pública anônima: os endpoints continuam autenticados.
- `GET /api/v1/social/friends/{accountId}/profile` acrescenta `relationshipStatus`, `relationshipId`, `requestSentByMe`, `isSelf`, `favoritesShared`, `watchHistoryShared` e `ratingsShared`. A identidade básica pode ser consultada pelo próprio titular, por participantes de uma relação aceita/pendente ou quando o perfil é público/descobrível. Biografia continua restrita a perfil público; descoberta não concede acesso a ela.
- Favoritos exigem amizade aceita e consentimento próprio, com limite existente de 100. A aplicação também elimina favoritos e flags sociais quando não existe amizade aceita. Os tipos de conteúdo são normalizados para `movie`/`series` na resposta.
- `GET /api/v1/social/friends/{accountId}/activities?cursor=...` filtra o autor no servidor, preservando cursor, amizade aceita e consentimentos com data de ativação. Um autor indisponível retorna uma coleção vazia, sem confirmar existência de conta.
- Foi corrigido o feed de avaliações, inclusive o feed geral: além do consentimento social, `Audience` deve ser `Public`. Avaliações privadas não podem gerar atividades visíveis. Comentários não entram nesta consulta.
- Adicionar, cancelar, aceitar, recusar e remover usam as mutações existentes. A tela invalida as consultas sociais após a operação; remoção limpa imediatamente o conteúdo compartilhado em cache. Solicitações duplicadas continuam bloqueadas pelas regras do domínio.
- A grade usa `TitlePosterCard` e dados reais do catálogo, filtros de filmes/séries e carregamento incremental em lotes de 12. Visão geral limita favoritos a quatro e atividades a duas. A lista de atividades é virtualizada e usa o mesmo `SocialActivityCard` de Amigos.
- Medalhas públicas usam `AchievementMedal`; somente a interação revela nome, descrição/critério textual e data no `ModalDialog`. Não são exibidos progresso ou conquistas privadas.
- Indicadores mostram apenas a quantidade disponível de favoritos explicitamente compartilhados e medalhas públicas; o limite de favoritos não representa uma contagem irrestrita do banco. Username, número de amigos, filmes assistidos e séries acompanhadas não são inferidos.
- Avatar personalizado não é fornecido por esse backend; o perfil usa iniciais. Notas em estrelas, comentários, números de temporada/episódio, início/fim de série e conquistas como eventos do feed também não estão disponíveis no contrato social. Não são simulados.
- Não houve alteração de schema ou nova migration. Aplicação/backend precisam ser atualizados junto com o cliente porque o perfil acrescenta campos de contrato.

Validação detalhada, limitações e classificação de cada requisito estão no
[relatório do redesign do perfil social](../implementation-report-social-profile.md).
