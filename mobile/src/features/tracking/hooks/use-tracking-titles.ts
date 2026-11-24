import { useQueries } from '@tanstack/react-query';

import { fetchCatalogTitleDetails } from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import type {
  TrackedSeriesPage,
  TrackingHistoryPage,
} from '@/features/tracking/api/tracking-api';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

/** Catalog details for each history entry (same order), sharing the title cache with the catalog. */
export function useHistoryTitles(items: TrackingHistoryPage['items']) {
  const results = useQueries({
    queries: items.map((entry) => {
      const contentType = entry.type === 'movie' ? 'movie' as const : 'series' as const;
      const externalId = entry.type === 'movie' ? entry.externalId : entry.seriesExternalId;
      return {
        queryKey: catalogQueryKeys.title(entry.provider, contentType, externalId),
        queryFn: ({ signal }: { signal: AbortSignal }) => {
          if (!entry.provider || !externalId) {
            throw new Error('Os detalhes deste conteúdo não estão disponíveis.');
          }
          return fetchCatalogTitleDetails(entry.provider, contentType, externalId, signal);
        },
        enabled: Boolean(entry.provider && externalId),
        staleTime: staleTime.catalog,
      };
    }),
  });
  useErrorToast(
    results.find((result) => result.error)?.error,
    'Não foi possível carregar alguns títulos do histórico.',
  );
  return results;
}

/** Catalog details for each tracked series (same order). */
export function useTrackedSeriesTitles(items: TrackedSeriesPage['items']) {
  const results = useQueries({
    queries: items.map((item) => ({
      queryKey: catalogQueryKeys.title(item.provider, 'series', item.externalId),
      queryFn: ({ signal }: { signal: AbortSignal }) => {
        if (!item.provider || !item.externalId) {
          throw new Error('Os detalhes desta série não estão disponíveis.');
        }
        return fetchCatalogTitleDetails(item.provider, 'series', item.externalId, signal);
      },
      enabled: Boolean(item.provider && item.externalId),
      staleTime: staleTime.catalog,
    })),
  });
  useErrorToast(
    results.find((result) => result.error)?.error,
    'Não foi possível carregar alguns títulos acompanhados.',
  );
  return results;
}
