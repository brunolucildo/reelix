import { useQuery } from '@tanstack/react-query';

import { fetchCatalogTitleDetails } from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import type { FriendProfileData } from '@/features/social/api/social-api';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

/** Catalog title of a favorite a friend shares. */
export function useFavoriteTitle(favorite: FriendProfileData['favorites'][number]) {
  const title = useQuery({
    queryKey: catalogQueryKeys.title(favorite.provider, favorite.contentType, favorite.externalId),
    queryFn: ({ signal }) => fetchCatalogTitleDetails(favorite.provider, favorite.contentType, favorite.externalId, signal),
    staleTime: staleTime.catalog,
  });
  useErrorToast(title.error, 'Não foi possível carregar um favorito compartilhado.');
  return title;
}
