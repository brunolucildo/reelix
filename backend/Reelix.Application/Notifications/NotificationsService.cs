using System.Text.Json;
using Reelix.Application.Authentication;
using Reelix.Domain.Notifications;

namespace Reelix.Application.Notifications;

public sealed class NotificationsService(
    IUserAccountRepository accounts,
    INotificationsRepository notificationsRepository,
    IPushRegistrationConfiguration pushConfiguration)
{
    private const int PageSize = 25;

    public async Task<NotificationPreferencesData> GetPreferencesAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        return ToData(await notificationsRepository.GetPreferencesAsync(accountId, cancellationToken));
    }

    public async Task<NotificationPreferencesData> UpdatePreferencesAsync(
        Guid supabaseUserId,
        UpdateNotificationPreferences preferences,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        return ToData(await notificationsRepository.UpdatePreferencesAsync(
            accountId,
            preferences,
            cancellationToken));
    }

    public async Task<NotificationDeviceData> RegisterDeviceAsync(
        Guid supabaseUserId,
        RegisterNotificationDevice registration,
        CancellationToken cancellationToken)
    {
        if (registration.ExpoPushToken is null)
        {
            throw new ArgumentException("Informe um token Expo.", nameof(registration));
        }

        if (!pushConfiguration.IsEnabled ||
            pushConfiguration.ProjectId is null ||
            registration.ProjectId != pushConfiguration.ProjectId)
        {
            throw new InvalidOperationException(
                "O registro push está desabilitado até configurar o projeto e as credenciais Expo.");
        }

        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        return ToData(await notificationsRepository.RegisterDeviceAsync(
            accountId,
            registration,
            cancellationToken));
    }

    public async Task<bool> DeactivateDeviceAsync(
        Guid supabaseUserId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        return await notificationsRepository.DeactivateDeviceAsync(
            accountId,
            deviceId,
            cancellationToken);
    }

    public async Task<NotificationInboxPage> GetInboxAsync(
        Guid supabaseUserId,
        string? encodedCursor,
        CancellationToken cancellationToken)
    {
        var cursor = DecodeCursor(encodedCursor);
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        var items = await notificationsRepository.GetInboxPageAsync(
            accountId,
            cursor,
            PageSize + 1,
            cancellationToken);
        var hasMore = items.Count > PageSize;
        var page = items.Take(PageSize).ToArray();
        return new NotificationInboxPage(
            page,
            hasMore && page.Length > 0
                ? EncodeCursor(page[^1].CreatedAt, page[^1].Id)
                : null);
    }

    public async Task<NotificationUnreadCount> GetUnreadCountAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        return new NotificationUnreadCount(
            await notificationsRepository.GetUnreadCountAsync(accountId, cancellationToken));
    }

    public async Task<bool> MarkReadAsync(
        Guid supabaseUserId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        return await notificationsRepository.MarkReadAsync(
            accountId,
            notificationId,
            cancellationToken);
    }

    private async Task<Guid> GetAccountIdAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken) =>
        (await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken)).Id;

    private static NotificationPreferencesData ToData(NotificationPreferences preferences) =>
        new(preferences.SocialEnabled, preferences.NewEpisodeEnabled, preferences.PremiereEnabled);

    private static NotificationDeviceData ToData(NotificationDevice device) =>
        new(device.Id, device.Platform, device.CreatedAt, device.LastRegisteredAt);

    private static string EncodeCursor(DateTimeOffset createdAt, Guid id)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new NotificationCursor(createdAt, id));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static NotificationCursor? DecodeCursor(string? encodedCursor)
    {
        if (string.IsNullOrWhiteSpace(encodedCursor))
        {
            return null;
        }

        if (encodedCursor.Length > 200)
        {
            throw new ArgumentException("O cursor da caixa de entrada é inválido.", nameof(encodedCursor));
        }

        try
        {
            var value = encodedCursor.Replace('-', '+').Replace('_', '/');
            value = value.PadRight(value.Length + ((4 - value.Length % 4) % 4), '=');
            var cursor = JsonSerializer.Deserialize<NotificationCursor>(Convert.FromBase64String(value));
            if (cursor is null || cursor.Id == Guid.Empty)
            {
                throw new ArgumentException("O cursor da caixa de entrada é inválido.", nameof(encodedCursor));
            }

            return cursor;
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("O cursor da caixa de entrada é inválido.", nameof(encodedCursor), exception);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("O cursor da caixa de entrada é inválido.", nameof(encodedCursor), exception);
        }
    }
}
