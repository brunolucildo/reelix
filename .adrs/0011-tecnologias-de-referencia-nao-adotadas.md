# 0011 — Tecnologias do projeto de referência não adotadas

- **Status:** aceito
- **Data:** 2026-10-30
- **Origem:** registro retroativo; migrado do antigo `docs/technology-stack-evaluation.md`, que comparou a stack de um projeto de referência (processador distribuído de apostas) com a solução .NET, o app Expo e as specs do Reelix.

## Contexto

O projeto de referência comprova padrões úteis (Prometheus por processo, SQS FIFO, Inbox/Outbox, leases, retries, testes reais com PostgreSQL/LocalStack), mas seus requisitos financeiros e operacionais não são requisitos do Reelix. A decisão segue o princípio de só adicionar infraestrutura quando um caso de uso local justificar o custo.

## Decisão

Preservar a stack atual (.NET + EF Core/Npgsql + PostgreSQL; Expo + npm) e **não** adicionar dependências de runtime derivadas do projeto de referência neste momento.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| ----------- | ---- | ------- | ----------- |
| Bun 1.x | Runtime e gerenciador rápidos | O app usa npm e `package-lock.json`; CI usa npm | Um segundo gerenciador/lockfile sem problema a resolver |
| Node16 module/resolution com saída CommonJS | Config validada na referência | Não é transferível ao bundle Expo/Metro | Preservar a configuração exigida pelo Expo/TypeScript |
| NestJS | Estrutura conhecida | Já existe API ASP.NET Core com auth, contratos e módulos | Duplicaria deploy, autenticação e operação |
| MikroORM | ORM TypeScript da referência | EF Core + Npgsql já são donos do modelo e das migrations | Dividiria o ownership do schema |
| Money em `bigint` (minor units) | Evita erro de ponto flutuante | Não há valores monetários nas specs | Adotar só quando existir requisito monetário (value object, decimais como string) |
| AWS SQS + LocalStack | Entrega durável, retry e DLQ | Não há worker externo nem requisito de processamento durável; a spec de notificações proíbe mensageria complexa sem necessidade | Adiar; reaproveitar os padrões (Inbox/Outbox, idempotência, DLQ, leases) quando uma operação precisar sobreviver a reinícios |
| Prometheus text exposition | Métricas por processo validadas na referência | Sem hosting, rede, scraper nem autorização definidos | Candidato futuro (REELIX-016), com integração .NET, endpoint protegido e sem labels de alta cardinalidade |
| `bun test` / PostgreSQL real nos testes | Testes de integração mais fiéis | O backend usa xUnit | Manter xUnit; Testcontainers for .NET foi a adição útil (ADR 0009) |

## Consequências

- Positivas: menos superfície e menos custo operacional; decisões futuras têm critérios explícitos.
- Negativas: padrões úteis (broker durável, métricas) ficam adiados até haver caso de uso; observabilidade continua sem implantação.
- Impacto em código, operação e time: nenhuma tecnologia existente foi removida e nenhum pacote ou serviço foi adicionado. Critérios para reabrir: **Prometheus** quando houver hosting que colete métricas (endpoint restrito, sem `userId`/IDs de título em labels, sem tokens); **SQS/LocalStack** quando uma operação exigir durabilidade e workers independentes (documentar antes Inbox/Outbox, idempotência, retries, dead-letter e leases); **Money** quando surgir feature financeira (nunca `double`/`float`).
