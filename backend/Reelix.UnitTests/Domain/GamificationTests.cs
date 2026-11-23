using Reelix.Domain.Catalog;
using Reelix.Domain.Gamification;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Domain;

public sealed class GamificationTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;

    [Fact]
    public void InitialCatalog_UsesApprovedDeterministicThresholds()
    {
        var catalog = AchievementDefinition.InitialCatalog.ToDictionary(definition => definition.Code);

        Assert.Equal(5, catalog.Count);
        Assert.Equal(1, catalog["first-movie"].Threshold);
        Assert.Equal(10, catalog["cinephile"].Threshold);
        Assert.Equal(10, catalog["first-marathon"].Threshold);
        Assert.Equal(3, catalog["series-in-focus"].Threshold);
        Assert.Equal(5, catalog["genre-explorer"].Threshold);
        Assert.All(catalog.Values, definition => Assert.True(definition.IsActive));
    }

    [Fact]
    public void UserAchievement_RevokeAndReactivationPreserveFirstEarnedDate()
    {
        var achievement = UserAchievement.Create(
            Guid.NewGuid(),
            AchievementDefinition.InitialCatalog[0].Id,
            Now);
        var firstEarnedAt = achievement.FirstEarnedAt;
        var revokedAtInstant = Now.AddDays(1);
        var reactivatedAtInstant = Now.AddDays(2);

        achievement.Revoke(revokedAtInstant);
        var revokedAt = achievement.LastRevokedAt;
        achievement.Revoke(Now.AddDays(1.5));
        achievement.Earn(reactivatedAtInstant);

        Assert.Equal(UserAchievementStatus.Earned, achievement.Status);
        Assert.Equal(Now, firstEarnedAt);
        Assert.Equal(firstEarnedAt, achievement.FirstEarnedAt);
        Assert.Equal(reactivatedAtInstant, achievement.LastEarnedAt);
        Assert.Equal(revokedAtInstant, revokedAt);
        Assert.Equal(revokedAt, achievement.LastRevokedAt);
        Assert.Equal(reactivatedAtInstant, achievement.ReactivatedAt);
    }

    [Fact]
    public void Preferences_DefaultToPublicBadgesAndOptInRanking()
    {
        var preferences = GamificationPreferences.Create(Guid.NewGuid(), Now);

        Assert.True(preferences.BadgesPublic);
        Assert.False(preferences.RankingEnabled);
        Assert.Equal(Now, preferences.UpdatedAt);

        preferences.Update(false, true, Now.AddHours(1));

        Assert.False(preferences.BadgesPublic);
        Assert.True(preferences.RankingEnabled);
        Assert.Equal(Now.AddHours(1), preferences.UpdatedAt);
    }

    [Fact]
    public void CatalogMetadata_PreservesKnownRuntimeWhenLaterSnapshotOmitsIt()
    {
        var reference = ExternalCatalogReference.Create("tmdb", "123", "movie", Now);

        reference.UpdateMetadata(120, false, true, Now.AddMinutes(5));
        reference.UpdateMetadata(null, false, false, Now.AddMinutes(10));

        Assert.Equal(Now, reference.CreatedAt);
        Assert.Equal(120, reference.RuntimeMinutes);
        Assert.Equal(1, reference.MetadataVersion);
        Assert.Equal(Now.AddMinutes(5), reference.MetadataUpdatedAt);
    }
}
