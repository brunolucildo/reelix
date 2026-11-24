import { useQuery } from '@tanstack/react-query';

import {
  fetchTitleStatuses,
  searchCatalogTitles,
} from '@/features/lists/api/lists-api';
import { listsQueryKeys } from '@/features/lists/query-keys';
import { useErrorToast } from '@/providers/toast-provider';

/** Catalog search (2+ characters) plus the favorite/watchlist status of each result. */
export function useListsCatalogSearch(text: string) {
  const query = text.trim();
  const search = useQuery({
    queryKey: listsQueryKeys.catalogSearch(query),
    queryFn: () => searchCatalogTitles(query),
    enabled: query.length >= 2,
  });
  const titleIds = search.data?.map((title) => title.id) ?? [];
  const statuses = useQuery({
    queryKey: listsQueryKeys.titleStatus(titleIds),
    queryFn: () => fetchTitleStatuses(titleIds),
    enabled: titleIds.length > 0,
  });
  useErrorToast(search.error, 'Não foi possível buscar títulos. Verifique a conexão e tente novamente.');
  useErrorToast(statuses.error, 'Não foi possível verificar seus títulos salvos. Tente novamente.');

  const statusByTitleId = new Map((statuses.data ?? []).map((status) => [status.titleId, status]));
  return { search, statuses, statusByTitleId };
}
