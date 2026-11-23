using System.Security.Claims;

namespace Reelix.Api.Authentication;

/// <summary>
/// The only place that reads the <c>sub</c> claim of the validated Supabase JWT. Handlers,
/// the legacy <c>TryGet*Id</c> helpers and the rate limiter all go through it, so the rule for
/// "who is the caller" lives in one spot. The owner always comes from the token, never from the
/// request body or the route.
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>The raw <c>sub</c> claim, or null when the token has none.</summary>
    public static string? GetSubject(this ClaimsPrincipal user) =>
        user.FindFirst("sub")?.Value;

    /// <summary>
    /// True when <c>sub</c> is a non-empty GUID; false (absent, not a GUID or
    /// <see cref="Guid.Empty"/>) means the caller must be answered with 401.
    /// </summary>
    public static bool ReadSubjectId(this ClaimsPrincipal user, out Guid subjectId) =>
        Guid.TryParse(user.GetSubject(), out subjectId) && subjectId != Guid.Empty;

    /// <summary>The caller's e-mail claim, when the token carries one.</summary>
    public static string? GetEmail(this ClaimsPrincipal user) =>
        user.FindFirst("email")?.Value;
}

/// <summary>The authenticated caller of the current request.</summary>
public interface ICurrentUser
{
    /// <summary>The Supabase user id (<c>sub</c>), or null when absent or invalid.</summary>
    Guid? SubjectId { get; }

    /// <summary>The e-mail claim of the token, when present.</summary>
    string? Email { get; }
}

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? SubjectId =>
        User is { } user && user.ReadSubjectId(out var subjectId) ? subjectId : null;

    public string? Email => User?.GetEmail();
}
