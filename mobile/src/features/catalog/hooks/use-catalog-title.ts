import { useQuery } from '@tanstack/react-query';

import {
  fetchCatalogTitleDetails,
  type CatalogProvider,
} from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

type ContentType = 'movie' | 'series';

/** Title details for a catalog route; stays disabled until every route param is valid. */
export function useCatalogTitle(
  provider: CatalogProvider | null,
  contentType: ContentType | null,
  externalId: string | null,
) {
  const query = useQuery({
    queryKey: catalogQueryKeys.title(provider, contentType, externalId),
    queryFn: ({ signal }) => {
      if (!provider || !contentType || !externalId) {
        throw new Error('O endereço deste título é inválido.');
      }

      return fetchCatalogTitleDetails(provider, contentType, externalId, signal);
    },
    enabled: provider !== null && contentType !== null && externalId !== null,
    staleTime: staleTime.catalog,
  });
  useErrorToast(query.error, 'Não foi possível carregar os detalhes deste título. Tente novamente.');
  return query;
}
