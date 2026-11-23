import type { CatalogProvider } from '@/features/catalog/api/catalog-api';

export const trackingQueryKeys = {
  all: ['tracking'] as const,
  history: (page: number, pageSize: number, type?: 'movie' | 'episode') =>
    ['tracking', 'history', page, pageSize, type] as const,
  movieStatus: (titleId: string) => ['tracking', 'movie-status', titleId] as const,
  series: (page: number, pageSize: number) =>
    ['tracking', 'series', page, pageSize] as const,
  seriesProgress: (seriesId: string, provider: CatalogProvider = 'tmdb') =>
    ['tracking', 'series-progress', seriesId, provider] as const,
  seasonProgress: (seriesId: string, seasonNumber: number, provider: CatalogProvider) =>
    ['tracking', 'season-progress', seriesId, seasonNumber, provider] as const,
  pendingEpisodes: (seriesId: string, seasonNumber: number, provider: CatalogProvider) =>
    ['tracking', 'pending-episodes', seriesId, seasonNumber, provider] as const,
  homeSeason: (
    seriesId: string,
    latestEpisodeId: string | null | undefined,
    knownSeasonNumbers: number[] | null | undefined,
  ) => ['tracking', 'home-season', seriesId, latestEpisodeId, knownSeasonNumbers] as const,
  // Home calendar: kept outside the `tracking` root so tracking mutations keep not refetching it.
  calendarSeasons: (seriesId: string) => ['calendar', 'seasons', seriesId] as const,
  calendarSeason: (seriesId: string, seasonNumber: number) =>
    ['calendar', 'season', seriesId, seasonNumber] as const,
};
