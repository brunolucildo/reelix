using Microsoft.EntityFrameworkCore;
using Npgsql;
using Reelix.Application.Users;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Users;

public sealed class UserProfileRepository(ReelixDbContext dbContext)
    : IUserProfileRepository
{
    public async Task<UserProfile> GetOrCreateAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles
            .SingleOrDefaultAsync(profile => profile.AccountId == accountId, cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        profile = UserProfile.Create(accountId);
        dbContext.UserProfiles.Add(profile);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.Entry(profile).State = EntityState.Detached;
            return await dbContext.UserProfiles.SingleAsync(
                candidate => candidate.AccountId == accountId,
                cancellationToken);
        }

        return profile;
    }

    public Task<UserProfile?> GetPublicAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.UserProfiles.SingleOrDefaultAsync(
            profile => profile.AccountId == accountId && profile.ProfileIsPublic,
            cancellationToken);

    public Task SaveAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        dbContext.UserProfiles.Update(profile);
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}