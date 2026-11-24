using Reelix.Application.Authentication;
using Reelix.Application.Common;

namespace Reelix.Application.Notifications;

/// <summary>
/// <c>GET /notifications/inbox</c>: a page of 25 notifications, newest first. It never writes: an
/// account that does not exist yet has an empty inbox.
/// </summary>
public sealed class GetNotificationInboxUseCase(
    IUserAccountRepository accounts,
    IUserNotifications notifications)
{
    private const int PageSize = 25;

    public async Task<GetNotificationInboxResult> ExecuteAsync(
        GetNotificationInboxQuery query,
        CancellationToken cancellationToken)
    {
        var cursor = DecodeCursor(query.Cursor);
        var account = await accounts.FindAsync(query.SubjectId, cancellationToken);
        if (account is null)
        {
            return new GetNotificationInboxResult([], null);
        }

        var items = await notifications.ListPageAsync(account.Id, cursor, PageSize + 1, cancellationToken);
        var hasMore = items.Count > PageSize;
        var page = items.Take(PageSize).ToArray();
        return new GetNotificationInboxResult(
            page,
            hasMore && page.Length > 0
                ? CursorCodec.EncodeJson(new NotificationCursor(page[^1].CreatedAt, page[^1].Id))
                : null);
    }

    private static NotificationCursor? DecodeCursor(string? encodedCursor)
    {
        if (string.IsNullOrWhiteSpace(encodedCursor))
        {
            return null;
        }

        var cursor = CursorCodec.DecodeJson<NotificationCursor>(encodedCursor, InvalidCursor);
        return cursor is null || cursor.Id == Guid.Empty
            ? throw InvalidCursor(null)
            : cursor;
    }

    // CursorCodec asks for an ArgumentException; the HTTP mapper turns it into a 400 whose detail
    // carries the message without the " (Parameter '...')" suffix.
    private static ArgumentException InvalidCursor(Exception? innerException) =>
        new("O cursor da caixa de entrada é inválido.", "encodedCursor", innerException);
}
