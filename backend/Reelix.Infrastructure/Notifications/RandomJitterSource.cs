using Reelix.Application.Notifications;

namespace Reelix.Infrastructure.Notifications;

/// <summary>The jitter of the push retry backoff, drawn from the shared random generator.</summary>
public sealed class RandomJitterSource : IJitterSource
{
    public double NextUnit() => Random.Shared.NextDouble();
}
