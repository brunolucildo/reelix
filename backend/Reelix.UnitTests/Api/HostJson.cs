using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Reelix.UnitTests.Api;

/// <summary>
/// Test helper of the DTO pattern (spec 0013, R8): serializes a value with the very JSON options of
/// the running host (camelCase, enums as camelCase strings), so a contract test compares the exact
/// text a client would receive with a golden JSON, never a re-implementation of the options.
/// </summary>
internal static class HostJson
{
    public static string Serialize(IServiceProvider services, object value) =>
        JsonSerializer.Serialize(
            value,
            value.GetType(),
            services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions);

    /// <summary>Compares two JSON texts structurally, ignoring insignificant white space.</summary>
    public static void AssertSameJson(string expected, string actual) =>
        Assert.Equal(Normalize(expected), Normalize(actual));

    private static string Normalize(string json) =>
        JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement);
}
