using System.Globalization;
using Reelix.Application.Authentication;

namespace Reelix.Application.Gamification;

public sealed class GamificationService(
    IGamificationRepository repository,
    IUserAccountRepository accounts)
{
    public async Task<GamificationDashboard> GetDashboardAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken) =>
        await repository.GetDashboardAsync(
            await GetAccountIdAsync(supabaseUserId, cancellationToken), cancellationToken);

    public async Task<GamificationPreferencesData> UpdatePreferencesAsync(
        Guid supabaseUserId,
        bool badgesPublic,
        bool rankingEnabled,
        CancellationToken cancellationToken) =>
        await repository.UpdatePreferencesAsync(
            await GetAccountIdAsync(supabaseUserId, cancellationToken),
            badgesPublic,
            rankingEnabled,
            cancellationToken);

    public Task<PublicBadgesData> GetPublicBadgesAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        repository.GetPublicBadgesAsync(accountId, cancellationToken);

    public async Task<GamificationStatistics> GetStatisticsAsync(
        Guid supabaseUserId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        var start = periodStart.ToUniversalTime();
        var end = periodEnd.ToUniversalTime();
        if (start >= end || end - start > TimeSpan.FromDays(366))
        {
            throw new ArgumentException(
                "O período deve ser válido e não pode exceder 366 dias.");
        }

        return await repository.GetStatisticsAsync(
            await GetAccountIdAsync(supabaseUserId, cancellationToken), start, end, cancellationToken);
    }

    public async Task<GamificationRankingPage> GetMonthlyRankingAsync(
        Guid supabaseUserId,
        int year,
        int month,
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        if (year is < 2000 or > 9998 || month is < 1 or > 12)
        {
            throw new ArgumentException("Informe um ano e mês válidos.");
        }

        if (limit is < 1 or > 50)
        {
            throw new ArgumentException("O limite deve estar entre 1 e 50.");
        }

        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var cursorValue = ParseCursor(cursor);
        return await repository.GetMonthlyRankingAsync(
            await GetAccountIdAsync(supabaseUserId, cancellationToken),
            start,
            start.AddMonths(1),
            cursorValue,
            limit,
            cancellationToken);
    }

    private async Task<Guid> GetAccountIdAsync(Guid supabaseUserId, CancellationToken cancellationToken)
    {
        if (supabaseUserId == Guid.Empty)
        {
            throw new ArgumentException("Informe uma conta autenticada.", nameof(supabaseUserId));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return account.Id;
    }

    private static GamificationRankingCursor? ParseCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var encoded = cursor.Replace('-', '+').Replace('_', '/');
            encoded = encoded.PadRight(encoded.Length + (4 - encoded.Length % 4) % 4, '=');
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var parts = decoded.Split(':', 2);
            if (parts.Length == 2 &&
                int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var count) &&
                count >= 0 &&
                Guid.TryParse(parts[1], out var accountId) &&
                accountId != Guid.Empty)
            {
                return new GamificationRankingCursor(count, accountId);
            }
        }
        catch (FormatException)
        {
        }

        throw new ArgumentException("O cursor de paginação é inválido.", nameof(cursor));
    }
}
