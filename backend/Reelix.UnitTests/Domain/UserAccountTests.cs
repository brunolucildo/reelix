using Reelix.Domain.Users;

namespace Reelix.UnitTests.Domain;

public sealed class UserAccountTests
{
    [Fact]
    public void Create_RejectsEmptySupabaseIdentifier()
    {
        Assert.Throws<ArgumentException>(() => UserAccount.Create(Guid.Empty));
    }

    [Fact]
    public void Create_StoresStableSupabaseIdentifierAndUtcTimestamp()
    {
        var supabaseUserId = Guid.NewGuid();

        var account = UserAccount.Create(supabaseUserId);

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(supabaseUserId, account.SupabaseUserId);
        Assert.Equal(TimeSpan.Zero, account.CreatedAt.Offset);
    }
}
