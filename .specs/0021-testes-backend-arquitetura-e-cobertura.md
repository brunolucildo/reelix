# 0021 — Testes do backend: arquitetura, cobertura e host de teste para E2E

> Spec executável. Preencha todas as seções; `TODO` é lacuna, não requisito.
> Use `create-spec` para criar, `refine-spec` para auditar e `implement-me` para executar.

- **Status:** pronta
- **Slug:** testes-backend-arquitetura-e-cobertura
- **Data:** 2026-11-19
- **Atualizado em:** 2026-11-19
- **Owner:** Tech lead (a confirmar)
- **Relacionados:** `.plans/arquitetura-design-testes-breakdown.md` (D1, D2, D5, D9, D10; Q5); ADR 0009 (Testcontainers) e ADR 0001 (quatro projetos); `.docs/TESTING.md` §2, §6, §7.5, §7.6, §8; `.docs/QA.md` §1, §3.1, §6; `.docs/ARCHITECTURE.md` §3.2 e §6.1; depende de `0013` (TimeProvider), `0014`/`0015`/`0016` (regras de arquitetura); alimenta `0023`; paralela de `0022`.

## 1. Escopo

### Dentro do escopo

- (a) Projeto `backend/Reelix.ArchitectureTests` (xUnit, sem Docker) que referencia só os 4 assemblies de produção e verifica as regras de dependência e de forma da §4 (R2 a R4), com allowlist temporária explícita.
- (b) Separação de testes unitários e de integração por `[Trait("Category", "Integration")]`, com comandos `dotnet test --filter` e execução sem Docker documentada.
- (c) Suporte de teste compartilhado: um repositório falso de conta único, `TestJwt` (RSA e HS256) reutilizável, fábrica e fixture em arquivos próprios.
- (d) Cobertura com coverlet no CI: relatório, piso inicial medido e falha abaixo do piso; fechamento das lacunas priorizadas (R11).
- (e) `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) nos testes temporais.
- (f) URLs de TMDB, OMDb, Expo Push e OpenAI configuráveis (defaults inalterados) e **host de teste para E2E** (API real em `Development`, PostgreSQL via Testcontainers, JWT HS256 conhecido, stub de catálogo local). Resolve a Q5 do mapa.
- (g) Gate de CI atualizado e atualização de `.docs/TESTING.md`, `.docs/QA.md`, `.docs/AGENTS.md`, `.docs/DEVELOPMENT.md` §9 e `.docs/ARCHITECTURE.md` §3.1/§3.2.

### Fora do escopo

- Refatorar o código de produção para cumprir as regras (`0013` a `0016`); aqui só se mede e se registra a dívida.
- Qualquer mudança de comportamento de produção além das URLs configuráveis (R12); migrations, rotas e JSON.
- Testes do mobile (`0022`) e Playwright (`0023`, que consome o host de R13).
- Teste de contrato OpenAPI, observabilidade, `dotnet format`/analisadores, mutation testing e testes de carga (backlog do mapa).

## 2. Contexto

Estado medido em 2026-11-19 (commit `1baace9`): `dotnet test Reelix.sln --configuration Release --no-restore` aprova **202 de 202** testes em um único projeto, `Reelix.UnitTests`, que mistura domínio, serviços, modelo EF, HTTP em memória e Testcontainers; **10** testes exigem Docker (`PostgresAuthorizationIntegrationTests`) e não há `[Trait]` para filtrá-los. Há 0 testes de arquitetura. `IUserAccountRepository` tem 6 implementações falsas em 6 arquivos (`AuthenticationEndpointTests`, `GamificationPersistenceTests`, `UserListsServiceTests`, `RatingsCommentsServiceTests`, `SocialServiceTests`, `TrackingServiceTests`); `AuthenticationApiFactory` e `PostgresAuthorizationApiFactory`/`Fixture` vivem dentro dos arquivos de teste, com 3 geradores de chave RSA e 10 chamadas a `Environment.SetEnvironmentVariable`. Não há `TimeProvider` no código (0 ocorrências) e os testes usam `UtcNow` 25 vezes.

Cobertura medida com `dotnet test --collect:"XPlat Code Coverage"` (coverlet.collector 6.0.4, Docker ativo): **82,61% de linhas** (14.902/18.037) e 39,85% de ramos (821/2.060), valor inflado pelas migrations. **Sem `Migrations`: 62,6%** (10.140/16.200): Api 53,3%, Application 67,9%, Domain 79,1%, Infrastructure 60,4%. Sem teste algum: `GetAuthenticatedAccount`, `OnboardingService`, `UserProfileService`, `NotificationsService`, `SocialNotificationProcessor`, `NotificationDeliveryProcessor` e os 3 workers (`Program.cs` linhas 177 a 179).

`Program.cs` define `options.Authority = issuer` sempre, e os endereços de TMDB, OMDb, Expo e OpenAI estão fixos em `Reelix.Infrastructure/DependencyInjection.cs` (linhas 66, 88, 99, 104). Hoje só o teste em memória escapa disso, com `PostConfigure<JwtBearerOptions>` que zera `Authority`. O E2E (`0023`) precisa de um host real nessas condições.

## 3. Necessidade de negócio

| Item                     | Descrição |
| ------------------------ | --------- |
| Usuário/papel afetado    | Quem desenvolve e revisa o backend; quem opera o CI; a spec `0023`, que depende do host de teste |
| Decisão ou ação que muda | A regra de dependência entre camadas passa a ser verificada por máquina; a cobertura deixa de cair em silêncio; o E2E ganha um backend determinístico sem rede externa |
| Resultado esperado       | Violação de camada ou queda de cobertura reprova o PR; `dotnet test --filter "Category!=Integration"` roda sem Docker; um comando sobe API e banco de teste para o Playwright |
| Métrica de sucesso       | 0 regras sem teste negativo; allowlist de arquitetura vazia ao fim da `0016`; cobertura acima do piso medido; Application com meta de 80% de linhas; 0 duplicatas de repositório falso de conta |

## 4. Requisitos

| ID  | Requisito | Prioridade | Critério de aceite |
| --- | --------- | ---------- | ------------------ |
| R1  | Projeto `Reelix.ArchitectureTests` (xUnit, `net10.0`) na solução, referenciando somente `Reelix.Domain`, `Reelix.Application`, `Reelix.Infrastructure` e `Reelix.Api` | Must | Given o repositório, When `dotnet list backend/Reelix.ArchitectureTests reference` e `dotnet test backend/Reelix.ArchitectureTests --configuration Release` rodam com o Docker parado, Then lista exatamente 4 referências, sem `Reelix.UnitTests` nem Testcontainers, e os testes passam |
| R2  | Regras de camada: Domain sem dependência de outros assemblies do projeto, EF Core ou ASP.NET; Application sem EF Core, `Reelix.Infrastructure`, `Reelix.Api`, `System.Net.Http` e `HttpStatusCode`; Infrastructure sem `Reelix.Api`; Api sem tipos de `Reelix.Domain` em contratos HTTP (parâmetros/retornos de endpoint e DTOs) e handlers | Must | Given uma violação introduzida de propósito (ex.: `using Microsoft.EntityFrameworkCore` em tipo de Application), When o teste da regra roda, Then falha nomeando o tipo e a regra; sem a violação, passa |
| R3  | Regras de forma: um handler injeta exatamente um caso de uso; nenhum `DateTime.UtcNow`/`DateTimeOffset.UtcNow` em Domain e Application (varredura de IL, pois a regra é de chamada e não de tipo); nenhum `*Service` de Application com mais de 5 métodos públicos; módulo A não referencia repositório nem entidade/tabela de módulo B (Catalog, Tracking, Lists, Ratings, Social, Notifications, Gamification, Users) | Must | Given o código após a `0016`, When `dotnet test backend/Reelix.ArchitectureTests` roda, Then cada regra tem um teste nomeado `Regra_Comportamento` e todas passam com a allowlist vazia |
| R4  | Allowlist temporária e explícita para o que a `0016` ainda não satisfaz: cada entrada traz regra, tipo, motivo e a spec que a zera; teste falha se uma entrada deixar de ser violada (obsoleta) ou se a violação existir fora da allowlist | Must | Given uma allowlist com N entradas, When uma violação nova aparece, Then o teste reprova; When uma entrada deixa de ser violada, Then o teste reprova pedindo a remoção; ao fim da `0016` a allowlist tem 0 entradas |
| R5  | Escolha da biblioteca (NetArchTest.Rules, ArchUnitNET ou análise direta com `System.Reflection.Metadata`) registrada em ADR novo (complementa o 0009), com critérios: roda em `net10.0` provado por teste verde, licença compatível, manutenção recente, expressa as regras de R3 (inclui IL) | Must | Given o ADR, When revisado, Then cita versão, licença, data de última publicação e a comprovação em `net10.0`, e os testes de R2 e R3 usam a opção escolhida |
| R6  | Separar unitário de integração por `[Trait("Category", "Integration")]` em todo teste que exige Docker (hoje os 10 de `PostgresAuthorizationIntegrationTests`) | Must | Given o Docker parado, When `dotnet test Reelix.sln --configuration Release --no-restore --filter "Category!=Integration"` roda, Then passa com 192 testes (202 menos 10); When `--filter "Category=Integration"` roda com Docker, Then executa os 10 |
| R7  | Suporte compartilhado: um `FakeUserAccountRepository` substitui as 6 cópias; `PostgresAuthorizationFixture`, `PostgresAuthorizationApiFactory` e `AuthenticationApiFactory` saem dos arquivos de teste para arquivos próprios; fixtures preferem `UseSetting`/configuração do host a variáveis de ambiente do processo | Must | Given a árvore de testes, When `grep -rln ": IUserAccountRepository" backend/Reelix.UnitTests` roda, Then lista no máximo 1 arquivo (o do suporte compartilhado) e nenhum arquivo de teste de módulo; as 3 classes de fábrica/fixture ficam em arquivos próprios e `dotnet test` segue com 202 aprovados |
| R8  | `TestJwt` único (RSA e HS256) com cenários `valid`, `wrong-issuer`, `wrong-audience`, `expired`, `invalid-signature`, usado pelas duas fábricas e pelo host de R13 | Must | Given o código, When `grep -rn "RSA.Create" backend` roda, Then há uma única ocorrência de geração de chave RSA de teste; os 202 testes seguem aprovados |
| R9  | `FakeTimeProvider` nos testes de regra temporal (episódio lançado, data futura, leases, backoff de entrega, marcação com data); depende da `0013` ter injetado `TimeProvider` | Must | Given a `0013` concluída, When `grep -rn "UtcNow" backend/Reelix.UnitTests` roda, Then só `TestJwt` aparece (o JwtBearer valida validade pelo relógio do sistema), e um teste avança o relógio falso e observa a mudança de comportamento |
| R10 | Cobertura no CI: `dotnet test` com `--collect:"XPlat Code Coverage"`, exclusão de `Migrations`, relatório publicado como artefato (cobertura e resumo por assembly) e **piso inicial** igual ao valor medido na implementação (linhas, sem migrations), arredondado para baixo, que reprova o job abaixo dele; linha de base de hoje: 62,6% | Must | Given o CI, When um PR remove testes e a cobertura cai abaixo do piso, Then o step falha com a mensagem do valor atual x piso; sem queda, passa e o artefato existe |
| R11 | Lacunas priorizadas viram testes de comportamento: `GetAuthenticatedAccount`, `OnboardingService`, `UserProfileService`, `NotificationsService`, `SocialNotificationProcessor`, `NotificationDeliveryProcessor`, os 3 workers (loop, cancelamento, falha de um ciclo não derruba o host), regras de conquistas de Gamification (se a `0014` não as cobriu); nomes podem mudar com `0014`/`0016`, vale o caso de uso equivalente | Must | Given a cobertura por tipo, When o relatório de R10 é gerado, Then cada tipo listado tem ao menos 1 teste de comportamento e Application atinge a meta de 80% de linhas (meta, não piso) |
| R12 | URLs de TMDB, OMDb, Expo Push e OpenAI configuráveis (ex.: `Catalog:Tmdb:BaseUrl`, `Catalog:Omdb:BaseUrl`, `ExpoPush:BaseUrl`, `OpenAI:SpoilerAnalysis:BaseUrl`); defaults idênticos aos atuais; URL diferente do default só é aceita em `Development`/`Testing` e reprova a partida (`ValidateOnStart`) em `Production`, para o token de provedor nunca ir a um host arbitrário | Must | Given nenhuma configuração, When o `HttpClient` de cada provedor é resolvido, Then `BaseAddress` é a atual; Given `BaseUrl` apontando a um servidor local em `Development`, Then o adaptador chama o servidor local; Given `BaseUrl` customizada em `Production`, Then a API não inicia |
| R13 | Host de teste para E2E (projeto próprio, ex.: `backend/Reelix.E2EHost`): `Program` real em `Development`; sobe `postgres:17-alpine` por Testcontainers e aplica `Database.MigrateAsync()`; `Supabase__Url` HTTP local e `Supabase__JwtSecret` recebidos por ambiente; JWT HS256 aceito sem acesso à rede (obstáculo de `Authority` resolvido no host, sem alterar `Program.cs` de produção); push e OpenAI desativados; catálogo por stub determinístico (R14); porta por `--urls`; `GET /e2e/ready` 200 só depois do banco migrado; recusa iniciar se a connection string não for a do contêiner | Must | Given Docker ativo, When `dotnet run --project backend/Reelix.E2EHost --urls http://127.0.0.1:5135` roda, Then `/e2e/ready` responde 200, `GET /api/v1/auth/me` com JWT HS256 válido responde 200, com assinatura errada responde 401, e nenhuma conexão sai para hosts externos |
| R14 | Stub de catálogo local do host: busca `e2e` devolve 2 filmes (`900001`, `900002`) e 1 série (`900101`, 2 episódios) com provedor `tmdb`, gêneros, detalhe, temporada e descoberta (popular/próximas); títulos fictícios; contrato consumido pela `0023` | Must | Given o host, When `GET /api/v1/catalog/search?query=e2e` roda com JWT válido, Then retorna os 3 itens com ids fixos, sem credencial TMDB configurada |
| R15 | Gate de CI: o job `backend` mantém o nome; passos: build, testes de arquitetura (primeiro, falha rápida), testes unitários, testes de integração, cobertura com piso, TRX e cobertura como artefatos | Must | Given um PR com violação de camada, When o CI roda, Then o job `backend` falha no passo de arquitetura antes dos demais testes |
| R16 | Documentação atualizada com comandos reais: `TESTING.md` §1.2, §2, §5, §6, §7, §8; `QA.md` §1, §3, §3.1, §6; `AGENTS.md` §3; `DEVELOPMENT.md` §9; `ARCHITECTURE.md` §3 | Must | Given os docs, When `grep -rn "Category!=Integration" .docs` roda, Then o comando aparece em `TESTING.md`, `QA.md` e `AGENTS.md`, e nenhum doc cita como futuro algo que esta spec criou |

## 5. Fluxos e estados

- **Caminho feliz:** o desenvolvedor roda `dotnet test --filter "Category!=Integration"` sem Docker em segundos; abre PR; o CI roda arquitetura, unidade, integração e cobertura; o Playwright usa `dotnet run --project backend/Reelix.E2EHost`.
- **Vazio:** allowlist de arquitetura vazia é o estado final esperado, não um erro.
- **Carregando:** o host aguarda o contêiner e as migrations; `/e2e/ready` não responde 200 antes disso.
- **Erro:** violação nova, entrada de allowlist obsoleta, cobertura abaixo do piso, `BaseUrl` customizada em `Production` e Docker ausente no host de teste reprovam com mensagem que nomeia o motivo.
- **Sucesso parcial:** se a `0016` ainda não terminou, a allowlist contém as violações restantes, cada uma apontando a spec que a zera; (c), (e) e (f) seguem independentes.
- **Cancelamento/retry:** o host derruba o contêiner ao receber SIGINT/SIGTERM; reexecução parte de banco novo.

## 6. Acceptance Criteria

- [ ] Given o Docker parado, When `dotnet test Reelix.sln --configuration Release --no-restore --filter "Category!=Integration"` roda, Then 192 testes (mais os de arquitetura) passam.
- [ ] Given `Category=Integration` e Docker ativo, When o filtro roda, Then 10 testes passam.
- [ ] Given a `0016` concluída, When `dotnet test backend/Reelix.ArchitectureTests` roda, Then passa com a allowlist vazia; com violação deliberada, falha nomeando tipo e regra.
- [ ] Given o CI, When a cobertura (sem migrations) cai abaixo do piso, Then o job `backend` reprova; o piso e a medição estão registrados em `QA.md` §1.
- [ ] Given o host de R13 no ar, When a `0023` o inicia, Then JWT HS256, catálogo do stub e banco migrado funcionam sem rede externa.
- [ ] Given `grep -rn "RSA.Create" backend` e `grep -rln ": IUserAccountRepository" backend/Reelix.UnitTests`, Then 1 ocorrência e no máximo 1 arquivo (o do suporte compartilhado).
- [ ] Given `dotnet build Reelix.sln --configuration Release --no-restore`, Then 0 erros e nenhum aviso novo sem explicação.

## 7. O que fazer

1. Medir de novo a linha de base (testes, cobertura) e registrar em `QA.md` §1.1.
2. Extrair o suporte (R7, R8): decidir entre pasta `Support/` em `Reelix.UnitTests` e biblioteca `Reelix.TestSupport` sem xUnit, necessária porque o host de R13 reutiliza `TestJwt` e o stub de catálogo.
3. Aplicar o `Trait` (R6) e atualizar o CI (R15).
4. Criar `Reelix.ArchitectureTests` (R1 a R5), medindo as violações atuais; registrar cada uma na allowlist com a spec responsável.
5. Introduzir `FakeTimeProvider` (R9) quando a `0013` estiver integrada.
6. Configurar a cobertura (R10), fechar as lacunas (R11) e fixar o piso.
7. Tornar as URLs configuráveis com validação (R12) e criar o host e o stub (R13, R14); verificar a necessidade de `Kestrel` real na fábrica (a disponibilidade no `Microsoft.AspNetCore.Mvc.Testing` 10.x não foi confirmada) ou usar hosting startup.
8. Atualizar documentação (R16) e abrir ADR da biblioteca (R5).

## 8. O que não fazer

- Não relaxar uma regra para passá-la: o que ainda for violado entra na allowlist com dono e spec.
- Não mudar `Program.cs`, rotas, JSON, migrations ou defaults de URL além do necessário a R12.
- Não deixar teste ou host herdar a connection string dos User Secrets; não tocar o banco Supabase.
- Não fixar piso antes de medir nem baixar o piso para aprovar um PR; não commitar `TestResults/` ou cobertura.
- Não colocar código de teste em `Reelix.Api`, `Reelix.Application`, `Reelix.Domain` ou `Reelix.Infrastructure`.

## 9. Riscos e dependências

| Tipo        | Descrição | Mitigação |
| ----------- | --------- | --------- |
| Dependência | Regras de arquitetura só passam após `0016`; `FakeTimeProvider` exige a `0013` | (c), (f) e a medição começam antes; allowlist temporária com dono |
| Risco       | NetArchTest.Rules 1.3.2 (netstandard2.0, última publicação 2021-05-23) e ArchUnitNET.xUnit 0.13.5 (2026-10-26) não declaram `net10.0` diretamente | Prova por teste verde em `net10.0`; fallback `System.Reflection.Metadata` |
| Risco       | Allowlist vira depósito de dívida | Teste de entrada obsoleta e meta de 0 entradas ao fim da `0016` |
| Risco       | Docker no CI e tempo do job `backend` | `ubuntu-latest` já tem Docker; arquitetura e unidade primeiro; medir a duração e registrar |
| Risco       | `BaseUrl` configurável abre exfiltração de token de provedor | Validação de ambiente com `ValidateOnStart` (R12) |
| Risco       | Host de teste diverge do `Program` real | O host reutiliza `Program`; sobrescreve só autenticação, catálogo e banco |
| Risco       | Piso de cobertura oscila entre máquinas | Medir no CI, excluir `Migrations`, margem de arredondamento para baixo |

## 10. Resultado

Como validar que a spec foi atendida:

- **Comando/verificação:** `dotnet test Reelix.sln --configuration Release --no-restore` (todos), `--filter "Category!=Integration"` (sem Docker), `dotnet test backend/Reelix.ArchitectureTests --configuration Release`, `dotnet test ... --collect:"XPlat Code Coverage"` com a checagem do piso, `dotnet run --project backend/Reelix.E2EHost --urls http://127.0.0.1:5135`.
- **Evidência esperada:** linha de base de hoje: 202 testes xUnit (10 com Docker), 0 de arquitetura, 0 mobile, 0 E2E, cobertura 62,6% de linhas sem migrations (82,61% com). Metas: arquitetura com 1 teste por regra e allowlist vazia; 192 testes sem Docker; Application em 80% de linhas; piso publicado em `QA.md`.
- **Comportamento observável:** PR com violação de camada ou queda de cobertura reprova no job `backend`; a `0023` sobe API e banco de teste com um comando.

## 11. Perguntas abertas e premissas

| Pergunta/Premissa | Impacto | Bloqueante? | Quem decide |
| ----------------- | ------- | ----------- | ----------- |
| Biblioteca: NetArchTest, ArchUnitNET ou análise direta de assembly (critérios em R5) | Forma dos testes da R2/R3 | Não (decidida na implementação, ADR) | Tech lead |
| Mecanismo de `Authority` no host (R13): `PostConfigure<JwtBearerOptions>` como nas fábricas, ou hosting startup; `Kestrel` real em `WebApplicationFactory` não foi confirmado | Desenho do host | Não; premissa: nada em produção muda | Tech lead |
| Premissa: `Trait` em vez de projetos separados; `Reelix.TestSupport` só se o host precisar compartilhar `TestJwt` e o stub | Estrutura da solução | Não | Tech lead |
| Mecanismo do piso: `coverlet.msbuild` com `Threshold` ou coletor mais script sobre o Cobertura | Passos do CI | Não | Tech lead |
| Premissa: valor do piso = medição da implementação (hoje 62,6% sem migrations) | Gate | Não | Tech lead |
| Premissa (Q5): URLs configuráveis em vez de stub por DNS/proxy; defaults inalterados | Viabilidade da `0023` | Não | Tech lead |

## 12. Refinamento

| Data       | O que mudou | Motivo |
| ---------- | ----------- | ------ |
| 2026-11-19 | Criação da spec (modo lote do `breakdown-specs`) com linha de base medida: 202 testes, cobertura 62,6% sem migrations | Camada de testes do backend do épico `arquitetura-design-testes` |
