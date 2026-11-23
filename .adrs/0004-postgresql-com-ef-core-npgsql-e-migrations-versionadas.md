# 0004 — PostgreSQL com EF Core/Npgsql e migrations versionadas

- **Status:** aceito
- **Data:** 2026-10-26 (aproximada, pelo histórico git)
- **Origem:** registro retroativo de decisão já implementada

## Contexto

Usuários, referências de catálogo, tracking, listas, avaliações, relações sociais e notificações precisam de integridade (chaves estrangeiras, unicidade, transações). `.docs/ARCHITECTURE.md` §6 fixa PostgreSQL como fonte de verdade, EF Core + Npgsql para persistência normal e migrations, Dapper só se medição justificar, e diz que o backend deve manter regras e autorização mesmo usando Supabase como PostgreSQL gerenciado. O banco hospedado no Supabase também pode ser exposto por uma Data API; era preciso decidir como o schema evolui, como é revisado antes de aplicar e como o acesso direto é bloqueado.

## Decisão

- **Persistência:** EF Core 10 + Npgsql (`UseNpgsql` com `ConnectionStrings:Reelix`, obrigatória na partida) em um `ReelixDbContext` único. Tabelas e colunas em `snake_case` explícito no schema `public`, instantes em `timestamp with time zone`. Dapper não é usado.
- **Migrations versionadas** em `backend/Reelix.Infrastructure/Persistence/Migrations/` (10 até `ExpandFixedAvatars`, em 2026-11-04). A ferramenta `dotnet-ef` 10.0.13 é local ao repositório (`.config/dotnet-tools.json`). A API **não aplica migrations na partida**; só os testes chamam `MigrateAsync` (ADR 0009).
- **CI gera, não aplica:** o job `backend` roda `dotnet ef migrations script --idempotent` (com `Supabase__Url` e connection string fictícias, pois o comando não conecta) e publica `artifacts/reelix-migrations.sql` como artefato (14 dias) para revisão manual (`.github/workflows/ci.yml`). Aplicar é um passo humano, com connection string do ambiente-alvo.
- **Ambiente único:** o único ambiente existente é `Development` (confirmado pelo usuário, `.docs/QA.md`); qualquer ambiente novo exige repetir a verificação de migrations antes de uso.
- **RLS habilitado e acesso só pela API:** cada migration que cria tabelas executa `ENABLE ROW LEVEL SECURITY` e `REVOKE ALL` de `PUBLIC` e, se existirem, de `anon`, `authenticated` e `service_role`. Não há `CREATE POLICY` nem `GRANT` nas migrations. A API acessa o banco pela própria connection string, nunca pela Data API. `.docs/QA.md` registra RLS nas 24 tabelas do banco de desenvolvimento.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| Dapper ou SQL puro | Controle total do SQL | Duas formas de acesso; mapeamento manual | `architecture.md` §4: só para consultas medidas; SQL cru fica pontual nos repositórios (`FOR UPDATE`, ADR 0007) |
| MikroORM | Mesmo ORM de outro projeto | Segundo ORM e dono do schema dividido | `technology-stack-evaluation.md`: EF Core + Npgsql já cobrem |
| Aplicar migrations na partida da API ou pelo CI | Sem passo manual | Alteração de schema sem revisão, num banco compartilhado | O CI só gera o script; `authentication-setup.md` pede revisar antes de aplicar |
| Cliente falando direto com a Data API do Supabase | Menos código de API | Exigiria políticas RLS testadas por tabela (`architecture.md` §6) e tiraria a autorização do backend | A API é o único caminho de dados; a RLS sem policies fica como defesa em profundidade (parte inferida) |

## Consequências

- Positivas: schema versionado e revisável no repositório; script idempotente sai a cada build; os papéis da Data API não têm privilégio nas tabelas e a RLS está ligada; constraints (`CK_*`, índices únicos) protegem invariantes além do código.
- Negativas: nada garante que o banco de um ambiente esteja na última migration (verificação manual); RLS sem policies só protege a Data API, não substitui a autorização da API; as fontes não definem separação entre papel de migração e papel de runtime; hosting e política de migrações por ambiente seguem como decisão pendente (`.docs/ARCHITECTURE.md §6.3`).
- Impacto em código, operação e time: toda mudança de modelo vira migration (`dotnet ef migrations add`), com SQL de RLS na migration de tabela nova; o artefato `reelix-migrations.sql` também está versionado em `artifacts/`. Procedimentos em `.specs/0001-authentication.md` (seção "Banco e verificações"). Nunca apontar a connection string de produção ou compartilhada sem autorização explícita.
