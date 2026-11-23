using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reelix.Api.RateLimiting;

namespace Reelix.UnitTests.Api;

/// <summary>
/// A minimal in-memory host with the same error and rate limit components as the real API
/// (problem details, status code pages, exception handler and the shared rate limiter) and
/// routes written by the test. It lets a test throw any error on purpose without touching the
/// database or a provider.
/// </summary>
internal sealed class SyntheticApiHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private SyntheticApiHost(WebApplication app, CapturingLoggerProvider logs)
    {
        _app = app;
        Logs = logs;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public CapturingLoggerProvider Logs { get; }

    public static async Task<SyntheticApiHost> StartAsync(Action<WebApplication> mapRoutes)
    {
        var logs = new CapturingLoggerProvider();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(logs);
        builder.Services.AddProblemDetails();
        builder.Services.AddReelixRateLimiting();

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.Use(async (context, next) =>
        {
            // Lets a test act as a caller without a real token: the header carries the subject.
            if (context.Request.Headers.TryGetValue("X-Test-Subject", out var subject))
            {
                context.User = new ClaimsPrincipal(
                    new ClaimsIdentity([new Claim("sub", subject.ToString())], "test"));
            }

            await next();
        });
        app.UseRateLimiter();
        mapRoutes(app);
        await app.StartAsync();
        return new SyntheticApiHost(app, logs);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<(LogLevel Level, string Message)> _entries = [];

    public IReadOnlyList<(LogLevel Level, string Message)> Entries
    {
        get
        {
            lock (_entries)
            {
                return _entries.ToArray();
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(CapturingLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (owner._entries)
            {
                owner._entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}
