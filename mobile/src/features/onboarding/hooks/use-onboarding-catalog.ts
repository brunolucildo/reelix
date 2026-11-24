import { useQuery } from '@tanstack/react-query';

import { fetchCatalogGenres, searchCatalog } from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import { useErrorToast } from '@/providers/toast-provider';

/** Genres of one content type (cached by the catalog module). */
export function useOnboardingGenres(type: 'movie' | 'series') {
  return useQuery({
    queryKey: catalogQueryKeys.genres(type),
    queryFn: () => fetchCatalogGenres(type),
  });
}

/** Catalog search for the favorites steps; runs only for 2+ characters while `enabled`. */
export function useOnboardingTitleSearch(type: 'movie' | 'series', query: string, enabled: boolean) {
  const search = useQuery({
    queryKey: catalogQueryKeys.search(type, query),
    queryFn: () => searchCatalog(query, type),
    enabled: enabled && query.trim().length >= 2,
  });
  useErrorToast(search.error, search.error?.message ?? 'Não foi possível buscar no catálogo.');
  return search;
}
