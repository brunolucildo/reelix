import { useQuery } from '@tanstack/react-query';

import { fetchCatalogTitleDetails } from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import { fetchOwnRatings, type OwnRatingEntry } from '@/features/ratings/api/ratings-api';
import { ratingsQueryKeys } from '@/features/ratings/query-keys';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

/** One page of the ratings the viewer has given. */
export function useOwnRatings(page: number) {
  const ratings = useQuery({
    queryKey: ratingsQueryKeys.ownRatings(page),
    queryFn: ({ signal }) => fetchOwnRatings(page, signal),
  });
  useErrorToast(ratings.error, 'Não foi possível carregar suas avaliações. Tente novamente.');
  return ratings;
}

/** Catalog title behind a rating (an episode rating resolves to its series). */
export function useRatedTitle(rating: OwnRatingEntry) {
  const type = rating.targetType === 'episode' ? 'series' : rating.targetType;
  const externalId = rating.targetType === 'episode' ? rating.seriesExternalId : rating.externalId;
  const title = useQuery({
    queryKey: catalogQueryKeys.title(rating.provider, type, externalId),
    queryFn: ({ signal }) => fetchCatalogTitleDetails(rating.provider, type, externalId!, signal),
    enabled: Boolean(externalId),
    staleTime: staleTime.catalog,
  });
  useErrorToast(title.error, 'Não foi possível carregar os detalhes de um título avaliado.');
  return title;
}
