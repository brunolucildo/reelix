# 0009 — Testcontainers PostgreSQL para testes de integração

- **Status:** aceito
- **Data:** 2026-11-03 (aproximada, pelo histórico git)
- **Origem:** registro retroativo de decisão já implementada

## Contexto

A suíte xUnit inicial testava contratos, modelos EF e migrations sem um PostgreSQL real, o que não prova isolamento entre contas, constraints, consentimentos nem concorrência. `docs/technology-stack-evaluation.md` (2026-10-30) já apontava Testcontainers for .NET como a primeira adição técnica a avaliar, sem substituir o xUnit. A autenticação real (Supabase, ADR 0003) não pode ser usada em teste, e o único banco de desenvolvimento (ADR 0004) não deve ser alterado por testes.

## Decisão

- **Fixture com Testcontainers:** `PostgresAuthorizationFixture` (`IClassFixture`, `IAsyncLifetime`) sobe um container `postgres:17-alpine` (`Testcontainers.PostgreSql` 4.16.0), aplica a cadeia completa de migrations com `Database.MigrateAsync()` e remove o container no `DisposeAsync`. A suíte não usa nem altera o banco configurado nos User Secrets.
- **`WebApplicationFactory<Program>`:** `PostgresAuthorizationApiFactory` sobe a API real em `Development`, troca o `DbContext` para o container e roda requests HTTP de ponta a ponta (rotas, autorização, EF e PostgreSQL).
- **JWT RSA em memória:** a fábrica gera um `RSA` de 2048 bits, remove `Authority` e `ConfigurationManager` do `JwtBearerOptions` e fixa emissor, audiência e chave pública de teste; `CreateToken(subject)` assina tokens `sub` arbitrários. Nada de rede nem de Supabase. As variáveis `Supabase__Url` e `ConnectionStrings__Reelix` são definidas e restauradas no `Dispose`.
- **Escopo:** 10 testes em `PostgresAuthorizationIntegrationTests` cobrem isolamento e consentimento em perfil, listas, avaliações, tracking (inclusive marcações concorrentes), preferências e inbox de notificações, gamificação e social (aceite só pelo destinatário; feed exige amizade e consentimento vigente). Os testes sem banco continuam usando `AuthenticationApiFactory`, com repositórios falsos e tokens de cenário (emissor, audiência, expirado, assinatura inválida).
- **Requisito:** Docker em execução, localmente e no CI. O workflow roda `dotnet test` em `ubuntu-latest`; segundo `.docs/QA.md`, a suíte (202 testes) passou local e na CI.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| Só testes com repositórios falsos ou `InMemory` | Rápido, sem Docker | Não valida SQL, constraints, RLS nem concorrência | `technology-stack-evaluation.md`: Testcontainers é a adição mais útil para persistência real |
| Rodar contra o banco de desenvolvimento (Supabase) | Sem Docker | Altera dados compartilhados; depende de rede e credencial | A fixture não pode tocar o banco dos User Secrets (`authentication-setup.md`) |
| PostgreSQL local instalado à mão (inferido) | Sem container por teste | Estado sujo e ambiente diferente entre máquinas e CI | Inferido: container descartável dá estado limpo e igual no CI |
| Tokens reais do Supabase nos testes | Mais fiel | Rede, credenciais e usuários de teste reais | Chave RSA em memória cobre a validação de emissor, audiência, validade e assinatura |
| Suíte de testes do mobile (E2E) | Cobre o fluxo inteiro | Não existe runner mobile | Fora do escopo desta decisão (`.docs/QA.md`: testes mobile pendentes) |

## Consequências

- Positivas: autorização, isolamento por conta, constraints e a cadeia de migrations são provados contra PostgreSQL 17 real; cada classe de teste começa de um banco limpo; a CI não precisa de segredos; a suíte unitária continua rápida e sem Docker.
- Negativas: exige Docker (mais lento e pesado); o fixture é por classe e só uma classe o usa, então não há reaproveitamento; a imagem fixa `postgres:17-alpine` não reproduz extensões ou papéis do Supabase (inferido: os papéis `anon`/`authenticated` não existem no container, então os `REVOKE` condicionais das migrations não são exercitados com eles); cobertura limitada a 10 cenários, sem todos os endpoints nem concorrência de workers (ADR 0007); a fábrica altera variáveis de ambiente do processo (restauradas no `Dispose`).
- Impacto em código, operação e time: novos cenários de integração entram em `backend/Reelix.UnitTests/Api/`; o CI precisa de Docker no runner; quem roda `dotnet test` localmente precisa do Docker Desktop. O backlog registra a ampliação dos testes de integração como concluída (REELIX-002, `.docs/ARCHITECTURE.md §6.3`); novas áreas seguem o mesmo padrão.
