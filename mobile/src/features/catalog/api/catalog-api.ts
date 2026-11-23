import { z } from 'zod';

import { httpRequest } from '@/lib/http-client';

const genreSchema = z.object({
  id: z.number().int().positive(),
  name: z.string().min(1),
});

export const catalogTitleSchema = z.object({
  id: z.string().uuid(),
  provider: z.enum(['tmdb', 'omdb']),
  externalId: z.string().min(1),
  contentType: z.enum(['movie', 'series']),
  title: z.string(),
  originalTitle: z.string().nullable(),
  overview: z.string().nullable(),
  genres: z.array(z.string()),
  releaseDate: z.string().nullable(),
  originalLanguage: z.string().nullable(),
  runtimeMinutes: z.number().int().positive().nullable(),
  posterUrl: z.string().url().nullable(),
  backdropUrl: z.string().url().nullable(),
});

const catalogSearchSchema = z.object({
  items: z.array(catalogTitleSchema),
});

const catalogDiscoverySchema = z.object({
  items: z.array(catalogTitleSchema),
  page: z.number().int().positive(),
  totalPages: z.number().int().positive(),
  totalResults: z.number().int().nonnegative(),
  provider: z.enum(['tmdb', 'omdb']),
  contentType: z.enum(['movie', 'series']),
  category: z.enum(['popular', 'upcoming']),
});

const catalogSeasonSchema = z.object({
  id: z.string().uuid(),
  seriesId: z.string().uuid(),
  provider: z.enum(['tmdb', 'omdb']),
  externalSeriesId: z.string(),
  title: z.string().nullable(),
  seasonNumber: z.number().int().nonnegative(),
  releaseDate: z.string().nullable(),
  episodes: z.array(z.object({
    id: z.string().uuid(),
    externalId: z.string(),
    episodeNumber: z.number().int().nullable(),
    stillUrl: z.string().url().nullable().optional(),
    title: z.string(),
    overview: z.string().nullable(),
    releaseDate: z.string().nullable(),
    runtimeMinutes: z.number().int().positive().nullable(),
  })),
});

const catalogSeasonSummarySchema = z.object({
  seasonNumber: z.number().int().nonnegative(),
  title: z.string().nullable(),
  releaseDate: z.string().nullable(),
});

export type CatalogProvider = z.infer<typeof catalogTitleSchema>['provider'];

export function isCatalogProvider(value: unknown): value is CatalogProvider {
  return value === 'tmdb' || value === 'omdb';
}

export type CatalogGenre = z.infer<typeof genreSchema>;

export type CatalogTitle = z.infer<typeof catalogTitleSchema>;

export type CatalogDiscoveryPage = z.infer<typeof catalogDiscoverySchema>;

export type CatalogDiscoveryCategory = CatalogDiscoveryPage['category'];

export type CatalogSeason = z.infer<typeof catalogSeasonSchema>;

export type CatalogSeasonSummary = z.infer<typeof catalogSeasonSummarySchema>;

export async function fetchCatalogGenres(type: 'movie' | 'series'): Promise<CatalogGenre[]> {
  return httpRequest(
    `/api/v1/catalog/genres?type=${type}&provider=tmdb&language=pt-BR`,
    z.array(genreSchema),
  );
}

export async function searchCatalog(
  query: string,
  type: 'movie' | 'series',
  signal?: AbortSignal,
): Promise<CatalogTitle[]> {
  const search = new URLSearchParams({ query, type, provider: 'tmdb', language: 'pt-BR' });
  const result = await httpRequest(`/api/v1/catalog/search?${search}`, catalogSearchSchema, { signal });
  return result.items;
}

export async function fetchCatalogDiscovery(
  type: 'movie' | 'series',
  category: CatalogDiscoveryCategory,
  genreIds: number[],
  page: number,
  signal?: AbortSignal,
): Promise<CatalogDiscoveryPage> {
  const search = new URLSearchParams({
    type,
    section: category,
    page: String(page),
    provider: 'tmdb',
    language: 'pt-BR',
  });
  for (const genreId of genreIds) {
    search.append('genreIds', String(genreId));
  }
  return httpRequest(`/api/v1/catalog/discover?${search}`, catalogDiscoverySchema, { signal });
}

export async function fetchCatalogTitleDetails(
  provider: CatalogTitle['provider'],
  type: CatalogTitle['contentType'],
  externalId: string,
  signal?: AbortSignal,
): Promise<CatalogTitle> {
  return httpRequest(
    `/api/v1/catalog/${provider}/${type}/${encodeURIComponent(externalId)}?language=pt-BR`,
    catalogTitleSchema,
    { signal },
  );
}

export async function fetchCatalogSeason(
  provider: CatalogTitle['provider'],
  externalSeriesId: string,
  seasonNumber: number,
  signal?: AbortSignal,
): Promise<CatalogSeason> {
  return httpRequest(
    `/api/v1/catalog/${provider}/series/${encodeURIComponent(externalSeriesId)}/seasons/${seasonNumber}?language=pt-BR`,
    catalogSeasonSchema,
    { signal },
  );
}

export async function fetchCatalogSeasons(
  provider: CatalogTitle['provider'],
  externalSeriesId: string,
  signal?: AbortSignal,
): Promise<CatalogSeasonSummary[]> {
  return httpRequest(
    `/api/v1/catalog/${provider}/series/${encodeURIComponent(externalSeriesId)}/seasons?language=pt-BR`,
    z.array(catalogSeasonSummarySchema),
    { signal },
  );
}
