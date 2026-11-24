import { useQuery } from '@tanstack/react-query';

import { searchCatalog } from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

export function useCatalogSearch(type: 'movie' | 'series', query: string) {
  const results = useQuery({
    queryKey: catalogQueryKeys.search(type, query),
    queryFn: ({ signal }) => searchCatalog(query, type, signal),
    enabled: query.length > 0,
    staleTime: staleTime.short,
  });
  useErrorToast(results.error, 'Não foi possível pesquisar no catálogo. Tente novamente.');
  return results;
}
