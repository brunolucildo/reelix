import {
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';

import {
  fetchPendingEpisodes,
  fetchMovieTrackingStatus,
  fetchSeasonProgress,
  fetchSeriesProgress,
  fetchTrackedSeries,
  fetchTrackingHistory,
  markEpisodeWatched,
  markMovieWatched,
  removeWatchedEpisode,
  removeWatchedMovie,
  type CatalogProvider,
} from '@/services/tracking-api';
import { useErrorToast, useToast } from '@/providers/toast-provider';

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
};

function queryErrorMessage(error: unknown, fallback: string) {
  return error instanceof Error && 'status' in error && error.status === 401
    ? 'Sua sessão expirou. Entre novamente para continuar.'
    : fallback;
}

export function useTrackingHistory(
  page = 1,
  pageSize = 20,
  type?: 'movie' | 'episode',
) {
  const query = useQuery({
    queryKey: trackingQueryKeys.history(page, pageSize, type),
    queryFn: ({ signal }) => fetchTrackingHistory(page, pageSize, type, 'newest', signal),
  });
  useErrorToast(query.error, queryErrorMessage(
    query.error,
    'Não foi possível carregar seu histórico. Tente novamente.',
  ));
  return query;
}

export function useMovieTrackingStatus(titleId: string) {
  const query = useQuery({
    queryKey: trackingQueryKeys.movieStatus(titleId),
    queryFn: ({ signal }) => fetchMovieTrackingStatus(titleId, signal),
    enabled: Boolean(titleId),
  });
  useErrorToast(query.error, queryErrorMessage(
    query.error,
    'Não foi possível verificar o acompanhamento deste filme.',
  ));
  return query;
}

export function useTrackedSeries(page = 1, pageSize = 20) {
  const query = useQuery({
    queryKey: trackingQueryKeys.series(page, pageSize),
    queryFn: ({ signal }) => fetchTrackedSeries(page, pageSize, signal),
  });
  useErrorToast(query.error, queryErrorMessage(
    query.error,
    'Não foi possível carregar suas séries acompanhadas. Tente novamente.',
  ));
  return query;
}

export function useSeriesProgress(seriesId: string, provider: CatalogProvider = 'tmdb') {
  const query = useQuery({
    queryKey: trackingQueryKeys.seriesProgress(seriesId, provider),
    queryFn: ({ signal }) => fetchSeriesProgress(seriesId, signal, provider),
    enabled: Boolean(seriesId),
  });
  useErrorToast(query.error, 'Não foi possível carregar o progresso da série. Tente novamente.');
  return query;
}

export function useSeasonProgress(
  seriesId: string,
  seasonNumber: number,
  provider: CatalogProvider = 'tmdb',
) {
  const query = useQuery({
    queryKey: trackingQueryKeys.seasonProgress(seriesId, seasonNumber, provider),
    queryFn: ({ signal }) => fetchSeasonProgress(seriesId, seasonNumber, provider, signal),
    enabled: Boolean(seriesId) && seasonNumber >= 0,
  });
  useErrorToast(query.error, 'Não foi possível carregar o progresso da temporada. Tente novamente.');
  return query;
}

export function usePendingEpisodes(
  seriesId: string,
  seasonNumber: number,
  provider: CatalogProvider = 'tmdb',
) {
  const query = useQuery({
    queryKey: trackingQueryKeys.pendingEpisodes(seriesId, seasonNumber, provider),
    queryFn: ({ signal }) => fetchPendingEpisodes(seriesId, seasonNumber, provider, signal),
    enabled: Boolean(seriesId) && seasonNumber >= 0,
  });
  useErrorToast(query.error, queryErrorMessage(
    query.error,
    'Não foi possível carregar os episódios pendentes. Tente novamente.',
  ));
  return query;
}

function useTrackingMutation<TVariables, TData>(
  mutationFn: (variables: TVariables) => Promise<TData>,
) {
  const queryClient = useQueryClient();
  const { showError } = useToast();

  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: trackingQueryKeys.all }),
        queryClient.invalidateQueries({ queryKey: ['gamification'] }),
      ]);
    },
    onError: (error) => showError(queryErrorMessage(
      error,
      'Não foi possível salvar seu acompanhamento. Tente novamente.',
    )),
  });
}

export function useMarkMovieWatched() {
  return useTrackingMutation<{ titleId: string; watchedAt?: string }, Awaited<ReturnType<typeof markMovieWatched>>>(
    ({ titleId, watchedAt }) => markMovieWatched(titleId, watchedAt),
  );
}

export function useRemoveWatchedMovie() {
  return useTrackingMutation<string, void>(removeWatchedMovie);
}

export function useMarkEpisodeWatched() {
  return useTrackingMutation<
    { seriesId: string; episodeId: string; watchedAt?: string },
    Awaited<ReturnType<typeof markEpisodeWatched>>
  >(
    ({ seriesId, episodeId, watchedAt }) => markEpisodeWatched(seriesId, episodeId, watchedAt),
  );
}

export function useRemoveWatchedEpisode() {
  return useTrackingMutation<
    { seriesId: string; episodeId: string },
    void
  >(({ seriesId, episodeId }) => removeWatchedEpisode(seriesId, episodeId));
}
