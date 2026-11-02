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
