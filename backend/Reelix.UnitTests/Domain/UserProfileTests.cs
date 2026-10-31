using Reelix.Domain.Users;

namespace Reelix.UnitTests.Domain;

public sealed class UserProfileTests
{
    [Fact]
    public void Create_DefaultsAllProfileVisibilityToPrivate()
    {
        var profile = UserProfile.Create(Guid.NewGuid());

        Assert.False(profile.ProfileIsPublic);
        Assert.False(profile.FavoritesArePublic);
        Assert.False(profile.WatchHistoryIsPublic);
    }

    [Fact]
    public void Update_NormalizesOptionalTextAndStoresPrivacyChoices()
    {
        var profile = UserProfile.Create(Guid.NewGuid());

        profile.Update("  Cinefila  ", "  Amo cinema. ", true, false, false);

        Assert.Equal("Cinefila", profile.DisplayName);
        Assert.Equal("Amo cinema.", profile.Biography);
        Assert.True(profile.ProfileIsPublic);
        Assert.False(profile.FavoritesArePublic);
        Assert.False(profile.WatchHistoryIsPublic);
    }

    [Theory]
    [InlineData(51, 0)]
    [InlineData(0, 281)]
    public void Update_RejectsTextAboveFieldLimits(int displayNameLength, int biographyLength)
    {
        var profile = UserProfile.Create(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => profile.Update(
            displayNameLength == 0 ? null : new string('n', displayNameLength),
            biographyLength == 0 ? null : new string('b', biographyLength),
            false,
            false,
            false));
    }
}