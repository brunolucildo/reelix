namespace Reelix.Application.Errors;

/// <summary>
/// Base of the typed application errors (ADR XXXX-0013). A flow signals an expected failure by
/// throwing one of them; the Api maps it to an HTTP problem in a single place. The exception
/// <see cref="Exception.Message"/> is the user-facing detail (pt-BR); <see cref="Title"/> is the
/// optional user-facing summary when the error itself decides it.
/// </summary>
public abstract class ApplicationError : Exception
{
    protected ApplicationError(string message, string? title = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Title = title;
    }

    /// <summary>The summary shown to the user, or null to let the route decide.</summary>
    public string? Title { get; }
}

/// <summary>The request is well formed but breaks a rule (HTTP 400).</summary>
public class ValidationError(string message, string? title = null, Exception? innerException = null)
    : ApplicationError(message, title, innerException);

/// <summary>The resource does not exist or is not visible to the caller (HTTP 404).</summary>
public class NotFoundError(string message, string? title = null, Exception? innerException = null)
    : ApplicationError(message, title, innerException);

/// <summary>The request conflicts with the current state of the resource (HTTP 409).</summary>
public class ConflictError(string message, string? title = null, Exception? innerException = null)
    : ApplicationError(message, title, innerException);

/// <summary>A capability is turned off or not configured (HTTP 503).</summary>
public class ServiceUnavailableError(string message, string? title = null, Exception? innerException = null)
    : ApplicationError(message, title, innerException);

/// <summary>The caller is not authenticated (HTTP 401).</summary>
public class UnauthenticatedError(string message = "Autenticação necessária.", Exception? innerException = null)
    : ApplicationError(message, null, innerException);

/// <summary>
/// An external dependency (a catalog provider, for example) failed. Carries a stable
/// <see cref="Code"/>, the status the failure is reported with and an optional
/// <see cref="RetryAfter"/> hint.
/// </summary>
/// <remarks>
/// <see cref="StatusCode"/> is a transitional plain integer: the catalog adapters already report
/// it this way and spec 0015 moves the translation to the adapter.
/// </remarks>
public class ExternalDependencyError(
    string code,
    string message,
    int statusCode,
    TimeSpan? retryAfter = null,
    Exception? innerException = null) : ApplicationError(message, null, innerException)
{
    public string Code { get; } = code;

    public int StatusCode { get; } = statusCode;

    public TimeSpan? RetryAfter { get; } = retryAfter;
}
