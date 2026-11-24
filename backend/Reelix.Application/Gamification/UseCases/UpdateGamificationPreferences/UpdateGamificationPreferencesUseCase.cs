using Reelix.Application.Authentication;

namespace Reelix.Application.Gamification;

/// <summary><c>PUT /gamification/preferences</c>: saves badge visibility and the ranking opt-in.</summary>
public sealed class UpdateGamificationPreferencesUseCase(
    IUserAccountRepository accounts,
    IGamificationPreferencesStore preferences)
{
    public async Task<UpdateGamificationPreferencesResult> ExecuteAsync(
        UpdateGamificationPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        var account = await accounts.EnsureAsync(command.SubjectId, cancellationToken);
        var saved = await preferences.SaveAsync(
            account.Id,
            command.BadgesPublic,
            command.RankingEnabled,
            cancellationToken);
        return new UpdateGamificationPreferencesResult(saved);
    }
}
