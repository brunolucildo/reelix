import { useQuery } from '@tanstack/react-query';

import {
  fetchCustomList,
  fetchCustomLists,
  fetchFavorites,
  fetchWatchlist,
} from '@/features/lists/api/lists-api';
import { listsQueryKeys } from '@/features/lists/query-keys';
import { useErrorToast } from '@/providers/toast-provider';

const CONNECTION_HINT = 'Verifique a conexão e tente novamente.';

export function useFavorites(page: number, enabled: boolean) {
  const query = useQuery({
    queryKey: listsQueryKeys.favorites(page),
    queryFn: () => fetchFavorites(page),
    enabled,
  });
  useErrorToast(query.error, `Não foi possível carregar seus favoritos. ${CONNECTION_HINT}`);
  return query;
}

export function useWatchlist(page: number, oldestFirst: boolean, enabled: boolean) {
  const query = useQuery({
    queryKey: listsQueryKeys.watchlist(page, oldestFirst),
    queryFn: () => fetchWatchlist(page, oldestFirst),
    enabled,
  });
  useErrorToast(query.error, `Não foi possível carregar sua watchlist. ${CONNECTION_HINT}`);
  return query;
}

export function useCustomLists(page: number, enabled: boolean) {
  const query = useQuery({
    queryKey: listsQueryKeys.customLists(page),
    queryFn: () => fetchCustomLists(page),
    enabled,
  });
  useErrorToast(query.error, `Não foi possível carregar suas listas. ${CONNECTION_HINT}`);
  return query;
}

export function useCustomList(listId: string | null, page: number, enabled: boolean) {
  const query = useQuery({
    queryKey: listsQueryKeys.customList(listId, page),
    queryFn: () => fetchCustomList(listId!, page),
    enabled: enabled && listId !== null,
  });
  useErrorToast(query.error, `Não foi possível carregar esta lista. ${CONNECTION_HINT}`);
  return query;
}
