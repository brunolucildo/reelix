import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import type { CatalogTitle } from '@/features/catalog/api/catalog-api';
import {
  createCustomList,
  fetchCustomLists,
  fetchTitleStatuses,
  removeFavorite,
  removeWatchlistTitle,
  setCustomListTitle,
  setFavorite,
  setWatchlistTitle,
} from '@/features/lists/api/lists-api';
import { listsQueryKeys } from '@/features/lists/query-keys';
import { socialQueryKeys } from '@/features/social/query-keys';
import { getNextPageByCount } from '@/lib/pagination';
import { useErrorToast } from '@/providers/toast-provider';

/**
 * Favorite, watchlist and private-list actions of one catalog title. `pickerVisible` loads the
 * viewer's lists only while the "add to list" dialog is open.
 */
export function useTitleCollectionActions(title: CatalogTitle, pickerVisible: boolean) {
  const queryClient = useQueryClient();
  const [feedback, setFeedback] = useState<string | null>(null);
  const [addedLists, setAddedLists] = useState<string[]>([]);
  const status = useQuery({
    queryKey: listsQueryKeys.titleStatus([title.id]),
    queryFn: () => fetchTitleStatuses([title.id]),
  });
  const lists = useInfiniteQuery({
    queryKey: listsQueryKeys.titlePicker,
    queryFn: ({ pageParam }) => fetchCustomLists(pageParam),
    initialPageParam: 1,
    getNextPageParam: getNextPageByCount,
    enabled: pickerVisible,
  });
  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: listsQueryKeys.all }),
      queryClient.invalidateQueries({ queryKey: socialQueryKeys.friendProfileAll }),
    ]);
  };
  const relation = status.data?.find((item) => item.titleId === title.id);
  const update = useMutation({
    mutationFn: async (target: 'favorite' | 'watchlist') => {
      if (!relation) throw new Error('Atualize o estado do título antes de continuar.');
      if (target === 'favorite') return relation.isFavorite ? removeFavorite(title.id) : setFavorite(title);
      return relation.isInWatchlist ? removeWatchlistTitle(title.id) : setWatchlistTitle(title);
    },
    onSuccess: refresh,
  });
  const addToList = useMutation({
    mutationFn: async (listId: string) => ({ listId, result: await setCustomListTitle(listId, title) }),
    onSuccess: async ({ listId, result }) => {
      setAddedLists((current) => [...new Set([...current, listId])]);
      setFeedback(result.added ? 'Título adicionado à lista.' : 'Este título já está nesta lista.');
      await refresh();
    },
  });
  const createList = useMutation({
    mutationFn: async (name: string) => {
      const list = await createCustomList(name, '');
      await setCustomListTitle(list.id, title);
      return list;
    },
    onSuccess: async (list) => {
      setAddedLists((current) => [...current, list.id]);
      setFeedback('Lista criada e título adicionado.');
      await refresh();
    },
    onError: () => { void queryClient.invalidateQueries({ queryKey: listsQueryKeys.customAll }); },
  });
  useErrorToast(status.error, 'Não foi possível verificar seus títulos salvos.');
  useErrorToast(lists.error, 'Não foi possível carregar suas listas.');
  useErrorToast(update.error, 'Não foi possível salvar o título.');
  useErrorToast(addToList.error, 'Não foi possível adicionar à lista.');
  useErrorToast(createList.error, 'Não foi possível criar a lista e adicionar o título.');

  return {
    status,
    relation,
    lists,
    update,
    addToList,
    createList,
    feedback,
    setFeedback,
    addedLists,
    busy: update.isPending || status.isFetching || !relation,
    listBusy: addToList.isPending || createList.isPending,
  };
}
