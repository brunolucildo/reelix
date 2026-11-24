import { useQuery } from '@tanstack/react-query';

import { fetchCatalogSeason, fetchCatalogTitleDetails } from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import type { TrackedSeriesPage } from '@/features/tracking/api/tracking-api';
import { useSeriesProgress } from '@/features/tracking/hooks/use-tracking';
import { trackingQueryKeys } from '@/features/tracking/query-keys';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

/** Title, latest watched season/episode and progress of one tracked series ("Continuar Assistindo"). */
export function useContinueWatchingCard(item: TrackedSeriesPage['items'][number]) {
  const title = useQuery({
    queryKey: catalogQueryKeys.title(item.provider, 'series', item.externalId),
    queryFn: ({ signal }) => fetchCatalogTitleDetails(item.provider!, 'series', item.externalId!, signal),
    enabled: Boolean(item.provider && item.externalId),
    staleTime: staleTime.catalog,
  });
  const season = useQuery({
    queryKey: trackingQueryKeys.homeSeason(item.seriesId, item.latestEpisodeId, item.knownSeasonNumbers),
    queryFn: async ({ signal }) => {
      for (const number of item.knownSeasonNumbers ?? []) {
        const details = await fetchCatalogSeason(item.provider!, item.externalId!, number, signal);
        const episode = details.episodes.find((candidate) => candidate.id === item.latestEpisodeId);
        if (episode) {
          return { number, episode };
        }
      }
      return null;
    },
    enabled: Boolean(item.provider && item.externalId && item.latestEpisodeId),
  });
  const progress = useSeriesProgress(item.seriesId, item.provider ?? 'tmdb');
  useErrorToast(title.error, 'Não foi possível carregar o pôster da série. Tente novamente.');
  useErrorToast(season.error, 'Não foi possível identificar a temporada. Tente novamente.');

  const retry = () => {
    if (title.isError) void title.refetch();
    if (season.isError) void season.refetch();
    if (progress.isError) void progress.refetch();
  };

  return { title, season, progress, retry };
}
