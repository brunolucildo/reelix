# Home / Discovery

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
