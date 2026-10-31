using Reelix.Application.Authentication;
using Reelix.Application.Catalog;

namespace Reelix.Application.Users;

public sealed class OnboardingService(
    IUserAccountRepository accounts,
    IUserPreferencesRepository preferences,
    CatalogGenreService catalogGenres)
{
    public async Task<OnboardingPreferences> GetAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await preferences.GetOrCreateAsync(account.Id, cancellationToken);
    }

    public async Task<OnboardingPreferences> SaveProgressAsync(
        Guid supabaseUserId,
        SaveOnboardingProgress progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ValidateProgress(progress);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);

        await ValidateGenresAsync(
            progress.MovieGenreIds,
            CatalogContentType.Movie,
            cancellationToken);
        await ValidateGenresAsync(
            progress.SeriesGenreIds,
            CatalogContentType.Series,
            cancellationToken);

        if (!await preferences.CatalogTitlesExistAsync(
                progress.FavoriteMovieIds,
                CatalogContentType.Movie,
                cancellationToken))
        {
            throw new ArgumentException("One or more favorite movie IDs are not in the catalog.");
        }

        if (!await preferences.CatalogTitlesExistAsync(
                progress.FavoriteSeriesIds,
                CatalogContentType.Series,
                cancellationToken))
        {
            throw new ArgumentException("One or more favorite series IDs are not in the catalog.");
        }

        await preferences.SaveProgressAsync(account.Id, progress, cancellationToken);
        return await preferences.GetOrCreateAsync(account.Id, cancellationToken);
    }

    public async Task<OnboardingPreferences> CompleteAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        await preferences.CompleteAsync(account.Id, cancellationToken);
        return await preferences.GetOrCreateAsync(account.Id, cancellationToken);
    }

    private async Task ValidateGenresAsync(
        IReadOnlyCollection<int> selectedGenreIds,
        CatalogContentType contentType,
        CancellationToken cancellationToken)
    {
        if (selectedGenreIds.Count == 0)
        {
            return;
        }

        var validIds = await catalogGenres.GetGenresAsync(
            CatalogProvider.Tmdb,
            contentType,
            "pt-BR",
            cancellationToken);
        if (selectedGenreIds.Any(id => validIds.All(genre => genre.Id != id)))
        {
            throw new ArgumentException($"One or more {contentType} genre IDs are invalid.");
        }
    }

    private static void ValidateProgress(SaveOnboardingProgress progress)
    {
        if (!Enum.IsDefined(progress.CurrentStep) || progress.CurrentStep == OnboardingStep.Completed)
        {
            throw new ArgumentException("A valid in-progress onboarding step is required.", nameof(progress));
        }

        ValidateSelection(progress.MovieGenreIds, 20, "movie genres");
        ValidateSelection(progress.SeriesGenreIds, 20, "series genres");
        ValidateSelection(progress.FavoriteMovieIds, 10, "favorite movies");
        ValidateSelection(progress.FavoriteSeriesIds, 10, "favorite series");
    }

    private static void ValidateSelection<T>(
        IReadOnlyCollection<T> items,
        int maximum,
        string name)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count > maximum || items.Distinct().Count() != items.Count)
        {
            throw new ArgumentException($"Select no more than {maximum} unique {name}.", name);
        }
    }
}