using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification.Contracts;

/// <summary>HTTP contract of <c>GET /api/v1/gamification/statistics</c> (and the lifetime statistics of the dashboard).</summary>
public sealed record GamificationStatisticsResponse(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int MoviesWatched,
    int EpisodesWatched,
    int SeriesExplored,
    int GenresExplored,
    int MoviesMissingGenreMetadata,
    int ItemsWithRuntime,
    int ItemsWithoutRuntime,
    int? EstimatedWatchMinutes,
    decimal? EstimatedWatchHours,
    IReadOnlyList<string> RuntimeSources,
    int? RuntimeMetadataVersion,
    DateTimeOffset? RuntimeMetadataUpdatedAt,
    int? ConfirmedWatchMinutes,
    DateTimeOffset CalculatedAt)
{
    public double RuntimeCoverage { get; init; }

    public static GamificationStatisticsResponse From(GamificationStatistics statistics) =>
        new(
            statistics.PeriodStart,
            statistics.PeriodEnd,
            statistics.MoviesWatched,
            statistics.EpisodesWatched,
            statistics.SeriesExplored,
            statistics.GenresExplored,
            statistics.MoviesMissingGenreMetadata,
            statistics.ItemsWithRuntime,
            statistics.ItemsWithoutRuntime,
            statistics.EstimatedWatchMinutes,
            statistics.EstimatedWatchHours,
            statistics.RuntimeSources,
            statistics.RuntimeMetadataVersion,
            statistics.RuntimeMetadataUpdatedAt,
            statistics.ConfirmedWatchMinutes,
            statistics.CalculatedAt)
        {
            RuntimeCoverage = statistics.RuntimeCoverage,
        };
}
