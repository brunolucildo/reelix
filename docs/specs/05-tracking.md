# Tracking

## Escopo

Front-End, Back-End e banco de dados, com integração ao catálogo.

## Contexto

O Reelix precisa permitir que os usuários acompanhem seu histórico de visualização.

O usuário poderá marcar filmes como assistidos, acompanhar séries, registrar episódios concluídos e visualizar o progresso de cada série.

O Tracking será a fonte principal dos dados de visualização do usuário e deverá ser reutilizável por Home, Perfil, Gamificação e Notificações.

## Requisitos Funcionais

1. Permitir marcar um filme como assistido.
2. Permitir remover ou corrigir um registro de visualização.
3. Permitir registrar a data de visualização quando aplicável.
4. Permitir marcar um episódio como assistido.
5. Permitir desmarcar um episódio anteriormente registrado.
6. Manter o progresso individual de cada série.
7. Calcular a quantidade de episódios assistidos de uma série.
8. Calcular o total de episódios disponíveis a partir dos dados válidos do catálogo.
9. Exibir o progresso da série em uma tela de acompanhamento.
10. Permitir consultar o histórico de visualização.
11. Permitir consultar os filmes assistidos.
12. Permitir consultar as séries acompanhadas e seus episódios pendentes.
13. Atualizar o progresso após marcar ou desmarcar um episódio.
14. Evitar duplicação de registros para a mesma ação de acompanhamento.
15. Permitir ordenação do histórico por data.
16. Tratar episódios especiais ou sem numeração convencional conforme os identificadores fornecidos pelo catálogo.
17. Diferenciar a conclusão de uma série da conclusão de uma temporada.
18. Permitir registrar um filme como assistido sem obrigar o usuário a informar uma data manualmente.
19. Disponibilizar os dados necessários para estatísticas de visualização.
20. Garantir que o histórico pertença ao usuário autenticado.

### Contratos de API esperados

Os endpoints deverão seguir o padrão `/api/v1`.

A implementação deverá prever operações equivalentes a:

- Consultar o histórico do usuário autenticado.
- Registrar um filme como assistido.
- Remover um registro de visualização.
- Registrar um episódio como assistido.
- Desmarcar um episódio.
- Consultar o progresso de uma série.
- Consultar os episódios pendentes de uma série acompanhada.

Os métodos HTTP e os contratos definitivos deverão seguir as convenções REST adotadas pelo projeto e ser documentados no OpenAPI.

## Requisitos Não Funcionais

- Aplicar autorização em todas as operações.
- Garantir integridade dos registros.
- Evitar condições de corrida que gerem registros duplicados.
- Criar índices para consultas frequentes por usuário, título e data.
- Evitar consultas que carreguem todo o histórico sem paginação.
- Manter o cálculo de progresso consistente.
- Tratar indisponibilidade do catálogo sem apagar dados de acompanhamento existentes.
- Não revelar o histórico de outros usuários.

## Você Deve

- Criar entidades e relacionamentos para registros de visualização.
- Utilizar identificadores internos estáveis para títulos e episódios.
- Garantir a unicidade lógica dos registros conforme as regras definidas.
- Implementar paginação do histórico.
- Implementar testes para marcar, desmarcar e recalcular o progresso.
- Documentar como episódios sem data de lançamento são tratados.
- Diferenciar episódios assistidos de episódios disponíveis.
- Reutilizar dados do catálogo.
- Garantir que operações repetidas não produzam duplicações.
- Atualizar os dados retornados ao aplicativo após uma alteração.

## Você Não Deve

- Implementar avaliações ou comentários neste módulo.
- Implementar listas personalizadas.
- Inferir que um episódio foi assistido apenas porque sua data de lançamento passou.
- Marcar automaticamente episódios como assistidos.
- Excluir registros de acompanhamento apenas porque um título deixou de aparecer no catálogo.
- Permitir que o cliente determine livremente o identificador do proprietário do registro.
- Calcular estatísticas históricas usando estimativas apresentadas como dados exatos.

## Tecnologias (Stack)

- React Native
- Expo
- TypeScript
- TanStack Query
- ASP.NET Core
- C#
- PostgreSQL
- Entity Framework Core
- Npgsql
- xUnit

## Critérios de aceitação

- O usuário consegue marcar e desmarcar filmes e episódios.
- O progresso de uma série é atualizado corretamente.
- O histórico apresenta os registros do usuário autenticado.
- Operações repetidas não criam duplicações.
- O usuário não consegue alterar o histórico de outra pessoa.
- O sistema trata títulos ou episódios indisponíveis no catálogo.
- Consultas extensas possuem paginação.
- Os testes de regras de negócio e persistência passam.

## Resultado

Sistema de acompanhamento de visualização funcional, com histórico persistido, progresso de séries e dados disponíveis para outros módulos.
