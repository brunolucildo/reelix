namespace Reelix.Api.Errors;

/// <summary>
/// Route-level metadata that tells the problem mapper how a route words its errors. It carries
/// the pieces that today differ per route (titles, how much of a provider failure is exposed) so
/// that moving a flow to the shared mapper does not change its HTTP contract.
/// </summary>
public sealed record ApplicationProblemOptions
{
    /// <summary>Title of a 400 caused by an <see cref="ArgumentException"/> or a validation error.</summary>
    public string ValidationTitle { get; init; } = "Solicitação inválida.";

    /// <summary>Title of a 404 when the error does not carry its own.</summary>
    public string NotFoundTitle { get; init; } = "Recurso não encontrado.";

    /// <summary>Fixed detail of a 404; null uses the message of the error.</summary>
    public string? NotFoundDetail { get; init; }

    /// <summary>How much of an external dependency failure the response exposes.</summary>
    public DependencyProblemStyle Dependency { get; init; } = DependencyProblemStyle.Detailed;

    /// <summary>Title used by <see cref="DependencyProblemStyle.Opaque"/>.</summary>
    public string OpaqueDependencyTitle { get; init; } = "Não foi possível concluir a solicitação.";

    /// <summary>Detail used by <see cref="DependencyProblemStyle.Opaque"/>.</summary>
    public string OpaqueDependencyDetail { get; init; } = "Tente novamente mais tarde.";
}

/// <summary>How an external dependency failure is shown to the caller.</summary>
public enum DependencyProblemStyle
{
    /// <summary>Title is the message, with the problem <c>type</c>, the <c>code</c> extension and <c>Retry-After</c>.</summary>
    Detailed,

    /// <summary>Title is the message and the problem <c>type</c> is set; no <c>code</c> and no <c>Retry-After</c>.</summary>
    TypeOnly,

    /// <summary>A fixed title and detail; nothing from the provider is exposed.</summary>
    Opaque,
}
