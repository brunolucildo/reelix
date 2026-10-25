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
