namespace Reelix.Application.Notifications;

/// <param name="SubjectId">The Supabase user id (<c>sub</c>) of the caller.</param>
/// <param name="Cursor">The opaque cursor of the previous page, if any.</param>
public sealed record GetNotificationInboxQuery(Guid SubjectId, string? Cursor);
