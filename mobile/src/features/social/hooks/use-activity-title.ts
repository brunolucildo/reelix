import { useQuery } from '@tanstack/react-query';

import { fetchCatalogTitleDetails } from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import type { SocialActivityData } from '@/features/social/api/social-api';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

/** Catalog title of a friend's activity; skipped for episodes and in the compact list. */
export function useActivityTitle(activity: SocialActivityData, compact: boolean) {
  const contentType = activity.targetType === 'series' ? 'series' : 'movie';
  const title = useQuery({
    queryKey: catalogQueryKeys.title(activity.provider, contentType, activity.externalId),
    queryFn: ({ signal }) => fetchCatalogTitleDetails(activity.provider, contentType, activity.externalId, signal),
    enabled: !compact && activity.targetType !== 'episode',
    staleTime: staleTime.catalog,
  });
  useErrorToast(title.error, 'Não foi possível carregar os detalhes do título.');
  return title;
}
