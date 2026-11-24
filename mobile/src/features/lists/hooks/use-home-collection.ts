import { useQueries, useQuery } from '@tanstack/react-query';

import { catalogQueryKeys } from '@/features/catalog/query-keys';
import {
  fetchFavorites,
  fetchTitleDetails,
  fetchWatchlist,
} from '@/features/lists/api/lists-api';
import { listsQueryKeys } from '@/features/lists/query-keys';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

/**
 * Up to 8 favorites (or "Quero assistir" watchlist entries) of one content type for the Home,
 * with the catalog details of each. The watchlist is read page by page until 8 match the type.
 */
export function useHomeCollection(collection: 'favorites' | 'watchlist', type: 'movie' | 'series') {
  const isWatchlist = collection === 'watchlist';
  const favorites = useQuery({
    queryKey: listsQueryKeys.home(collection, type),
    queryFn: async () => {
      if (!isWatchlist) return fetchFavorites(1, 100);
      const firstPage = await fetchWatchlist(1, false);
      const items = [...firstPage.items];
      let page = 1;
      while (items.filter((item) => item.contentType === type).length < 8 && page * firstPage.pageSize < firstPage.totalCount) {
        page += 1;
        const nextPage = await fetchWatchlist(page, false);
        items.push(...nextPage.items);
      }
      return { ...firstPage, items };
    },
    staleTime: staleTime.short,
  });
  const references = (favorites.data?.items ?? [])
    .filter((reference) => reference.contentType === type)
    .slice(0, 8);
  const titleDetails = useQueries({
    queries: references.map((reference) => ({
      queryKey: catalogQueryKeys.title(reference.provider, reference.contentType, reference.externalId),
      queryFn: ({ signal }: { signal: AbortSignal }) => fetchTitleDetails(reference, signal),
      staleTime: staleTime.catalog,
    })),
  });
  const detailError = titleDetails.find((result) => result.error)?.error ?? null;

  useErrorToast(favorites.error, isWatchlist ? 'Não foi possível carregar Quero assistir. Tente novamente.' : 'Não foi possível carregar seus favoritos. Tente novamente.');
  useErrorToast(detailError, isWatchlist ? 'Não foi possível carregar alguns títulos de Quero assistir. Tente novamente.' : 'Não foi possível carregar alguns títulos favoritos. Tente novamente.');

  return {
    favorites,
    references,
    titleDetails,
    detailError,
    items: titleDetails.flatMap((result) => result.data ? [result.data] : []),
    isLoadingDetails: titleDetails.some((result) => result.isPending),
  };
}
