using Reelix.Domain.Users;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Domain;

public sealed class UserAccountTests
{
    [Fact]
    public void Create_RejectsEmptySupabaseIdentifier()
    {
        Assert.Throws<ArgumentException>(() =>
            UserAccount.Create(Guid.Empty, FixedTimeProvider.DefaultNow));
    }

    [Fact]
    public void Create_StoresStableSupabaseIdentifierAndTheReceivedInstant()
    {
        var supabaseUserId = Guid.NewGuid();

        var account = UserAccount.Create(supabaseUserId, FixedTimeProvider.DefaultNow);

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(supabaseUserId, account.SupabaseUserId);
        Assert.Equal(FixedTimeProvider.DefaultNow, account.CreatedAt);
        Assert.Equal(TimeSpan.Zero, account.CreatedAt.Offset);
    }
}
