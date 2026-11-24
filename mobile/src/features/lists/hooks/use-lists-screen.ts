import { useState } from 'react';

import {
  useCustomList,
  useCustomLists,
  useFavorites,
  useWatchlist,
} from '@/features/lists/hooks/use-lists-queries';
import { useListTitleDetails } from '@/features/lists/hooks/use-list-title-details';

export type ListsSection = 'favorites' | 'watchlist' | 'lists';

/**
 * Navigation state of the lists screen (tab, pages, selected list) and the queries that feed it.
 * Only the active tab's queries run.
 */
export function useListsScreen(initialSection: string | undefined) {
  const [section, setSection] = useState<ListsSection>(() =>
    initialSection === 'lists' || initialSection === 'watchlist' ? initialSection : 'favorites');
  const [page, setPage] = useState(1);
  const [listPage, setListPage] = useState(1);
  const [oldestFirst, setOldestFirst] = useState(false);
  const [selectedListId, setSelectedListId] = useState<string | null>(null);

  const favorites = useFavorites(page, section === 'favorites');
  const watchlist = useWatchlist(page, oldestFirst, section === 'watchlist');
  const customLists = useCustomLists(listPage, section === 'lists');
  const activeListId = selectedListId ?? customLists.data?.items[0]?.id ?? null;
  const selectedList = useCustomList(activeListId, page, section === 'lists');

  const collection = section === 'favorites' ? favorites : watchlist;
  const references = section === 'lists'
    ? selectedList.data?.titles.items ?? []
    : collection.data?.items ?? [];
  const titleDetails = useListTitleDetails(references);

  const changeSection = (next: ListsSection) => {
    setPage(1);
    setSection(next);
  };

  return {
    section,
    changeSection,
    page,
    setPage,
    listPage,
    oldestFirst,
    toggleOrder: () => {
      setPage(1);
      setOldestFirst((current) => !current);
    },
    activeListId,
    selectList: (listId: string) => {
      setPage(1);
      setSelectedListId(listId);
    },
    changeListPage: (next: number) => {
      setSelectedListId(null);
      setPage(1);
      setListPage(next);
    },
    /** Shows the list that was just created or edited. */
    showList: (listId: string) => {
      setSelectedListId(listId);
      setListPage(1);
      changeSection('lists');
    },
    /** Back to the first list after the selected one was deleted. */
    resetAfterDelete: () => {
      setSelectedListId(null);
      setListPage(1);
      setPage(1);
    },
    favorites,
    watchlist,
    collection,
    customLists,
    selectedList,
    titleDetails,
  };
}

export type ListsScreenState = ReturnType<typeof useListsScreen>;
