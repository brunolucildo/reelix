using Reelix.Application.Catalog;

namespace Reelix.Application.Users;

public enum OnboardingStep
{
    Welcome,
    Genres,
    FavoriteMovies,
    FavoriteSeries,
    Review,
    Completed,
}

public sealed record OnboardingPreferences(
    bool IsComplete,
    OnboardingStep CurrentStep,
    IReadOnlyList<int> MovieGenreIds,
    IReadOnlyList<int> SeriesGenreIds,
    IReadOnlyList<Guid> FavoriteMovieIds,
    IReadOnlyList<Guid> FavoriteSeriesIds);

public sealed record SaveOnboardingProgress(
    OnboardingStep CurrentStep,
    IReadOnlyList<int> MovieGenreIds,
    IReadOnlyList<int> SeriesGenreIds,
    IReadOnlyList<Guid> FavoriteMovieIds,
    IReadOnlyList<Guid> FavoriteSeriesIds);

public interface IUserPreferencesRepository
{
    Task<OnboardingPreferences?> GetExistingAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<OnboardingPreferences> GetOrCreateAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task SaveProgressAsync(
        Guid accountId,
        SaveOnboardingProgress progress,
        CancellationToken cancellationToken);

    Task CompleteAsync(Guid accountId, CancellationToken cancellationToken);

    Task<bool> CatalogTitlesExistAsync(
        IReadOnlyCollection<Guid> titleIds,
        CatalogContentType contentType,
        CancellationToken cancellationToken);
}