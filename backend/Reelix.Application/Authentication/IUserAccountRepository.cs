using Reelix.Domain.Users;

namespace Reelix.Application.Authentication;

/// <summary>
/// Port for the internal account that belongs to a Supabase identity. Reading and provisioning
/// are separate, explicit steps: <see cref="FindAsync"/> never writes, <see cref="EnsureAsync"/>
/// creates the account when it is missing.
/// </summary>
public interface IUserAccountRepository
{
    /// <summary>Returns the account of the Supabase user, or null when it does not exist yet.</summary>
    Task<UserAccount?> FindAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the account of the Supabase user, creating it when missing. Concurrent calls for
    /// the same user resolve to a single row (unique index on the Supabase user id).
    /// </summary>
    Task<UserAccount> EnsureAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Transitional name kept until the modules adopt <see cref="FindAsync"/> and
    /// <see cref="EnsureAsync"/> (specs 0014 to 0016); it behaves exactly like <see cref="EnsureAsync"/>.
    /// </summary>
    Task<UserAccount> GetOrCreateAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken) =>
        EnsureAsync(supabaseUserId, cancellationToken);
}
