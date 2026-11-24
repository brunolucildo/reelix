import { useQuery } from '@tanstack/react-query';

import { searchCatalog } from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import { useErrorToast } from '@/providers/toast-provider';

/** Movie search used to register a watched movie by hand. */
export function useTrackingMovieSearch(query: string) {
  const results = useQuery({
    queryKey: catalogQueryKeys.trackingMovieSearch(query),
    queryFn: () => searchCatalog(query, 'movie'),
    enabled: query.length > 0,
  });
  useErrorToast(results.error, 'Não foi possível buscar filmes. Tente novamente.');
  return results;
}

/** Series search used to register watched episodes by hand. */
export function useTrackingSeriesSearch(query: string) {
  const results = useQuery({
    queryKey: catalogQueryKeys.trackingSeriesSearch(query),
    queryFn: () => searchCatalog(query, 'series'),
    enabled: query.length > 0,
  });
  useErrorToast(results.error, 'Não foi possível buscar séries. Tente novamente.');
  return results;
}
