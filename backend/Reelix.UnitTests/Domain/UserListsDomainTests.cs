using Reelix.Domain.Users;

namespace Reelix.UnitTests.Domain;

public sealed class UserListsDomainTests
{
    [Fact]
    public void CreateCustomList_TrimsTextAndDefaultsDescriptionToNull()
    {
        var list = UserCustomList.Create(Guid.NewGuid(), "  Para assistir  ", "  ");

        Assert.Equal("Para assistir", list.Name);
        Assert.Null(list.Description);
        Assert.Equal(list.CreatedAt, list.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("                                                                                 ")]
    public void CreateCustomList_RejectsInvalidName(string name)
    {
        Assert.Throws<ArgumentException>(() => UserCustomList.Create(Guid.NewGuid(), name, null));
    }

    [Fact]
    public void CreateCustomList_RejectsDescriptionAboveLimit()
    {
        Assert.Throws<ArgumentException>(() => UserCustomList.Create(
            Guid.NewGuid(),
            "Favoritos",
            new string('d', 501)));
    }

    [Fact]
    public void UpdateCustomList_NormalizesFieldsAndChangesTimestamp()
    {
        var list = UserCustomList.Create(
            Guid.NewGuid(),
            "Antiga",
            null,
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        list.Update("  Nova  ", "  Coleção pessoal  ", DateTimeOffset.Parse("2026-01-02T00:00:00Z"));

        Assert.Equal("Nova", list.Name);
        Assert.Equal("Coleção pessoal", list.Description);
        Assert.Equal(DateTimeOffset.Parse("2026-01-02T00:00:00Z"), list.UpdatedAt);
    }

    [Theory]
    [InlineData("Movie")]
    [InlineData("episode")]
    public void CreateWatchlistTitle_RejectsUnsupportedContentType(string contentType)
    {
        Assert.Throws<ArgumentException>(() =>
            UserWatchlistTitle.Create(Guid.NewGuid(), Guid.NewGuid(), contentType));
    }

    [Fact]
    public void CreateListTitle_RejectsNegativePosition()
    {
        Assert.Throws<ArgumentException>(() =>
            UserCustomListTitle.Create(Guid.NewGuid(), Guid.NewGuid(), "movie", -1));
    }
}
