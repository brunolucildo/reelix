import { useQueries, useQuery } from '@tanstack/react-query';

import { fetchCatalogSeason, fetchCatalogSeasons } from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import {
  fetchFavorites,
  fetchTitleDetails,
  fetchWatchlist,
  type TitleReference,
} from '@/features/lists/api/lists-api';
import { listsQueryKeys } from '@/features/lists/query-keys';
import { useTrackedSeries } from '@/features/tracking/hooks/use-tracking';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

/**
 * Series saved as favorites or in the watchlist that the viewer has not started tracking yet
 * (up to 8), with the first episode of each as a suggestion.
 */
export function useStartWatching() {
  const favorites = useQuery({
    queryKey: listsQueryKeys.startWatchingFavorites,
    queryFn: () => fetchFavorites(1, 100),
    staleTime: staleTime.short,
  });
  const watchlist = useQuery({
    queryKey: listsQueryKeys.startWatchingWatchlist,
    queryFn: () => fetchWatchlist(1, false),
    staleTime: staleTime.short,
  });
  const tracked = useTrackedSeries(1, 100);
  const trackedKeys = new Set((tracked.data?.items ?? [])
    .filter((series) => series.provider && series.externalId)
    .map((series) => `${series.provider}:${series.externalId}`));
  const references = mergeSeriesReferences([
    ...(favorites.data?.items ?? []),
    ...(watchlist.data?.items ?? []),
  ]).filter((reference) => !trackedKeys.has(`${reference.provider}:${reference.externalId}`))
    .slice(0, 8);
  const details = useQueries({
    queries: references.map((reference) => ({
      queryKey: catalogQueryKeys.title(reference.provider, 'series', reference.externalId),
      queryFn: ({ signal }: { signal: AbortSignal }) => fetchTitleDetails(reference, signal),
      staleTime: staleTime.catalog,
    })),
  });
  const items = details.flatMap((result) => result.data ? [result.data] : []);
  const episodeSuggestions = useQueries({
    queries: items.map((item) => ({
      queryKey: catalogQueryKeys.firstEpisode(item.provider, item.externalId),
      queryFn: async ({ signal }: { signal: AbortSignal }) => {
        const seasons = await fetchCatalogSeasons(item.provider, item.externalId, signal);
        const firstSeason = seasons.find((season) => season.seasonNumber > 0) ?? seasons[0];
        if (!firstSeason) return null;

        const season = await fetchCatalogSeason(
          item.provider,
          item.externalId,
          firstSeason.seasonNumber,
          signal,
        );
        const firstEpisode = [...season.episodes]
          .filter((episode) => episode.episodeNumber !== null)
          .sort((left, right) => (left.episodeNumber ?? 0) - (right.episodeNumber ?? 0))[0];
        return firstEpisode
          ? { seasonNumber: season.seasonNumber, episode: firstEpisode }
          : null;
      },
      staleTime: staleTime.catalog,
    })),
  });

  useErrorToast(favorites.error, 'Não foi possível carregar os favoritos para começar a assistir.');
  useErrorToast(watchlist.error, 'Não foi possível carregar a watchlist para começar a assistir.');
  useErrorToast(tracked.error, 'Não foi possível verificar as séries já acompanhadas.');
  const detailsError = details.find((result) => result.error)?.error;
  useErrorToast(detailsError, 'Não foi possível carregar alguns títulos para começar a assistir.');
  const suggestionError = episodeSuggestions.find((result) => result.error)?.error;
  useErrorToast(suggestionError, 'Não foi possível carregar a sugestão de primeiro episódio.');

  return {
    favorites,
    watchlist,
    tracked,
    references,
    details,
    items,
    episodeSuggestions,
    detailsError,
    suggestionError,
    isPending: favorites.isPending || watchlist.isPending || tracked.isPending,
    hasError: favorites.isError || watchlist.isError || tracked.isError,
    isLoadingDetails: details.some((result) => result.isPending),
  };
}

function mergeSeriesReferences(references: TitleReference[]) {
  const unique = new Map<string, TitleReference>();
  for (const reference of references) {
    if (reference.contentType !== 'series') continue;
    unique.set(`${reference.provider}:${reference.externalId}`, reference);
  }
  return Array.from(unique.values());
}
