import { useInfiniteQuery } from '@tanstack/react-query';

import {
  fetchCatalogDiscovery,
  type CatalogDiscoveryCategory,
} from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import { getNextPageByTotal } from '@/lib/pagination';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

/** Infinite discovery rail (popular/upcoming, optionally by genre) with duplicate titles removed. */
export function useDiscoveryFeed(
  title: string,
  type: 'movie' | 'series',
  category: CatalogDiscoveryCategory,
  genreIds: number[],
) {
  const normalizedGenreIds = [...genreIds].sort((a, b) => a - b);
  const feed = useInfiniteQuery({
    queryKey: catalogQueryKeys.discovery(type, category, normalizedGenreIds),
    queryFn: ({ pageParam, signal }) =>
      fetchCatalogDiscovery(type, category, normalizedGenreIds, pageParam, signal),
    initialPageParam: 1,
    getNextPageParam: getNextPageByTotal,
    staleTime: staleTime.catalog,
  });
  useErrorToast(feed.error, `Não foi possível carregar "${title}". Tente novamente.`);

  const seenIds = new Set<string>();
  const items = (feed.data?.pages.flatMap((page) => page.items) ?? []).filter((item) => {
    if (seenIds.has(item.id)) {
      return false;
    }

    seenIds.add(item.id);
    return true;
  });

  return { feed, items };
}
