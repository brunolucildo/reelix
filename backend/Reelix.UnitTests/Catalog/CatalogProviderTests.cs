using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reelix.Application.Catalog;
using Reelix.Infrastructure.Catalog;

namespace Reelix.UnitTests.Catalog;

public sealed class CatalogProviderTests
{
    [Fact]
    public async Task TmdbSearch_MapsNormalizedFieldsAndUsesBearerAuthentication()
    {
        using var client = CreateClient(request =>
        {
            Assert.Contains("/3/search/multi?", request.RequestUri!.PathAndQuery);
            Assert.Contains("query=star%20wars", request.RequestUri.PathAndQuery);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
            return JsonResponse(
                """
            {
              "page": 1,
              "results": [{
                "id": 11,
                "media_type": "movie",
                "title": "A New Hope",
                "original_title": "Star Wars",
                "overview": "A space opera.",
                "release_date": "1977-05-25",
                "original_language": "en",
                "poster_path": "/poster.jpg",
                "backdrop_path": null
              }],
              "total_pages": 2,
              "total_results": 21
            }
            """);
        });
        var provider = new TmdbCatalogProvider(
            client,
            Options.Create(new TmdbCatalogOptions { AccessToken = "test-token" }),
            NullLogger<TmdbCatalogProvider>.Instance);

        var page = await provider.SearchAsync(
            "star wars",
            null,
            1,
            "pt-BR",
            CancellationToken.None);

        var title = Assert.Single(page.Items);
        Assert.Equal("11", title.ExternalId);
        Assert.Equal(CatalogContentType.Movie, title.ContentType);
        Assert.Equal("A New Hope", title.Title);
        Assert.Equal("Star Wars", title.OriginalTitle);
        Assert.Equal(new DateOnly(1977, 5, 25), title.ReleaseDate);
        Assert.Equal("https://image.tmdb.org/t/p/w500/poster.jpg", title.PosterUrl);
        Assert.Null(title.BackdropUrl);
        Assert.Equal(21, page.TotalResults);
    }

    [Fact]
    public async Task TmdbDetails_MapsGenresAndLeavesMissingDatesNull()
    {
        using var client = CreateClient(_ => JsonResponse(
            """
            {
              "id": 11,
              "title": "A New Hope",
              "genres": [{"id": 12, "name": "Adventure"}],
              "release_date": "",
              "runtime": 121
            }
            """));
        var provider = new TmdbCatalogProvider(
            client,
            Options.Create(new TmdbCatalogOptions { AccessToken = "test-token" }),
            NullLogger<TmdbCatalogProvider>.Instance);

        var title = await provider.GetDetailsAsync(
            CatalogContentType.Movie,
            "11",
            "pt-BR",
            CancellationToken.None);

        Assert.NotNull(title);
        Assert.Equal(["Adventure"], title.Genres);
        Assert.Null(title.ReleaseDate);
        Assert.Equal(121, title.RuntimeMinutes);
    }

    [Fact]
    public async Task TmdbRateLimit_ReportsRetryDelayWithoutProviderBody()
    {
        using var client = CreateClient(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
            response.Content = new StringContent("sensitive provider response");
            return response;
        });
        var provider = new TmdbCatalogProvider(
            client,
            Options.Create(new TmdbCatalogOptions { AccessToken = "test-token" }),
            NullLogger<TmdbCatalogProvider>.Instance);

        var exception = await Assert.ThrowsAsync<CatalogProviderException>(() =>
            provider.SearchAsync("test", CatalogContentType.Movie, 1, "en-US", CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(7), exception.RetryAfter);
        Assert.DoesNotContain("sensitive", exception.Message);
    }

    [Fact]
    public async Task OmdbDetails_NormalizesResponseAndDoesNotExposePosterUrl()
    {
        using var client = CreateClient(request =>
        {
            Assert.Contains("i=tt0133093", request.RequestUri!.Query);
            Assert.Contains("apikey=test-key", request.RequestUri.Query);
            return JsonResponse(
                """
                {
                  "Response": "True",
                  "imdbID": "tt0133093",
                  "Type": "movie",
                  "Title": "The Matrix",
                  "Released": "31 Mar 1999",
                  "Genre": "Action, Sci-Fi",
                  "Language": "English",
                  "Runtime": "136 min",
                  "Poster": "https://example.invalid/poster.jpg",
                  "Plot": "A computer hacker learns the truth."
                }
                """);
        });
        var provider = new OmdbCatalogProvider(
            client,
            Options.Create(new OmdbCatalogOptions { ApiKey = "test-key" }),
            NullLogger<OmdbCatalogProvider>.Instance);

        var title = await provider.GetDetailsAsync(
            CatalogContentType.Movie,
            "tt0133093",
            "pt-BR",
            CancellationToken.None);

        Assert.NotNull(title);
        Assert.Equal("tt0133093", title.ExternalId);
        Assert.Equal(["Action", "Sci-Fi"], title.Genres);
        Assert.Equal(new DateOnly(1999, 3, 31), title.ReleaseDate);
        Assert.Equal(136, title.RuntimeMinutes);
        Assert.Null(title.PosterUrl);
        Assert.Null(title.BackdropUrl);
    }

    [Fact]
    public async Task OmdbSeason_MapsAvailableEpisodeFields()
    {
        using var client = CreateClient(_ => JsonResponse(
            """
            {
              "Response": "True",
              "Title": "Breaking Bad",
              "Season": "1",
              "Episodes": [{
                "imdbID": "tt0959621",
                "Title": "Pilot",
                "Episode": "1",
                "Released": "20 Jan 2008"
              }]
            }
            """));
        var provider = new OmdbCatalogProvider(
            client,
            Options.Create(new OmdbCatalogOptions { ApiKey = "test-key" }),
            NullLogger<OmdbCatalogProvider>.Instance);

        var season = await provider.GetSeasonAsync(
            "tt0903747",
            1,
            "pt-BR",
            CancellationToken.None);

        Assert.NotNull(season);
        Assert.Equal("Breaking Bad", season.Title);
        var episode = Assert.Single(season.Episodes);
        Assert.Equal("tt0959621", episode.ExternalId);
        Assert.Equal("Pilot", episode.Title);
        Assert.Equal(new DateOnly(2008, 1, 20), episode.ReleaseDate);
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> createResponse)
    {
        var client = new HttpClient(new StubHandler(createResponse));
        client.BaseAddress = new Uri("https://api.test/");
        return client;
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> createResponse)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(createResponse(request));
    }
}
