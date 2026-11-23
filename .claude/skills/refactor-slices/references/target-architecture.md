# Arquitetura-alvo — Reelix (.NET, Clean Architecture por fatia vertical)

> Referência carregada pelos agentes da skill `refactor-slices`.
> Fonte de verdade do projeto: `.docs/ARCHITECTURE.md`, `.docs/DEVELOPMENT.md`e `.adrs/0001-*`; para o mobile, `mobile/AGENTS.md`.
> Se o código atual divergir desta referência, o código vence **até** a refatoração ser aplicada; a divergência é exatamente o que a skill corrige.

## 1. Contrato das camadas

Solução `Reelix.sln`, projetos em `backend/`:

| Projeto | Responsabilidade | Pode referenciar | Não pode referenciar |
| --- | --- | --- | --- |
| `Reelix.Domain` | Entidades com invariantes, value objects, domain services puros | Nada (só BCL) | Application, Infrastructure, Api, EF Core, ASP.NET, HTTP |
| `Reelix.Application` | Use cases (um por fluxo), ports, erros de aplicação, services de capacidade reutilizável | Domain | Infrastructure, Api, EF Core, ASP.NET, `HttpStatusCode`/`HttpRequestException` |
| `Reelix.Infrastructure` | Repositórios, EF Configurations, adapters de provedores (TMDB, OMDb, OpenAI, Expo), DI dos adapters | Application, Domain | Api; regra de negócio |
| `Reelix.Api` | Endpoints (rotas), request handlers, DTOs de request/response, mapeamento de erro, composition root | Application | Domain (tipos), Infrastructure (**exceto** `Program.cs`/DI) |
| `Reelix.UnitTests` | Testes | Todos | — |

**Direção das dependências:** `Api → Application → Domain`; `Infrastructure → Application/Domain` (implementa ports). Nenhuma seta aponta para fora. A Api só conhece Infrastructure no composition root.

## 2. Layout-alvo (por módulo)

Módulos de produto: Catalog, Tracking, Lists, Ratings/Comments, Social, Notifications, Gamification, Users (Onboarding/Profile) e Authentication.

```text
backend/
  Reelix.Domain/<Modulo>/
    <Entidade>.cs                      # invariantes; sem DateTime.UtcNow
    <ValueObject>.cs
    <Regra>DomainService.cs            # puro, sem I/O (ex.: AchievementEvaluator)
  Reelix.Application/<Modulo>/
    UseCases/<Fluxo>/
      <Fluxo>UseCase.cs                # UMA classe, UM fluxo, UM ExecuteAsync
      <Fluxo>Command.cs | <Fluxo>Query.cs
      <Fluxo>Result.cs
    Ports/
      I<Necessidade>Reader.cs          # interfaces pequenas (ISP): leitura e escrita separadas
      I<Necessidade>Writer.cs
    Errors/
      <Modulo>Errors.cs                # erros de aplicação tipados
    Services/                          # só capacidade reutilizável, nunca um fluxo
    <Modulo>Module.cs                  # AddXxxApplication(this IServiceCollection)
  Reelix.Infrastructure/
    Persistence/<Modulo>/
      <Agregado>Repository.cs          # implementa ports; só persiste/consulta
      <Entidade>Configuration.cs       # EF Configuration
    <Provedor>/                        # TMDB, OMDb, OpenAI, Expo: adapters
    <Modulo>Module.cs                  # AddXxxInfrastructure(...)
  Reelix.Api/
    <Modulo>/
      <Modulo>Endpoints.cs             # só MapGroup + rotas + metadados OpenAPI
      Handlers/<Fluxo>Handler.cs       # UM handler, UM endpoint, UM use case
      Contracts/<Fluxo>Request.cs | <Fluxo>Response.cs   # DTOs HTTP (ou ao lado do handler)
      Workers/                         # BackgroundService: chama um use case por ciclo
    Common/
      ICurrentUser.cs (+ implementação) # leitura do `sub` em um único lugar
      ApplicationErrorMapper.cs         # mapeamento único erro→ProblemDetails (pt-BR)
      PageRequest/Cursor helpers        # se usados só na borda
    Program.cs                          # composition root
```

O planner pode adaptar nomes, mas **não pode** eliminar a separação das camadas, o vínculo 1:1 endpoint ↔ handler ↔ use case nem o mapeamento único de erros. Precedente no código: `Application/Authentication/GetAuthenticatedAccount.cs` já é um use case de fato (um fluxo, `ExecuteAsync`); a spec decide se ele é renomeado para `...UseCase`.

## 3. Use case (Application)

- Uma classe por fluxo, método público único `ExecuteAsync(<Fluxo>Command|Query, CancellationToken)`.
- Tipos `<Fluxo>Command|Query` e `<Fluxo>Result` ficam **ao lado** do use case, na mesma pasta do fluxo.
- Recebe ports pelo construtor (interfaces estreitas), nunca implementação concreta nem o service de outro módulo.
- Orquestra: validação → port de leitura → regra de domínio → port de escrita → efeito colateral (notificação, evento) **depois** de persistir.
- Obtém o instante por `TimeProvider` injetado e o **passa** ao Domain; o Domain não chama `DateTime.UtcNow`.
- Falhas esperadas viram **erros de aplicação tipados** (`Application/<Modulo>/Errors/`). A spec de implementação decide `Result<T>` vs exceções; a skill só exige que haja **um** mapeamento centralizado para `ProblemDetails` em pt-BR.
- Não conhece DTO HTTP, `IResult`, `HttpContext`, EF Core, `HttpStatusCode` nem `HttpRequestException`.

```csharp
// Application/Lists/UseCases/AddFavorite/AddFavoriteUseCase.cs
public sealed class AddFavoriteUseCase(
    ICatalogTitleLookup titles,          // port estreito exposto por Catalog
    IFavoritesWriter favorites,
    TimeProvider clock)
{
    public async Task<AddFavoriteResult> ExecuteAsync(
        AddFavoriteCommand command,
        CancellationToken cancellationToken)
    {
        if (!await titles.ExistsAsync(command.TitleId, cancellationToken))
            throw new TitleNotFoundError(command.TitleId);   // ou Result, conforme a spec

        var created = await favorites.AddAsync(
            command.UserId, command.TitleId, clock.GetUtcNow(), cancellationToken);
        return new AddFavoriteResult(created);
    }
}

public sealed record AddFavoriteCommand(Guid UserId, Guid TitleId);
public sealed record AddFavoriteResult(bool Created);
```

`Services/` só abriga capacidade reutilizável (ex.: emissão de cursor, cálculo compartilhado). Um `*Service` com vários fluxos públicos é o sintoma principal a corrigir (regra `service-too-many-methods`: > 5 métodos públicos).

## 4. Ports e erros (Application)

- **Ports** em `Application/<Modulo>/Ports/`, nomeados pela necessidade, pequenos (ISP): `IFavoritesReader`/`IFavoritesWriter` em vez de um `IUserListsRepository` com 15 métodos. Um use case depende só do que usa.
- **Nenhum** `*Contracts.cs` que misture DTOs, ports e exceções (regra `contracts-mixed`). Cada coisa vai para seu lugar: comando/resultado ao lado do use case, port em `Ports/`, erro em `Errors/`.
- **Erros** em `Errors/`, um tipo por situação de negócio (não encontrado, conflito, validação, indisponibilidade de provedor). Application não importa `HttpStatusCode`/`HttpRequestException`: o adapter traduz.

## 5. Request handler (Api)

### Regra 1:1

- Cada endpoint tem **um** arquivo/classe em `Api/<Modulo>/Handlers/<Fluxo>Handler.cs`.
- Cada handler injeta **exatamente um use case** (além de abstrações de borda como `ICurrentUser`).
- Cada use case é consumido por **exatamente um** handler. Se dois endpoints precisam do mesmo comportamento, crie dois use cases ou extraia a parte comum para um service de capacidade em `Application/<Modulo>/Services/`.
- Worker (`BackgroundService`) não é endpoint, mas também chama **um** use case por ciclo e não contém regra.

### Responsabilidades

1. Ler o usuário autenticado por `ICurrentUser` (ou extensão única do `ClaimsPrincipal`); **não** existe `TryGetUserId` por arquivo.
2. Converter request DTO da Api → `Command|Query` (inclui parse de enums e normalização de paginação, vindos de helpers únicos).
3. Chamar o use case.
4. Converter `Result` → response DTO da Api e devolver `IResult` com o **mesmo status** de hoje.
5. Deixar erros de aplicação chegarem ao mapeador central (`ApplicationErrorMapper`); handler **não** tem `catch`/`Results.Problem` inline.

O handler **não** contém regra de negócio, não acessa banco, não referencia Domain e não orquestra mais de um use case.

DTOs de request/response ficam na Api e **nunca** são tipos de Domain; DTOs de Application deixam de servir de contrato HTTP. O JSON público (nomes de propriedade, casing, nulos, enums, formato de datas) permanece idêntico.

```csharp
// Api/Lists/Handlers/AddFavoriteHandler.cs
public static class AddFavoriteHandler
{
    public static async Task<IResult> HandleAsync(
        Guid titleId,
        ICurrentUser currentUser,
        AddFavoriteUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new AddFavoriteCommand(currentUser.RequireUserId(), titleId), cancellationToken);
        return Results.Ok(new MutationResponse(result.Created));
    }
}

// Api/Lists/ListsEndpoints.cs  — só rotas e metadados
public static class ListsEndpoints
{
    public static RouteGroupBuilder MapListsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1").WithTags("Favorites and Lists").RequireAuthorization();

        group.MapPut("/favorites/{titleId:guid}", AddFavoriteHandler.HandleAsync)
            .WithName("AddFavorite")
            .WithSummary("Add a catalog title to favorites.")
            .Produces<MutationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        // ...
        return group;
    }
}
```

`WithName`, `WithSummary`, `Produces*`, `RequireAuthorization` e `WithTags` permanecem **exatamente** como estão hoje (fazem parte do contrato OpenAPI).

### Erros → ProblemDetails (único)

- Um único componente (`IExceptionHandler`, filtro de endpoint ou conversor de `Result`) traduz erro de aplicação → status + `ProblemDetails` (título/detalhe em pt-BR **idênticos aos atuais**).
- `ArgumentException` → 400 inline, `KeyNotFoundException` → 404 inline e `catch (CatalogProviderException)` repetido somem dos endpoints.
- Resposta 401 por ausência de `sub` mantém status e corpo atuais (hoje `Results.Unauthorized()`), agora emitida por um único ponto.

## 6. Domain

- Entidades protegem invariantes; construtores/fábricas e métodos recebem o instante (`DateTimeOffset now`) em vez de ler o relógio.
- Value objects e **domain services puros** (sem I/O, sem EF) abrigam regras como avaliação de conquistas e processamento de eventos sociais.
- **Proibido** `DateTime(Offset).UtcNow|Now` (regra `direct-clock-access`) em Domain, Application e Infrastructure; `TimeProvider` é injetado na Application (e nos adapters que precisem de relógio).
- **Proibido** regra de negócio em repositório/Infrastructure. Hoje: `GamificationRepository` (avaliação de conquistas: `Earn()`/`Revoke()` por tipo de regra) e `NotificationsRepository` (processamento de eventos sociais: `switch` por `EventType`) devem virar use cases + domain services, deixando o repositório só persistir/consultar.

## 7. Infrastructure

- Repositórios por agregado/port estreito; um repositório gordo implementando 15 métodos vira vários adapters pequenos (ou classes parciais claramente separadas por port) — o que importa é cada port ter sua implementação coesa.
- EF Configurations em `Persistence/<Modulo>/`. **Sem** mudar modelo, migrations ou snapshot (`Migrations/` intacto).
- Adapters de provedores (TMDB, OMDb, OpenAI, Expo) traduzem erros de HTTP (`HttpRequestException`, status) para erros de aplicação tipados; Application nunca vê HTTP.
- DI por módulo: um `<Modulo>Module.cs` (ou `AddXxx`) consistente em Application e em Infrastructure, no lugar dos `*DependencyInjection.cs` espalhados; `Program.cs` só compõe.
- Nenhum adapter depende do service concreto de outro módulo: dependa de um **port**.

## 8. Fronteiras entre módulos

- Um módulo **não** lê tabelas de outro e **não** injeta o service/use case concreto de outro.
- Capacidade compartilhada vira **port estreito exposto pelo módulo dono**, implementado na Infrastructure dele. Exemplo: "título existe no catálogo" é **um** port em `Catalog/Ports/ICatalogTitleLookup`, consumido por Lists, Tracking, Ratings etc.
- Dependências circulares entre módulos: quebrar com port + inversão, não com referência direta.

## 9. Regras de qualidade

- `CancellationToken` propagado em toda cadeia assíncrona (handler → use case → port).
- **GET sem efeito colateral de escrita.** Onde hoje um GET cria conta/perfil de forma preguiçosa, a criação vira um **passo explícito e nomeado** (ex.: `EnsureUserAccountUseCase`/port `IAccountProvisioner` chamado de forma visível), separado da leitura. Se eliminar a escrita mudaria o contrato observável, isso é decisão da spec, não da refatoração: preserve o comportamento e registre.
- **Paginação** validada em **um único lugar** (`PageRequest` ou equivalente) com os limites e erros atuais.
- **Cursor opaco** codificado/decodificado em **um único helper**.
- Sem atalhos para escapar do padrão: nada de `dynamic`, `#pragma warning disable` para esconder regra, `[Fact(Skip = ...)]` ou teste relaxado.

## 10. Contrato HTTP público — intocável

A refatoração é estrutural. **Nunca** podem mudar:

- rotas, métodos HTTP e prefixos (`/api/v1/...`), parâmetros de rota/query/corpo;
- status codes e quando cada um ocorre (inclusive 401/400/404/409 hoje produzidos inline);
- JSON de request e response: nomes de propriedade, casing, nulabilidade, enums (string/número), formato de datas, defaults;
- `ProblemDetails`: `status`, `title`, `detail`, extensões, em pt-BR como hoje;
- metadados OpenAPI (`WithName`, `Produces*`, tags, segurança);
- autenticação/autorização (`RequireAuthorization`, políticas) e rate limits;
- esquema do banco, migrations, snapshot do EF e SQL gerado de forma observável;
- chamadas a provedores externos (URLs, parâmetros, ordem) e payloads de push;
- dependências NuGet.

**Teste de caracterização obrigatório:** se o fluxo a refatorar não tiver teste que fixe esse contrato (status + corpo JSON, inclusive cenários de erro), o planner cria um **Batch 0** que escreve esses testes **antes** de qualquer mudança estrutural e os roda **verdes sobre o código antigo**. Eles entram no mesmo PR. Se um teste precisar mudar de asserção (e não só de namespace/construtor), o comportamento mudou: pare e reporte.

## 11. O que não é uma fatia

- `Reelix.Api/Program.cs`, `Common/` (ICurrentUser, mapeador, helpers) e `Reelix.Infrastructure/Persistence/ReelixDbContext.cs`: infraestrutura transversal, não módulos.
- `Authentication` é módulo (hoje só `GetAuthenticatedAccount`); aplica-se o padrão, mesmo pequeno.
- Workers (`NotificationDeliveryWorker`, `SocialNotificationWorker`, `CommentModerationWorker`) pertencem ao módulo de origem em `Api/<Modulo>/Workers/` e chamam um use case.

## 12. Mobile (seção curta — `mobile/`, Expo/React Native)

A refatoração do mobile é opcional e **estrutural**: telas, rotas do Expo Router (`src/app/`), textos e comportamento não mudam. Siga `mobile/AGENTS.md`, `.docs/DESIGN.md` e `.docs/DESIGN_SYSTEM.md` (UI/UX).

```text
mobile/src/
  app/                              # rotas Expo Router: finas, só composição
  features/<modulo>/
    api/                            # chamadas HTTP da feature (usam o http-client único)
    hooks/                          # React Query / estado da feature
    components/                     # componentes da feature
    screens-support/                # view-models, mapeadores e helpers da tela
  components/ui/                    # primitivos de UI compartilhados
  lib/                              # formatação (datas, números), utilidades puras
  services/http-client.ts           # UM client HTTP (base URL, auth, erro)
```

Regras:

- **Um** http-client (hoje `services/api-client.ts` e `*-api.ts` soltos); features só o consomem.
- **Query keys por fábrica** (`listsKeys.favorites(page)`), nunca strings soltas.
- **Hooks por feature** encapsulam query/mutation; tela não chama `fetch` nem monta chave.
- **Telas finas**: compõem hooks e componentes; sem regra de negócio, sem parsing de resposta.
- **Primitivos de UI** compartilhados em um lugar (botão, input, modal, cartão); sem duplicar estilo por tela.
- **Formulários** com `zod` + `react-hook-form` (`schemas/` por feature); sem validação manual dispersa.
- **Formatação** em `lib/`; sem `toLocaleDateString` repetido em componentes.
- Gates: `npm run lint` e `npx tsc --noEmit` dentro de `mobile/`. Não adicione dependência.

## 13. Documentação e ADR

Ao aplicar o padrão pela primeira vez:

1. Crie `.adrs/NNNN-use-case-por-fluxo-e-handler-por-endpoint.md` (próximo número livre; hoje `0011`) com decisão, alternativas e consequências.
2. Atualize `.docs/ARCHITECTURE.md` (e `.docs/DEVELOPMENT.md`) descrevendo camadas, `UseCases/`, `Ports/`, `Errors/`, `Handlers/` e as fronteiras entre módulos.
3. Registre no plano e no relatório final.

Os ADRs `0001`–`0010` já existem em `.adrs/` (o `0001` registra o monólito modular com Clean Architecture em quatro projetos: o novo ADR o complementa, não o substitui). Use o template de `.docs/RULES.md` §5.
