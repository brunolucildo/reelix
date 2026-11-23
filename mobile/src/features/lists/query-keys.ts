export const listsQueryKeys = {
  all: ['lists'] as const,
  favoritesAll: ['lists', 'favorites'] as const,
  favorites: (page: number) => ['lists', 'favorites', page] as const,
  watchlistAll: ['lists', 'watchlist'] as const,
  watchlist: (page: number, oldestFirst: boolean) => ['lists', 'watchlist', page, oldestFirst] as const,
  customAll: ['lists', 'custom'] as const,
  customLists: (page: number) => ['lists', 'custom', page] as const,
  customList: (listId: string | null, page: number) => ['lists', 'custom', listId, page] as const,
  titlePicker: ['lists', 'custom', 'title-picker'] as const,
  catalogSearch: (text: string) => ['lists', 'catalog-search', text] as const,
  titleStatusAll: ['lists', 'title-status'] as const,
  titleStatus: (titleIds: string[]) => ['lists', 'title-status', titleIds] as const,
  home: (collection: 'favorites' | 'watchlist', type: 'movie' | 'series') =>
    ['lists', collection, 'home', type] as const,
  startWatchingFavorites: ['lists', 'favorites', 'start-watching'] as const,
  startWatchingWatchlist: ['lists', 'watchlist', 'start-watching'] as const,
};
