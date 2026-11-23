import type { CatalogDiscoveryCategory, CatalogProvider } from '@/features/catalog/api/catalog-api';

type ContentType = 'movie' | 'series';
type Maybe<T> = T | null | undefined;

/** One key per catalog resource: every screen that shows the same title shares the same cache entry. */
export const catalogQueryKeys = {
  all: ['catalog'] as const,
  title: (provider: Maybe<CatalogProvider>, contentType: Maybe<ContentType>, externalId: Maybe<string>) =>
    ['catalog', 'title', provider, contentType, externalId] as const,
  genres: (type: ContentType) => ['catalog', 'genres', type] as const,
  search: (type: ContentType, query: string) => ['catalog', 'search', type, query] as const,
  trackingSeriesSearch: (query: string) => ['catalog', 'tracking-series-search', query] as const,
  trackingMovieSearch: (query: string) => ['catalog', 'tracking-movie-search', query] as const,
  discovery: (type: ContentType, category: CatalogDiscoveryCategory, genreIds: number[]) =>
    ['catalog', 'discovery', type, category, genreIds] as const,
  firstEpisode: (provider: CatalogProvider, externalId: string) =>
    ['catalog', 'first-episode', provider, externalId] as const,
  seasons: (provider: Maybe<CatalogProvider>, externalSeriesId: Maybe<string>) =>
    ['catalog', 'seasons', provider, externalSeriesId] as const,
  season: (
    provider: Maybe<CatalogProvider>,
    externalSeriesId: Maybe<string>,
    seasonNumber: Maybe<number>,
  ) => ['catalog', 'season', provider, externalSeriesId, seasonNumber] as const,
};
