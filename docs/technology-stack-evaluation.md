# Avaliação de tecnologias — transferência para o Reelix

**Data:** 30/10/2026  
**Resultado:** preservar a stack atual; não adicionar dependências de runtime neste momento.

A lista e a arquitetura completas do Distributed Wagering Processor foram comparadas com a solução .NET, o app Expo, os arquivos de dependência e as specs atuais do Reelix. O projeto de referência comprova Prometheus por processo, SQS FIFO, Inbox/Outbox, leases, retries e testes reais com PostgreSQL/LocalStack em processos independentes. Essas evidências aumentam a confiança nos padrões, mas não tornam os requisitos financeiros e operacionais do outro domínio requisitos do Reelix. A decisão segue o princípio de adicionar infraestrutura somente quando um caso de uso local justificar o custo.

| Tecnologia/decisão de referência | Situação no Reelix | Decisão |
|---|---|---|
| PostgreSQL | Já é o banco do backend, gerenciado atualmente pelo Supabase. | Manter. |
| TypeScript estrito | `mobile/tsconfig.json` já habilita `strict`. | Manter; não há benefício em duplicar configuração. |
| Bun 1.x | O app Expo usa npm e `mobile/package-lock.json`; os scripts e CI documentados usam npm. | Não adicionar um segundo gerenciador/lockfile. Reavaliar apenas se uma necessidade de tooling Bun surgir. |
| Node16 module/resolution mode com saída CommonJS | Não é uma configuração transferível para o bundle Expo/Metro e não resolve um problema atual do app. | Não aplicar. Preservar a configuração exigida pelo Expo/TypeScript atual. |
| NestJS | O backend existente é ASP.NET Core e já contém autenticação, contratos e módulos. | Não adicionar uma segunda API; isso duplicaria deploy, autenticação e operação. |
| MikroORM | EF Core + Npgsql já são responsáveis pelo modelo, acesso PostgreSQL e migrations. | Não adicionar um segundo ORM nem dividir ownership do schema. |
| Money em `bigint` minor units | Não há moeda, pagamentos, carteira ou cálculo financeiro nas specs atuais. | Adotar somente quando existir requisito monetário; então encapsular em um value object e serializar valores decimais como strings. |
| AWS SQS + LocalStack | Não existe worker nem requisito atual de processamento durável assíncrono. A spec de notificações proíbe mensageria complexa sem necessidade. A referência demonstra FIFO, Inbox/Outbox, leases, retry/DLQ, crash/restart e múltiplos processos. | Adiar o broker. Reaproveitar esses padrões quando notificações ou integrações precisarem de entrega durável e retry; decidir SQS/LocalStack nessa fase, junto com idempotência, DLQ e operação. |
| Prometheus text exposition | Ainda não há endpoint de métricas, hosting ou scraper configurado no Reelix. A referência comprova métricas por processo, incluindo counters e gauges dependentes atualizados no scrape. | Candidato de observabilidade validado na referência, mas condicionado a definir hosting, rede, autorização e cardinalidade. Implementar com integração .NET, não copiar o cliente Nest/Bun. |
| `bun test` e PostgreSQL real nos testes | Backend usa xUnit; testes atuais verificam contratos e migrations sem uma instância PostgreSQL descartável. A referência demonstra integração PostgreSQL/LocalStack e corridas multi-processo. | Manter xUnit. Testcontainers for .NET é a adição técnica mais útil para avaliar primeiro: testes de migration/RLS, constraints e persistência real, quando Docker estiver disponível no ambiente local/CI. |

## Critérios para próximas decisões

- **Prometheus:** adicionar quando existir um ambiente de hospedagem que vá coletar métricas. Restringir o endpoint à rede/scraper autorizado, evitar labels com `userId`, IDs de títulos ou outros dados de alta cardinalidade e nunca incluir tokens ou dados pessoais. O scraping precisa descobrir cada instância, pois counters são locais ao processo.
- **SQS/LocalStack:** adicionar quando uma operação precisar sobreviver a reinícios, ser retentada de forma durável ou processada por workers independentes. Primeiro documentar Inbox/Outbox, idempotência, política de retries, dead-letter, leases e ownership dos eventos.
- **Testcontainers:** avaliar primeiro, se o CI puder iniciar Docker, para testar migrations/RLS, constraints, persistência e concorrência em PostgreSQL real. Não substituir xUnit nem os testes unitários existentes.
- **Money:** só introduzir quando surgir uma feature financeira; não usar `double` ou `float` para valores monetários.

Nenhuma tecnologia existente foi removida ou substituída por esta avaliação. Nenhum pacote ou serviço de infraestrutura foi adicionado.
