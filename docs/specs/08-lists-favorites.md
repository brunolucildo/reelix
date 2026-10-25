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

Sistema de favoritos, watchlist e listas personalizadas implementado, com persistência consistente e controle de acesso.
