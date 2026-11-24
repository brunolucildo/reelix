import { useInfiniteQuery, useQuery } from '@tanstack/react-query';

import {
  fetchFriendRequests,
  fetchFriends,
  fetchSocialFeed,
  searchSocialUsers,
} from '@/features/social/api/social-api';
import { socialQueryKeys } from '@/features/social/query-keys';
import { getNextCursor } from '@/lib/pagination';
import { useErrorToast } from '@/providers/toast-provider';

export type SocialTab = 'feed' | 'requests' | 'friends' | 'discover' | 'activities';
export type RequestDirection = 'incoming' | 'outgoing';

/**
 * Queries of the social screen. Only what the active tab shows is fetched, except incoming
 * requests (always on, for the pending badge) and the feed.
 */
export function useSocialQueries(
  activeTab: SocialTab,
  requestDirection: RequestDirection,
  searchQuery: string,
) {
  const search = useQuery({
    queryKey: socialQueryKeys.search(searchQuery),
    queryFn: ({ signal }) => searchSocialUsers(searchQuery, signal),
    enabled: activeTab === 'discover' && searchQuery.length >= 2,
  });
  const friends = useInfiniteQuery({
    queryKey: socialQueryKeys.friends,
    queryFn: ({ pageParam, signal }) => fetchFriends(pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: getNextCursor,
    enabled: activeTab === 'friends',
  });
  const incoming = useInfiniteQuery({
    queryKey: socialQueryKeys.requests('incoming'),
    queryFn: ({ pageParam, signal }) => fetchFriendRequests('incoming', pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: getNextCursor,
  });
  const outgoing = useInfiniteQuery({
    queryKey: socialQueryKeys.requests('outgoing'),
    queryFn: ({ pageParam, signal }) => fetchFriendRequests('outgoing', pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: getNextCursor,
    enabled: activeTab === 'requests' && requestDirection === 'outgoing',
  });
  const feed = useInfiniteQuery({
    queryKey: socialQueryKeys.feed,
    queryFn: ({ pageParam, signal }) => fetchSocialFeed(pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: getNextCursor,
  });

  useErrorToast(search.error, 'Não foi possível pesquisar pessoas.');
  useErrorToast(friends.error, 'Não foi possível carregar seus amigos.');
  useErrorToast(incoming.error, 'Não foi possível carregar as solicitações recebidas.');
  useErrorToast(outgoing.error, 'Não foi possível carregar as solicitações enviadas.');
  useErrorToast(feed.error, 'Não foi possível carregar o feed de atividades.');

  const isFeed = activeTab === 'feed' || activeTab === 'activities';
  const activeQuery = isFeed ? feed : activeTab === 'friends' ? friends
    : activeTab === 'requests' ? (requestDirection === 'incoming' ? incoming : outgoing) : search;

  return {
    search,
    friends,
    incoming,
    outgoing,
    feed,
    allFriends: friends.data?.pages.flatMap((page) => page.items) ?? [],
    allIncoming: incoming.data?.pages.flatMap((page) => page.items) ?? [],
    allOutgoing: outgoing.data?.pages.flatMap((page) => page.items) ?? [],
    allActivities: feed.data?.pages.flatMap((page) => page.items) ?? [],
    isFeed,
    activeQuery,
    canLoad: activeTab !== 'discover' || searchQuery.length >= 2,
  };
}
