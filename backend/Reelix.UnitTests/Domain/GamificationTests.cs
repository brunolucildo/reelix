using Reelix.Domain.Catalog;
using Reelix.Domain.Gamification;

namespace Reelix.UnitTests.Domain;

public sealed class GamificationTests
{
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
            AchievementDefinition.InitialCatalog[0].Id);
        var firstEarnedAt = achievement.FirstEarnedAt;

        achievement.Revoke();
        var revokedAt = achievement.LastRevokedAt;
        achievement.Revoke();
        achievement.Earn();

        Assert.Equal(UserAchievementStatus.Earned, achievement.Status);
        Assert.Equal(firstEarnedAt, achievement.FirstEarnedAt);
        Assert.True(achievement.LastEarnedAt >= firstEarnedAt);
        Assert.Equal(revokedAt, achievement.LastRevokedAt);
        Assert.NotNull(achievement.ReactivatedAt);
    }

    [Fact]
    public void Preferences_DefaultToPublicBadgesAndOptInRanking()
    {
        var preferences = GamificationPreferences.Create(Guid.NewGuid());

        Assert.True(preferences.BadgesPublic);
        Assert.False(preferences.RankingEnabled);

        preferences.Update(false, true);

        Assert.False(preferences.BadgesPublic);
        Assert.True(preferences.RankingEnabled);
    }

    [Fact]
    public void CatalogMetadata_PreservesKnownRuntimeWhenLaterSnapshotOmitsIt()
    {
        var reference = ExternalCatalogReference.Create("tmdb", "123", "movie");

        reference.UpdateMetadata(120, false, true);
        reference.UpdateMetadata(null, false, false);

        Assert.Equal(120, reference.RuntimeMinutes);
        Assert.Equal(1, reference.MetadataVersion);
        Assert.NotNull(reference.MetadataUpdatedAt);
    }
}
