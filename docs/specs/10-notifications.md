# Notificações

## Escopo

Front-End, Back-End, persistência de preferências e integração com serviços de notificação push compatíveis com Expo.

## Contexto

O Reelix deverá notificar os usuários sobre eventos relevantes, como estreias, novos episódios de séries acompanhadas e interações sociais.

As notificações deverão respeitar as preferências do usuário, as permissões do dispositivo e a disponibilidade dos dados dos provedores de catálogo.

A existência de uma data de lançamento não garante que o episódio estará disponível em determinada região ou plataforma. As mensagens não deverão prometer informações que a fonte não confirma.

## Requisitos Funcionais

1. Permitir registrar um dispositivo autorizado para receber notificações push.
2. Permitir remover o registro de um dispositivo.
3. Permitir consultar as preferências de notificação.
4. Permitir ativar ou desativar categorias de notificações.
5. Permitir notificações sobre novos episódios de séries acompanhadas.
6. Permitir notificações sobre estreias de filmes relevantes para o usuário.
7. Permitir notificações de solicitações de amizade e outras interações sociais suportadas.
8. Evitar notificações duplicadas para o mesmo evento e destinatário.
9. Permitir que o usuário toque em uma notificação para abrir a tela relacionada.
10. Respeitar a configuração de privacidade dos dados usados na mensagem.
11. Tratar permissões negadas pelo sistema operacional.
12. Tratar tokens de dispositivos inválidos ou expirados.
13. Permitir consultar o estado de notificações relevantes no aplicativo quando necessário.
14. Evitar enviar alertas de episódios já marcados como assistidos.
15. Permitir desativar categorias de notificações sem exigir a desativação de todas.
16. Registrar o estado de processamento das notificações quando necessário para diagnóstico.
17. Tratar mudanças nas datas de lançamento conforme as atualizações disponíveis do catálogo.
18. Implementar política de retentativas para falhas transitórias.
19. Evitar envio de informações pessoais desnecessárias no conteúdo da notificação.
20. Definir uma estratégia de agendamento compatível com a infraestrutura escolhida.

## Requisitos Não Funcionais

- Respeitar limites dos provedores de push.
- Não enviar notificações sem a permissão necessária.
- Proteger tokens de dispositivos.
- Controlar duplicação e retentativas.
- Evitar dependência síncrona de serviços externos nas operações principais.
- Registrar falhas sem expor dados sensíveis.
- Controlar o volume de notificações por usuário.
- Não depender de atualização em tempo real do catálogo quando essa capacidade não estiver disponível.

## Você Deve

- Utilizar Expo Notifications quando compatível com a estratégia de distribuição do aplicativo.
- Validar a documentação e os requisitos de configuração da infraestrutura de push.
- Criar preferências por categoria.
- Implementar controle de duplicação.
- Definir como os eventos geram notificações.
- Tratar erros e retentativas.
- Evitar que a indisponibilidade de um serviço de push interrompa outras funcionalidades.
- Implementar testes para preferências, duplicações e autorização.
- Documentar como as datas de lançamento são obtidas e atualizadas.

## Você Não Deve

- Enviar notificações quando a categoria estiver desativada.
- Garantir entrega imediata ou em tempo real.
- Expor tokens de dispositivos nas respostas públicas da API.
- Enviar mensagens com informações não confirmadas pelo catálogo.
- Criar um sistema complexo de mensageria sem necessidade comprovada.
- Utilizar a OpenAI para redigir cada notificação rotineira.
- Considerar que o envio ao serviço de push comprova que o dispositivo recebeu a notificação.

## Decisão de infraestrutura

Não há fila selecionada para o MVP. AWS SQS e LocalStack podem ser avaliados quando o processamento exigir entrega durável, retentativas independentes do processo da API, dead-letter ou workers escalados separadamente. Até lá, não adicionar broker nem uma segunda infraestrutura local; registrar idempotência e política de retentativa no plano da feature quando esses requisitos forem definidos.

## Tecnologias (Stack)

- React Native
- Expo
- TypeScript
- Expo Notifications
- ASP.NET Core
- C#
- PostgreSQL
- Entity Framework Core
- Serviço de agendamento a definir conforme a infraestrutura
- xUnit

## Critérios de aceitação

- O usuário consegue configurar suas preferências.
- O dispositivo pode ser registrado e removido.
- As notificações respeitam as preferências do usuário.
- Eventos repetidos não produzem notificações duplicadas.
- Notificações relacionadas a episódios direcionam para o conteúdo correto.
- Tokens inválidos são tratados.
- Falhas de envio são registradas sem exposição de segredos.
- A aplicação continua funcionando quando o serviço de push está indisponível.
- Os testes relevantes passam.

## Resultado

Sistema de notificações implementado, com preferências configuráveis, integração push e processamento controlado dos eventos relevantes.
