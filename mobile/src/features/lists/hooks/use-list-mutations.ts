import { useQueryClient } from '@tanstack/react-query';
import { useCallback, useState } from 'react';

import type { CatalogTitle } from '@/features/catalog/api/catalog-api';
import {
  createCustomList,
  deleteCustomList,
  moveCustomListTitle,
  removeCustomListTitle,
  removeFavorite,
  removeWatchlistTitle,
  setCustomListTitle,
  setFavorite,
  setWatchlistTitle,
  updateCustomList,
  type CustomListSummary,
} from '@/features/lists/api/lists-api';
import { listsQueryKeys } from '@/features/lists/query-keys';
import { useToast } from '@/providers/toast-provider';

/**
 * Every write of the lists screen. Each action refreshes the lists caches by prefix, shows a
 * success line (`successMessage`) or an error toast, and resolves to whether it succeeded.
 */
export function useListMutations() {
  const queryClient = useQueryClient();
  const { showError } = useToast();
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const run = useCallback(async <T,>(action: () => Promise<T>, message: string): Promise<T | null> => {
    setSuccessMessage(null);
    try {
      const result = await action();
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: listsQueryKeys.favoritesAll }),
        queryClient.invalidateQueries({ queryKey: listsQueryKeys.watchlistAll }),
        queryClient.invalidateQueries({ queryKey: listsQueryKeys.customAll }),
        queryClient.invalidateQueries({ queryKey: listsQueryKeys.titleStatusAll }),
      ]);
      setSuccessMessage(message);
      return result;
    } catch (error) {
      showError(
        error instanceof Error
          ? error.message
          : 'Não foi possível salvar a alteração. Verifique a conexão e tente novamente.',
      );
      return null;
    }
  }, [queryClient, showError]);

  return {
    successMessage,
    /** Creates the list, or updates `editing` when given. Resolves to the saved list. */
    saveList: (editing: CustomListSummary | null, name: string, description: string) => run(
      () => editing
        ? updateCustomList(editing.id, name, description)
        : createCustomList(name, description),
      editing ? 'Lista atualizada.' : 'Lista criada.',
    ),
    deleteList: (listId: string) => run(() => deleteCustomList(listId), 'Lista excluída.'),
    removeFavorite: (titleId: string) =>
      run(() => removeFavorite(titleId), 'Título removido dos favoritos.'),
    removeFromWatchlist: (titleId: string) =>
      run(() => removeWatchlistTitle(titleId), 'Título removido da watchlist.'),
    removeFromList: (listId: string, titleId: string) =>
      run(() => removeCustomListTitle(listId, titleId), 'Título removido da lista.'),
    moveInList: (listId: string, titleId: string, offset: -1 | 1) =>
      run(() => moveCustomListTitle(listId, titleId, offset), 'Ordem da lista atualizada.'),
    addFavorite: (title: CatalogTitle) =>
      run(() => setFavorite(title), 'Título adicionado aos favoritos.'),
    addToWatchlist: (title: CatalogTitle) =>
      run(() => setWatchlistTitle(title), 'Título adicionado à watchlist.'),
    addToList: (listId: string, title: CatalogTitle) =>
      run(() => setCustomListTitle(listId, title), 'Título adicionado à lista.'),
  };
}

export type ListMutations = ReturnType<typeof useListMutations>;
