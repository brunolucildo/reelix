import { z } from 'zod';

import { supabase } from '@/services/supabase';

const apiUrl = process.env.EXPO_PUBLIC_API_URL?.trim();

if (!apiUrl) {
  throw new Error('Configure EXPO_PUBLIC_API_URL in mobile/.env before starting the app.');
}

const parsedApiUrl = new URL(apiUrl);
if (
  parsedApiUrl.protocol !== 'https:' &&
  !(parsedApiUrl.protocol === 'http:' && ['localhost', '127.0.0.1', '10.0.2.2'].includes(parsedApiUrl.hostname))
) {
  throw new Error('EXPO_PUBLIC_API_URL must use HTTPS except for a local development server.');
}

const trackingTypeSchema = z.enum(['movie', 'episode']);
const trackingHistoryEntrySchema = z.object({
  id: z.string().uuid(),
  type: trackingTypeSchema,
  targetId: z.string().uuid(),
  seriesId: z.string().uuid().nullable(),
  watchedAt: z.string().datetime({ offset: true }),
  provider: z.enum(['tmdb', 'omdb']).nullable(),
  externalId: z.string().nullable(),
  seriesExternalId: z.string().nullable(),
});
const trackingHistoryPageSchema = z.object({
  items: z.array(trackingHistoryEntrySchema),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
});
const trackedSeriesSchema = z.object({
  seriesId: z.string().uuid(),
  watchedEpisodeCount: z.number().int().nonnegative(),
  provider: z.enum(['tmdb', 'omdb']).nullable().optional(),
  externalId: z.string().nullable().optional(),
  latestEpisodeId: z.string().uuid().nullable().optional(),
  knownSeasonNumbers: z.array(z.number().int().nonnegative()).nullable().optional(),
});
const trackedSeriesPageSchema = trackingHistoryPageSchema.extend({
  items: z.array(trackedSeriesSchema),
});
const seriesProgressSchema = z.object({
  seriesId: z.string().uuid(),
  watchedEpisodeCount: z.number().int().nonnegative(),
  availableEpisodeCount: z.number().int().nonnegative().nullable(),
  progressPercentage: z.number().min(0).max(100).nullable(),
  isComplete: z.boolean().nullable(),
});
const seasonProgressSchema = z.object({
  seriesId: z.string().uuid(),
  seasonId: z.string().uuid(),
  seasonNumber: z.number().int().nonnegative(),
  watchedEpisodeCount: z.number().int().nonnegative(),
  availableEpisodeCount: z.number().int().nonnegative(),
  pendingEpisodeCount: z.number().int().nonnegative(),
  progressPercentage: z.number().min(0).max(100).nullable(),
  isComplete: z.boolean().nullable(),
});
const pendingEpisodeSchema = z.object({
  episodeId: z.string().uuid(),
  episodeNumber: z.number().int().nullable(),
  title: z.string(),
  releaseDate: z.string().nullable(),
});

export type TrackingHistoryEntry = z.infer<typeof trackingHistoryEntrySchema>;
export type TrackingHistoryPage = z.infer<typeof trackingHistoryPageSchema>;
export type TrackedSeriesPage = z.infer<typeof trackedSeriesPageSchema>;
export type SeriesProgress = z.infer<typeof seriesProgressSchema>;
export type SeasonProgress = z.infer<typeof seasonProgressSchema>;
export type PendingEpisode = z.infer<typeof pendingEpisodeSchema>;
export type CatalogProvider = 'tmdb' | 'omdb';

type RequestOptions = {
  method?: 'GET' | 'PUT' | 'DELETE';
  body?: unknown;
  signal?: AbortSignal;
};

async function trackingRequest<T>(
  path: string,
  schema: z.ZodType<T>,
  options: RequestOptions = {},
): Promise<T> {
  const { data, error } = await supabase.auth.getSession();
  if (error) {
    throw error;
  }
  if (!data.session) {
    throw new Error('Sua sessão expirou. Entre novamente para continuar.');
  }

  const response = await fetch(new URL(path, apiUrl), {
    method: options.method ?? 'GET',
    signal: options.signal,
    headers: {
      Accept: 'application/json',
      ...(options.body === undefined ? {} : { 'Content-Type': 'application/json' }),
      Authorization: `Bearer ${data.session.access_token}`,
    },
    ...(options.body === undefined ? {} : { body: JSON.stringify(options.body) }),
  });

  if (response.status === 204) {
    return undefined as T;
  }
  if (!response.ok) {
    let message = response.status === 401
      ? 'Sua sessão expirou. Entre novamente para continuar.'
      : 'Não foi possível atualizar seu acompanhamento. Tente novamente.';
    try {
      const problem = await response.json() as { detail?: string; title?: string };
      message = problem.detail ?? problem.title ?? message;
    } catch {
      // The response can be empty or non-JSON; retain the safe fallback.
    }
    throw Object.assign(new Error(message), { status: response.status });
  }

  const result = schema.safeParse(await response.json());
  if (!result.success) {
    throw new Error('A API retornou dados de acompanhamento em formato inesperado.');
  }
  return result.data;
}

export async function fetchTrackingHistory(
  page = 1,
  pageSize = 20,
  type?: 'movie' | 'episode',
  sort: 'newest' | 'oldest' = 'newest',
  signal?: AbortSignal,
): Promise<TrackingHistoryPage> {
  const query = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
    sort,
  });
  if (type) {
    query.set('type', type);
  }
  return trackingRequest(`/api/v1/tracking/history?${query}`, trackingHistoryPageSchema, { signal });
}

export async function markMovieWatched(
  titleId: string,
  watchedAt?: string,
): Promise<TrackingHistoryEntry> {
  return trackingRequest(
    `/api/v1/tracking/movies/${titleId}`,
    trackingHistoryEntrySchema,
    { method: 'PUT', body: { watchedAt: watchedAt ?? null } },
  );
}

export async function removeWatchedMovie(titleId: string): Promise<void> {
  await trackingRequest(
    `/api/v1/tracking/movies/${titleId}`,
    z.undefined(),
    { method: 'DELETE' },
  );
}

export async function markEpisodeWatched(
  seriesId: string,
  episodeId: string,
  watchedAt?: string,
): Promise<TrackingHistoryEntry> {
  return trackingRequest(
    `/api/v1/tracking/series/${seriesId}/episodes/${episodeId}`,
    trackingHistoryEntrySchema,
    { method: 'PUT', body: { watchedAt: watchedAt ?? null } },
  );
}

export async function removeWatchedEpisode(seriesId: string, episodeId: string): Promise<void> {
  await trackingRequest(
    `/api/v1/tracking/series/${seriesId}/episodes/${episodeId}`,
    z.undefined(),
    { method: 'DELETE' },
  );
}

export async function fetchTrackedSeries(
  page = 1,
  pageSize = 20,
  signal?: AbortSignal,
): Promise<TrackedSeriesPage> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return trackingRequest(
    `/api/v1/tracking/series?${query}`,
    trackedSeriesPageSchema,
    { signal },
  );
}

export async function fetchSeriesProgress(
  seriesId: string,
  signal?: AbortSignal,
): Promise<SeriesProgress> {
  return trackingRequest(
    `/api/v1/tracking/series/${seriesId}/progress`,
    seriesProgressSchema,
    { signal },
  );
}

export async function fetchSeasonProgress(
  seriesId: string,
  seasonNumber: number,
  provider: CatalogProvider = 'tmdb',
  signal?: AbortSignal,
): Promise<SeasonProgress> {
  const query = new URLSearchParams({ provider });
  return trackingRequest(
    `/api/v1/tracking/series/${seriesId}/seasons/${seasonNumber}/progress?${query}`,
    seasonProgressSchema,
    { signal },
  );
}

export async function fetchPendingEpisodes(
  seriesId: string,
  seasonNumber: number,
  provider: CatalogProvider = 'tmdb',
  signal?: AbortSignal,
): Promise<PendingEpisode[]> {
  const query = new URLSearchParams({ provider });
  return trackingRequest(
    `/api/v1/tracking/series/${seriesId}/seasons/${seasonNumber}/episodes/pending?${query}`,
    z.array(pendingEpisodeSchema),
    { signal },
  );
}
