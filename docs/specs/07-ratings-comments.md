# Avaliações e comentários

## Escopo

Front-End, Back-End e banco de dados, com integração ao catálogo e ao sistema de autenticação.

A integração com a OpenAI poderá ser utilizada para apoiar a identificação de possíveis spoilers, mediante configuração explícita.

## Contexto

O Reelix deverá permitir que os usuários avaliem filmes, séries e episódios e publiquem comentários sobre os conteúdos.

As avaliações serão feitas em uma escala de 0 a 5 estrelas.

Os comentários poderão conter spoilers. A plataforma deverá permitir que o autor sinalize esse conteúdo e ocultá-lo dos demais usuários até que decidam revelá-lo.

A moderação automática poderá auxiliar na análise, mas não deverá ser considerada infalível.

## Requisitos Funcionais

1. Permitir registrar uma avaliação de 0 a 5 estrelas.
2. Validar que a nota esteja dentro do intervalo permitido.
3. Permitir editar uma avaliação própria.
4. Permitir remover uma avaliação própria.
5. Permitir criar comentários sobre títulos.
6. Permitir criar comentários sobre episódios quando aplicável.
7. Permitir editar e excluir comentários próprios.
8. Permitir marcar explicitamente um comentário como contendo spoiler.
9. Ocultar o texto marcado como spoiler por padrão.
10. Permitir que o leitor revele manualmente um comentário sinalizado.
11. Exibir autor, data e conteúdo conforme as regras de privacidade.
12. Calcular médias de avaliações a partir dos registros válidos.
13. Diferenciar avaliações de filmes, séries e episódios.
14. Impedir duplicação de avaliações para o mesmo usuário e alvo, salvo se a regra de negócio permitir múltiplas avaliações.
15. Permitir denunciar comentários inadequados, caso o fluxo de denúncia seja implementado nesta etapa.
16. Registrar estados de moderação quando houver moderação automática ou manual.
17. Permitir identificar comentários pendentes de análise.
18. Não publicar automaticamente comentários que tenham sido bloqueados por uma regra explícita de moderação.
19. Exibir mensagens de validação e erros de envio.
20. Manter a avaliação independente do texto do comentário.

### Integração com OpenAI

1. Permitir enviar o texto de um comentário para análise automatizada quando a funcionalidade estiver habilitada.
2. Solicitar uma classificação estruturada, por exemplo, `possible_spoiler`, `confidence` e `reason`.
3. Validar o formato da resposta antes de utilizá-la.
4. Tratar respostas inválidas, indisponibilidade, timeout e limites de requisição.
5. Encaminhar casos ambíguos para revisão ou manter o comentário sujeito às regras conservadoras definidas pelo produto.
6. Preservar a possibilidade de sinalização manual de spoiler pelo autor.
7. Não utilizar a classificação da IA como prova definitiva de que o comentário contém ou não contém spoiler.

## Requisitos Não Funcionais

- Validar os dados no cliente e no servidor.
- Aplicar limites de tamanho para comentários.
- Proteger contra spam e abuso.
- Aplicar autorização por recurso.
- Não expor comentários privados ou de contas restritas.
- Não enviar dados pessoais desnecessários à OpenAI.
- Não registrar chaves ou conteúdo sensível nos logs.
- Controlar custos, timeouts e volume de chamadas à IA.
- Não permitir que uma falha da OpenAI comprometa a gravação de avaliações.
- Documentar a política de moderação e os limites do classificador.

## Você Deve

- Separar avaliação, comentário e estado de moderação.
- Definir contratos claros para cada tipo de avaliação.
- Validar notas no backend.
- Implementar ocultação de spoilers na resposta e na interface.
- Permitir que o usuário revele um comentário sinalizado.
- Criar testes para as regras de autorização e moderação.
- Implementar a integração com OpenAI por meio de um serviço no backend.
- Utilizar a chave da OpenAI somente no servidor.
- Validar respostas da IA com um esquema definido.
- Criar limites de requisições e mecanismos para evitar chamadas repetidas desnecessárias.
- Manter a funcionalidade principal disponível quando a IA estiver indisponível.
- Documentar os critérios para publicação, retenção e revisão de comentários.

## Você Não Deve

- Permitir notas menores que 0 ou maiores que 5.
- Permitir que um usuário altere avaliações de terceiros.
- Exibir spoilers sinalizados sem a ação do leitor.
- Confiar cegamente em uma resposta da IA.
- Enviar senhas, tokens ou dados pessoais desnecessários à OpenAI.
- Expor a chave da OpenAI no mobile.
- Utilizar a IA para inventar avaliações ou comentários de usuários.
- Bloquear toda a funcionalidade de avaliações quando a OpenAI estiver indisponível.
- Implementar funcionalidades sociais completas neste módulo.

## Tecnologias (Stack)

- React Native
- Expo
- TypeScript
- TanStack Query
- ASP.NET Core
- C#
- PostgreSQL
- Entity Framework Core
- OpenAI API
- xUnit

## Critérios de aceitação

- O usuário consegue criar, editar e remover suas próprias avaliações.
- Notas fora do intervalo de 0 a 5 são rejeitadas.
- O usuário consegue publicar e editar comentários próprios.
- Comentários sinalizados como spoilers ficam ocultos inicialmente.
- O leitor pode revelar o conteúdo sinalizado.
- A API aplica autorização e regras de privacidade.
- A classificação automática trata respostas inválidas e falhas externas.
- Uma indisponibilidade da OpenAI não impede operações que não dependem da IA.
- A chave da OpenAI não aparece no aplicativo ou nos logs.
- Os testes de domínio, API e integração passam.

## Resultado

Sistema de avaliações e comentários implementado, com notas de 0 a 5 estrelas, sinalização de spoilers e suporte opcional à moderação assistida por IA.
