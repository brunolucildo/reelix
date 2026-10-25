# Gamificação

## Escopo

Front-End, Back-End e banco de dados, com integração ao Tracking, Perfil e demais fontes de atividades relevantes.

## Contexto

A gamificação deverá incentivar o uso contínuo do Reelix por meio de conquistas, estatísticas e rankings.

As conquistas deverão ser baseadas em atividades verificáveis, evitando recompensas duplicadas ou estatísticas que não possam ser explicadas.

O módulo deverá começar com regras determinísticas, sem depender da OpenAI para calcular resultados.

## Requisitos Funcionais

1. Criar um catálogo de conquistas disponíveis.
2. Definir regras objetivas para cada conquista.
3. Permitir conquistas relacionadas a filmes assistidos.
4. Permitir conquistas relacionadas a episódios e séries acompanhadas.
5. Permitir conquistas relacionadas à diversidade de gêneros.
6. Registrar quando uma conquista é obtida.
7. Impedir que a mesma conquista seja concedida repetidamente ao mesmo usuário, salvo se for explicitamente recorrente.
8. Permitir consultar as conquistas obtidas.
9. Permitir consultar o progresso de conquistas ainda não obtidas, quando aplicável.
10. Exibir badges no perfil.
11. Permitir que conquistas obtidas sejam públicas por padrão.
12. Não tornar públicos o histórico completo, as preferências ou outras informações pessoais por causa da gamificação.
13. Calcular horas assistidas somente quando houver dados suficientes para uma estimativa justificável.
14. Diferenciar tempo confirmado de tempo estimado.
15. Permitir consultar estatísticas por período.
16. Criar rankings com critérios documentados.
17. Definir se os rankings são globais, entre amigos ou limitados a um grupo.
18. Aplicar paginação nos rankings.
19. Evitar que alterações repetidas no histórico concedam recompensas duplicadas.
20. Recalcular o progresso quando registros forem corrigidos ou removidos, conforme as regras de cada conquista.

## Requisitos Não Funcionais

- Garantir consistência das conquistas.
- Evitar cálculos excessivos em cada acesso ao perfil.
- Documentar fórmulas e critérios.
- Proteger informações privadas nos rankings.
- Evitar manipulações simples de pontuação.
- Tratar mudanças no catálogo e registros corrigidos.
- Garantir que os resultados possam ser reproduzidos a partir dos dados disponíveis.

## Você Deve

- Criar um catálogo de conquistas configurável.
- Implementar regras explícitas para conceder cada badge.
- Integrar-se ao Tracking como fonte dos dados de visualização.
- Garantir unicidade das conquistas obtidas.
- Documentar como as estatísticas são calculadas.
- Separar a definição da conquista do registro de sua obtenção.
- Criar testes para concessão, correção e remoção de registros.
- Definir políticas de privacidade para rankings.
- Garantir que os cálculos não contem registros duplicados.
- Permitir adicionar novas conquistas sem reescrever todo o módulo.

## Você Não Deve

- Conceder conquistas baseadas em dados inventados.
- Utilizar a OpenAI para decidir se um usuário merece uma conquista.
- Exibir horas estimadas como se fossem medidas exatas.
- Tornar público o histórico completo por padrão.
- Permitir que o usuário envie livremente a própria pontuação.
- Criar rankings sem definir a fórmula de ordenação.
- Implementar pagamentos, recompensas financeiras ou moedas virtuais nesta etapa.

## Tecnologias (Stack)

- ASP.NET Core
- C#
- PostgreSQL
- Entity Framework Core
- React Native
- Expo
- TypeScript
- TanStack Query
- xUnit

## Critérios de aceitação

- As conquistas possuem regras documentadas.
- O usuário consegue visualizar suas conquistas.
- Uma conquista não é concedida repetidamente por causa de eventos duplicados.
- Correções no histórico são tratadas de acordo com as regras definidas.
- Estatísticas utilizam dados válidos do Tracking.
- Rankings respeitam os critérios e as regras de privacidade.
- O sistema não permite que o usuário determine diretamente suas próprias pontuações.
- Os testes de consistência passam.

## Resultado

Módulo de gamificação implementado, com conquistas verificáveis, estatísticas e rankings transparentes.
