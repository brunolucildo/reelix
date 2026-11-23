namespace Reelix.Api.Errors;

public static class ApplicationProblemExtensions
{
    /// <summary>
    /// Maps the typed application errors and <see cref="ArgumentException"/> thrown by the
    /// endpoints (of a group or a single route) to problem responses. Call it once per group;
    /// pass options when the routes word their errors differently from the defaults.
    /// </summary>
    public static TBuilder WithApplicationProblems<TBuilder>(
        this TBuilder builder,
        ApplicationProblemOptions? options = null)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(options ?? new ApplicationProblemOptions());
        builder.AddEndpointFilter<TBuilder, ApplicationProblemFilter>();
        return builder;
    }
}
