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
            throw new ArgumentException("Um ou mais filmes favoritos não estão no catálogo.");
        }

        if (!await preferences.CatalogTitlesExistAsync(
                progress.FavoriteSeriesIds,
                CatalogContentType.Series,
                cancellationToken))
        {
            throw new ArgumentException("Uma ou mais séries favoritas não estão no catálogo.");
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
            throw new ArgumentException(contentType == CatalogContentType.Movie
                ? "Um ou mais gêneros de filmes são inválidos."
                : "Um ou mais gêneros de séries são inválidos.");
        }
    }

    private static void ValidateProgress(SaveOnboardingProgress progress)
    {
        if (!Enum.IsDefined(progress.CurrentStep) || progress.CurrentStep == OnboardingStep.Completed)
        {
            throw new ArgumentException("Informe uma etapa válida do onboarding em andamento.", nameof(progress));
        }

        ValidateSelection(progress.MovieGenreIds, 20, "gêneros de filmes");
        ValidateSelection(progress.SeriesGenreIds, 20, "gêneros de séries");
        ValidateSelection(progress.FavoriteMovieIds, 10, "filmes favoritos");
        ValidateSelection(progress.FavoriteSeriesIds, 10, "séries favoritas");
    }

    private static void ValidateSelection<T>(
        IReadOnlyCollection<T> items,
        int maximum,
        string name)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count > maximum || items.Distinct().Count() != items.Count)
        {
            throw new ArgumentException($"Selecione no máximo {maximum} {name}, sem repetições.");
        }
    }
}