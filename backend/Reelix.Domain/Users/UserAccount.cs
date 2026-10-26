namespace Reelix.Domain.Users;

public sealed class UserAccount
{
    private UserAccount(Guid id, Guid supabaseUserId, DateTimeOffset createdAt)
    {
        Id = id;
        SupabaseUserId = supabaseUserId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SupabaseUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static UserAccount Create(Guid supabaseUserId)
    {
        if (supabaseUserId == Guid.Empty)
        {
            throw new ArgumentException("A valid Supabase user identifier is required.", nameof(supabaseUserId));
        }

        return new UserAccount(Guid.NewGuid(), supabaseUserId, DateTimeOffset.UtcNow);
    }
}
