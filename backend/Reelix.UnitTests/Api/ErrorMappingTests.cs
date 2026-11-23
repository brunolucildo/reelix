using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Reelix.Api.Errors;
using Reelix.Application.Catalog;
using Reelix.Application.Errors;
using Reelix.Application.Lists;
using Reelix.Application.Notifications;
using Reelix.Application.Tracking;

namespace Reelix.UnitTests.Api;

/// <summary>
/// The shared problem mapper (spec 0013, R4 and R5): each typed error, thrown by a synthetic route,
/// is answered with the (status, title, detail, type, code, Retry-After) of the matching row of
/// section 5 of the spec. The same rows are characterized against the real routes in
/// <see cref="ErrorContractCharacterizationTests"/>.
/// </summary>
public sealed class ErrorMappingTests : IAsyncLifetime
{
    private SyntheticApiHost _host = null!;

    public async Task InitializeAsync() =>
        _host = await SyntheticApiHost.StartAsync(MapRoutes);

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task Row1_ArgumentException_UsesTheRouteTitleAndDropsTheParameterSuffix()
    {
        using var response = await _host.Client.GetAsync("/lists/argument");

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Dados da lista inválidos.", problem.GetProperty("title").GetString());
        Assert.Equal("Informe o nome da lista.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row1_ArgumentOutOfRange_AlsoDropsTheParameterSuffix()
    {
        using var response = await _host.Client.GetAsync("/lists/out-of-range");

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(
            "A página deve estar entre 1 e 100000.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row1_ARouteWithoutOptions_UsesTheGenericValidationTitle()
    {
        using var response = await _host.Client.GetAsync("/plain/argument");

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Solicitação inválida.", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ValidationError_CanCarryItsOwnTitle()
    {
        using var response = await _host.Client.GetAsync("/lists/validation");

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Cursor inválido.", problem.GetProperty("title").GetString());
        Assert.Equal("O cursor está corrompido.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row4_DetailedDependencyError_CarriesTypeCodeAndRetryAfterWithoutDetail()
    {
        using var response = await _host.Client.GetAsync("/catalog/dependency");

        var problem = await ReadProblemAsync(response, HttpStatusCode.TooManyRequests);
        Assert.Equal("O provedor limitou as consultas.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "https://reelix.local/problems/catalog_rate_limited",
            problem.GetProperty("type").GetString());
        Assert.Equal("catalog_rate_limited", problem.GetProperty("code").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
        Assert.Equal("3", Assert.Single(response.Headers.GetValues("Retry-After")));
    }

    [Fact]
    public async Task Row4_NoRetryAfterHeaderWhenTheHintIsMissingOrZero()
    {
        using var response = await _host.Client.GetAsync("/catalog/dependency-no-hint");

        await ReadProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.False(response.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Row5_TypeOnlyDependencyError_HasTypeButNoCodeAndNoRetryAfter()
    {
        using var response = await _host.Client.GetAsync("/genres/dependency");

        var problem = await ReadProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.Equal("O catálogo está indisponível.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "https://reelix.local/problems/catalog_unavailable",
            problem.GetProperty("type").GetString());
        Assert.False(problem.TryGetProperty("code", out _));
        Assert.False(response.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Row6_OpaqueDependencyError_HidesTheProviderMessage()
    {
        using var response = await _host.Client.GetAsync("/tracking/dependency");

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadGateway);
        Assert.Equal(
            "Não foi possível consultar o catálogo.",
            problem.GetProperty("title").GetString());
        Assert.Equal("Tente novamente mais tarde.", problem.GetProperty("detail").GetString());
        Assert.False(problem.TryGetProperty("code", out _));
        Assert.False(response.Headers.Contains("Retry-After"));
        Assert.DoesNotContain("provider secret", await ReadBodyAsync(response));
    }

    [Fact]
    public async Task Row7_TrackingNotFound_UsesTheRouteTitleAndFixedDetail()
    {
        using var response = await _host.Client.GetAsync("/tracking/not-found");

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal("Conteúdo não encontrado.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "O conteúdo não está disponível no catálogo.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row7_ListsNotFound_UsesTheRouteTitleAndTheErrorMessage()
    {
        using var response = await _host.Client.GetAsync("/lists/not-found");

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal("Título não encontrado.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "O título não está disponível no catálogo.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row9_SeriesMismatch_CarriesItsTitleAndDetail()
    {
        using var response = await _host.Client.GetAsync("/tracking/conflict");

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("Episódio incompatível.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "O episódio não pertence à série informada.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row9_DeviceConflict_CarriesItsTitleAndTheMessageAsDetail()
    {
        using var response = await _host.Client.GetAsync("/notifications/conflict");

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("Dispositivo já associado.", problem.GetProperty("title").GetString());
        Assert.Equal("Este token já está associado.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row9_GenericConflictError_UsesTheTitleOfTheFlow()
    {
        using var response = await _host.Client.GetAsync("/lists/conflict");

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("A lista foi alterada.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "Os títulos da lista mudaram desde a última consulta. Atualize os dados e tente novamente.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row10_ServiceUnavailableError_IsReportedAs503WithTheMessageAsDetail()
    {
        using var response = await _host.Client.GetAsync("/notifications/unavailable");

        var problem = await ReadProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.Equal("Notificações push indisponíveis.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "O registro push está desabilitado até configurar o projeto e as credenciais Expo.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row11_UnauthenticatedError_IsRenderedByTheStatusCodePages()
    {
        using var response = await _host.Client.GetAsync("/plain/unauthenticated");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(401, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Row13_UnexpectedException_IsNotMappedAndLeaksNoInternals()
    {
        using var response = await _host.Client.GetAsync("/plain/unexpected");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var text = await ReadBodyAsync(response);
        Assert.DoesNotContain("internal secret detail", text);
        Assert.DoesNotContain("InvalidOperationException", text);
        Assert.DoesNotContain("   at ", text);
        using var body = JsonDocument.Parse(text);
        Assert.Equal(500, body.RootElement.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData("/lists/argument")]
    [InlineData("/lists/validation")]
    [InlineData("/lists/not-found")]
    [InlineData("/tracking/conflict")]
    [InlineData("/notifications/unavailable")]
    [InlineData("/catalog/dependency")]
    public async Task ExpectedErrors_AreNotLoggedAtErrorLevel(string path)
    {
        using var response = await _host.Client.GetAsync(path);

        Assert.True((int)response.StatusCode >= 400);
        Assert.DoesNotContain(_host.Logs.Entries, entry => entry.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task UnexpectedException_IsLoggedAtErrorLevel_SoTheLogAssertionIsMeaningful()
    {
        await using var host = await SyntheticApiHost.StartAsync(MapRoutes);

        using var response = await host.Client.GetAsync("/plain/unexpected");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains(host.Logs.Entries, entry => entry.Level >= LogLevel.Error);
    }

    private static void MapRoutes(WebApplication app)
    {
        var lists = app.MapGroup("/lists").WithApplicationProblems(new ApplicationProblemOptions
        {
            ValidationTitle = "Dados da lista inválidos.",
            NotFoundTitle = "Título não encontrado.",
        });
        lists.MapGet("/argument", () => Throw(new ArgumentException(
            "Informe o nome da lista.", "name")));
        lists.MapGet("/out-of-range", () => Throw(new ArgumentOutOfRangeException(
            "page", "A página deve estar entre 1 e 100000.")));
        lists.MapGet("/validation", () => Throw(new ValidationError(
            "O cursor está corrompido.", "Cursor inválido.")));
        lists.MapGet("/not-found", () => Throw(new CatalogTitleNotFoundException()));
        lists.MapGet("/conflict", () => Throw(new ConflictError(
            "Os títulos da lista mudaram desde a última consulta. Atualize os dados e tente novamente.",
            "A lista foi alterada.")));

        var catalog = app.MapGroup("/catalog").WithApplicationProblems();
        catalog.MapGet("/dependency", () => Throw(new CatalogProviderException(
            "catalog_rate_limited",
            "O provedor limitou as consultas.",
            StatusCodes.Status429TooManyRequests,
            TimeSpan.FromSeconds(2.2))));
        catalog.MapGet("/dependency-no-hint", () => Throw(new CatalogProviderException(
            "catalog_unavailable",
            "O catálogo está indisponível.",
            StatusCodes.Status503ServiceUnavailable,
            TimeSpan.Zero)));

        var genres = app.MapGroup("/genres").WithApplicationProblems(new ApplicationProblemOptions
        {
            Dependency = DependencyProblemStyle.TypeOnly,
        });
        genres.MapGet("/dependency", () => Throw(new CatalogProviderException(
            "catalog_unavailable",
            "O catálogo está indisponível.",
            StatusCodes.Status503ServiceUnavailable,
            TimeSpan.FromSeconds(5))));

        var tracking = app.MapGroup("/tracking").WithApplicationProblems(new ApplicationProblemOptions
        {
            NotFoundTitle = "Conteúdo não encontrado.",
            NotFoundDetail = "O conteúdo não está disponível no catálogo.",
            Dependency = DependencyProblemStyle.Opaque,
            OpaqueDependencyTitle = "Não foi possível consultar o catálogo.",
            OpaqueDependencyDetail = "Tente novamente mais tarde.",
        });
        tracking.MapGet("/dependency", () => Throw(new CatalogProviderException(
            "catalog_unavailable",
            "provider secret",
            StatusCodes.Status502BadGateway)));
        tracking.MapGet("/not-found", () => Throw(new TrackingContentNotFoundException()));
        tracking.MapGet("/conflict", () => Throw(new TrackingSeriesMismatchException()));

        var notifications = app.MapGroup("/notifications").WithApplicationProblems();
        notifications.MapGet("/conflict", () => Throw(new NotificationDeviceConflictException(
            "Este token já está associado.")));
        notifications.MapGet("/unavailable", () => Throw(new ServiceUnavailableError(
            "O registro push está desabilitado até configurar o projeto e as credenciais Expo.",
            "Notificações push indisponíveis.")));

        var plain = app.MapGroup("/plain").WithApplicationProblems();
        plain.MapGet("/argument", () => Throw(new ArgumentException("Texto.", "campo")));
        plain.MapGet("/unauthenticated", () => Throw(new UnauthenticatedError()));
        plain.MapGet("/unexpected", () => Throw(new InvalidOperationException("internal secret detail")));
    }

    private static IResult Throw(Exception exception) => throw exception;

    private static async Task<JsonElement> ReadProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static Task<string> ReadBodyAsync(HttpResponseMessage response) =>
        response.Content.ReadAsStringAsync();
}
