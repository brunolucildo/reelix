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

    [Fact]
    public void FriendSharingConsent_RecordsActivationAndClearsItWhenRevoked()
    {
        var profile = UserProfile.Create(Guid.NewGuid());

        profile.Update(
            null,
            null,
            false,
            false,
            false,
            watchHistorySharedWithFriends: true,
            ratingsSharedWithFriends: true);

        Assert.NotNull(profile.WatchHistorySharedWithFriendsSince);
        Assert.NotNull(profile.RatingsSharedWithFriendsSince);

        profile.Update(null, null, false, false, false);

        Assert.False(profile.WatchHistorySharedWithFriends);
        Assert.False(profile.RatingsSharedWithFriends);
        Assert.Null(profile.WatchHistorySharedWithFriendsSince);
        Assert.Null(profile.RatingsSharedWithFriendsSince);
    }

    [Theory]
    [InlineData("noir")]
    [InlineData("cosmic")]
    [InlineData("phantom")]
    [InlineData("comedy")]
    [InlineData("animation")]
    [InlineData("adventure")]
    [InlineData("robot")]
    [InlineData("vampire")]
    [InlineData("maestro")]
    public void Avatar_AcceptsFixedChoicesAndPreservesThemWhenProfileTextChanges(string key)
    {
        var profile = UserProfile.Create(Guid.NewGuid());
        profile.SetAvatar(key);
        profile.Update("Nome", null, false, false, false);
        Assert.Equal(key, profile.AvatarKey);
        profile.SetAvatar("initials");
        Assert.Null(profile.AvatarKey);
    }

    [Theory]
    [InlineData("https://example.com/avatar.png")]
    [InlineData("unknown")]
    [InlineData("")]
    public void Avatar_RejectsUnknownChoices(string key) =>
        Assert.Throws<ArgumentException>(() => UserProfile.Create(Guid.NewGuid()).SetAvatar(key));

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