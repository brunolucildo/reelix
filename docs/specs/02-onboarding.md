# Onboarding

## Escopo

Front-End, Back-End e persistência das preferências iniciais do usuário.

## Contexto

Após o primeiro acesso, o Reelix deverá permitir que o usuário indique seus gostos para personalizar a página inicial e as recomendações.

O onboarding deverá aproveitar os dados disponíveis no módulo de catálogo. Não deverá criar uma segunda fonte de dados para filmes, séries ou gêneros.

## Requisitos Funcionais

1. Identificar se o usuário concluiu o onboarding.
2. Apresentar uma sequência de telas de boas-vindas e configuração inicial.
3. Permitir selecionar gêneros de interesse.
4. Permitir pesquisar e selecionar filmes favoritos.
5. Permitir pesquisar e selecionar séries favoritas.
6. Permitir avançar e retornar entre as etapas sem perder as seleções já realizadas.
7. Salvar as preferências no backend.
8. Permitir concluir o onboarding sem preencher todas as opções, desde que as regras mínimas de negócio sejam atendidas.
9. Permitir pular etapas opcionais.
10. Registrar a conclusão do onboarding.
11. Permitir editar as preferências posteriormente pelo módulo de perfil.
12. Utilizar as preferências para alimentar o módulo de Home e Discovery.
13. Exibir estados de carregamento, falha de comunicação e sucesso ao salvar.
14. Evitar que o usuário seja obrigado a repetir o onboarding após reabrir o aplicativo.
15. Permitir retomar o fluxo caso a aplicação seja encerrada antes da conclusão.

## Requisitos Não Funcionais

- Salvar as preferências de forma consistente.
- Evitar requisições desnecessárias ao catálogo.
- Manter a interface responsiva e acessível.
- Aplicar autorização no backend.
- Não expor as preferências de um usuário a outros usuários sem autorização.
- Garantir que falhas de comunicação não resultem em uma conclusão falsa do onboarding.
- Evitar dependência de chamadas à OpenAI para concluir o fluxo.

## Você Deve

- Consultar o estado do onboarding no backend.
- Reutilizar as telas e componentes de pesquisa do catálogo quando disponíveis.
- Criar contratos para leitura e atualização das preferências.
- Persistir as escolhas associadas ao usuário autenticado.
- Implementar uma experiência de seleção simples e visual.
- Tratar o onboarding como um fluxo retomável.
- Garantir que a conclusão seja persistida no servidor.
- Testar usuários novos, usuários com onboarding incompleto e usuários que já concluíram o fluxo.
- Permitir que preferências vazias sejam tratadas adequadamente pelos módulos consumidores.

## Você Não Deve

- Implementar cadastro ou login neste módulo.
- Duplicar os dados do catálogo no banco de preferências.
- Exigir seleção de um grande número de títulos.
- Bloquear permanentemente o uso do aplicativo caso o usuário pule etapas opcionais.
- Criar recomendações complexas nesta etapa.
- Enviar preferências a serviços externos sem necessidade ou transparência.
- Tornar públicas as preferências automaticamente.

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
- xUnit

## Critérios de aceitação

- Um usuário novo consegue iniciar o onboarding.
- Os gêneros e títulos selecionados podem ser salvos.
- O usuário consegue retornar a uma etapa sem perder as escolhas persistidas.
- O estado do onboarding permanece correto após reiniciar o aplicativo.
- O backend rejeita alterações feitas por usuários não autorizados.
- Títulos inexistentes ou referências inválidas são rejeitados.
- O usuário consegue concluir o fluxo sem selecionar favoritos, quando permitido pelas regras definidas.
- Os dados ficam disponíveis para os módulos que dependem das preferências.
- Os testes automatizados relevantes passam.

## Resultado

Fluxo inicial de personalização implementado, com preferências persistidas e disponíveis para a experiência personalizada do Reelix.
