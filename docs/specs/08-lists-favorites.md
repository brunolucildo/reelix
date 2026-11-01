# Listas e favoritos

## Escopo

Front-End, Back-End e banco de dados, com integração ao catálogo e ao sistema de autenticação.

## Contexto

O usuário precisa organizar os conteúdos que deseja assistir, os títulos favoritos e coleções personalizadas.

O módulo deverá separar o conceito de favorito da lista de conteúdos pendentes, permitindo que um mesmo título seja favorito e também esteja na watchlist.

## Requisitos Funcionais

1. Permitir adicionar filmes e séries aos favoritos.
2. Permitir remover filmes e séries dos favoritos.
3. Permitir adicionar títulos à watchlist.
4. Permitir remover títulos da watchlist.
5. Permitir consultar os favoritos.
6. Permitir consultar a watchlist.
7. Permitir criar listas personalizadas.
8. Permitir definir nome e descrição de uma lista.
9. Permitir adicionar e remover títulos das listas.
10. Permitir editar e excluir listas próprias.
11. Permitir ordenar itens quando aplicável.
12. Permitir consultar a quantidade de títulos em cada lista.
13. Impedir duplicações do mesmo título na mesma lista.
14. Definir uma política de privacidade para listas personalizadas.
15. Restringir o acesso às listas de acordo com essa política.
16. Permitir que a interface indique quando um título já é favorito ou está na watchlist.
17. Tratar títulos removidos ou indisponíveis no catálogo sem corromper as listas.
18. Exibir estados de carregamento, vazio e erro.
19. Permitir que o usuário remova itens sem excluir o título do catálogo.
20. Manter listas e favoritos associados ao usuário autenticado.

## Requisitos Não Funcionais

- Aplicar autorização no backend.
- Evitar duplicações por meio de validação de negócio e restrições de banco.
- Garantir consistência nas operações de inclusão e remoção.
- Utilizar paginação para listas extensas.
- Evitar consultas repetidas ao catálogo.
- Não expor listas privadas a terceiros.
- Manter favoritos e watchlist como conceitos distintos.

## Você Deve

- Criar entidades e relacionamentos adequados.
- Implementar operações de leitura e escrita para os recursos do usuário.
- Validar os títulos antes de adicioná-los.
- Implementar autorização para edição e exclusão de listas.
- Definir claramente a política inicial de privacidade.
- Criar testes para duplicações e acessos indevidos.
- Reutilizar o catálogo para exibir informações dos títulos.
- Tratar conflitos de atualização e falhas de persistência.
- Garantir que a remoção de um item da lista não exclua o título do catálogo.

## Você Não Deve

- Misturar favoritos com histórico de visualização.
- Marcar automaticamente um título como assistido ao removê-lo da watchlist.
- Permitir edição de listas de outros usuários sem autorização.
- Duplicar metadados completos do catálogo em cada item de lista.
- Implementar compartilhamento social avançado neste módulo.
- Criar regras de recomendação próprias.
- Excluir o registro global de um título quando um usuário o remover de sua lista.

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

- O usuário consegue adicionar e remover favoritos.
- O usuário consegue gerenciar a watchlist.
- O usuário consegue criar, editar e excluir listas próprias.
- Não existem itens duplicados na mesma lista.
- Listas privadas não podem ser acessadas por usuários não autorizados.
- A remoção de um item não afeta o catálogo global.
- A interface reflete as alterações persistidas.
- Listas extensas podem ser consultadas sem carregar todos os registros de uma vez.
- Os testes relevantes passam.

## Resultado

Vertical slice de favoritos, watchlist e listas personalizadas implementado; a ativação em produção depende da integração coordenada descrita abaixo.

## Contrato da entrega

- Todas as rotas são autenticadas e ficam sob `/api/v1`. O backend identifica o usuário pelo `sub` validado do JWT e resolve a conta Reelix; nenhum payload aceita `accountId` ou proprietário.
- Favoritos reutilizam `UserFavoriteTitle`, inclusive os títulos gravados pelo onboarding. Watchlist e itens de listas personalizadas são relações independentes.
- Favoritos usam `PUT /favorites/{titleId}` e `DELETE /favorites/{titleId}`; watchlist usa `PUT /watchlist/{titleId}` e `DELETE /watchlist/{titleId}`. O corpo de inclusão informa somente `contentType` (`movie` ou `series`).
- Listas privadas usam `GET/POST /lists`, `GET/PUT/DELETE /lists/{listId}`, `PUT/DELETE /lists/{listId}/titles/{titleId}` e `PUT /lists/{listId}/titles/order`. A ordenação recebe a sequência completa de IDs internos Reelix.
- `POST /title-status` recebe até 50 IDs internos do catálogo e informa se cada título já está nos favoritos ou na watchlist do usuário autenticado, permitindo que a interface mostre esses estados sem inferi-los localmente.
- Coleções retornam `page`, `pageSize`, `totalCount` e referências do catálogo (ID Reelix, tipo e chave do provedor), sem copiar metadados para as relações do usuário. Os itens das listas personalizadas incluem posição e data de inclusão; watchlist aceita ordenação por data. Favoritos usam ordenação estável por ID porque a relação existente não armazena data de inclusão.
- Inclusões repetidas retornam sucesso com `added: false`; remoções são idempotentes. Limites de página: 1 a 100 títulos. Nomes de listas: até 80 caracteres; descrições: até 500.
- Reordenação com uma sequência desatualizada retorna `409 Conflict`; títulos duplicados ou inválidos retornam `400 Bad Request`.
- Uma lista ou título não encontrado ou pertencente a outra conta não é exposto; a API responde com `ProblemDetails` e mensagens pt-BR. Remover relações não exclui registros do catálogo.
- Listas personalizadas são privadas por padrão e owner-only nesta entrega; não há compartilhamento público. A visibilidade de favoritos e histórico não é ampliada por este módulo.

O módulo está integrado ao `ReelixDbContext`, migração PostgreSQL, DI, endpoints autenticados, rota `lists` e wireframe central. Favoritos continuam usando a relação existente do onboarding; watchlist e listas personalizadas usam relações próprias. As tabelas têm chaves/restrições para impedir duplicações e referências do catálogo com exclusão restrita.
