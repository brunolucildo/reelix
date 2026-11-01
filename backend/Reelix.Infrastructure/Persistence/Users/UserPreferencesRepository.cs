using Microsoft.EntityFrameworkCore;
using Npgsql;
using Reelix.Application.Catalog;
using Reelix.Application.Users;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Users;

public sealed class UserPreferencesRepository(ReelixDbContext dbContext)
    : IUserPreferencesRepository
{
    public async Task<OnboardingPreferences?> GetExistingAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var preferences = await FindAsync(accountId, cancellationToken);
        return preferences is null ? null : ToContract(preferences);
    }

    public async Task<OnboardingPreferences> GetOrCreateAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var preferences = await FindAsync(accountId, cancellationToken);
        if (preferences is null)
        {
            preferences = UserPreferences.Create(accountId);
            dbContext.UserPreferences.Add(preferences);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                dbContext.Entry(preferences).State = EntityState.Detached;
                preferences = await FindAsync(accountId, cancellationToken)
                    ?? throw new InvalidOperationException("The preference row could not be reloaded after a concurrent insert.");
            }
        }

        return ToContract(preferences);
    }

    public async Task SaveProgressAsync(
        Guid accountId,
        SaveOnboardingProgress progress,
        CancellationToken cancellationToken)
    {
        await GetOrCreateAsync(accountId, cancellationToken);
        var preferences = await FindAsync(accountId, cancellationToken)
            ?? throw new InvalidOperationException("The preference row could not be loaded for update.");

        preferences.UpdateProgress(progress.CurrentStep.ToString());
        dbContext.UserGenrePreferences.RemoveRange(preferences.GenrePreferences);
        dbContext.UserFavoriteTitles.RemoveRange(preferences.FavoriteTitles);
        preferences.GenrePreferences.Clear();
        preferences.FavoriteTitles.Clear();

        AddGenres(accountId, CatalogContentType.Movie, progress.MovieGenreIds);
        AddGenres(accountId, CatalogContentType.Series, progress.SeriesGenreIds);
        AddFavorites(accountId, CatalogContentType.Movie, progress.FavoriteMovieIds);
        AddFavorites(accountId, CatalogContentType.Series, progress.FavoriteSeriesIds);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await GetOrCreateAsync(accountId, cancellationToken);
        var preferences = await FindAsync(accountId, cancellationToken)
            ?? throw new InvalidOperationException("The preference row could not be loaded for completion.");

        preferences.Complete();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CatalogTitlesExistAsync(
        IReadOnlyCollection<Guid> titleIds,
        CatalogContentType contentType,
        CancellationToken cancellationToken)
    {
        var uniqueIds = titleIds.Distinct().ToArray();
        if (uniqueIds.Length == 0)
        {
            return true;
        }

        var expectedType = contentType == CatalogContentType.Movie ? "Movie" : "Series";
        var count = await dbContext.ExternalCatalogReferences
            .Where(reference => uniqueIds.Contains(reference.Id) && reference.ContentType == expectedType)
            .Select(reference => reference.Id)
            .Distinct()
            .CountAsync(cancellationToken);
        return count == uniqueIds.Length;
    }

    private Task<UserPreferences?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.UserPreferences
            .Include(preferences => preferences.GenrePreferences)
            .Include(preferences => preferences.FavoriteTitles)
            .SingleOrDefaultAsync(preferences => preferences.AccountId == accountId, cancellationToken);

    private void AddGenres(Guid accountId, CatalogContentType contentType, IEnumerable<int> genreIds)
    {
        foreach (var genreId in genreIds)
        {
            var preference = UserGenrePreference.Create(accountId, contentType.ToString(), genreId);
            dbContext.UserGenrePreferences.Add(preference);
            dbContext.Entry(preference).State = EntityState.Added;
        }
    }

    private void AddFavorites(Guid accountId, CatalogContentType contentType, IEnumerable<Guid> titleIds)
    {
        foreach (var titleId in titleIds)
        {
            dbContext.UserFavoriteTitles.Add(
                UserFavoriteTitle.Create(accountId, titleId, contentType.ToString()));
        }
    }

    private static OnboardingPreferences ToContract(UserPreferences preferences) => new(
        preferences.CompletedAt is not null,
        Enum.TryParse<OnboardingStep>(preferences.CurrentStep, out var step)
            ? step
            : OnboardingStep.Welcome,
        preferences.GenrePreferences
            .Where(item => item.ContentType == CatalogContentType.Movie.ToString())
            .Select(item => item.GenreId)
            .Order()
            .ToArray(),
        preferences.GenrePreferences
            .Where(item => item.ContentType == CatalogContentType.Series.ToString())
            .Select(item => item.GenreId)
            .Order()
            .ToArray(),
        preferences.FavoriteTitles
            .Where(item => item.ContentType == CatalogContentType.Movie.ToString())
            .Select(item => item.TitleId)
            .ToArray(),
        preferences.FavoriteTitles
            .Where(item => item.ContentType == CatalogContentType.Series.ToString())
            .Select(item => item.TitleId)
            .ToArray());

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}