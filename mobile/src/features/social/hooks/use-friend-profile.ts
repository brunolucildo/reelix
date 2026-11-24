import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { fetchPublicGamificationBadges } from '@/features/gamification/api/gamification-api';
import { gamificationQueryKeys } from '@/features/gamification/query-keys';
import {
  cancelFriendRequest,
  fetchFriendActivities,
  fetchFriendProfile,
  removeFriend,
  respondToFriendRequest,
  sendFriendRequest,
  type FriendProfileData,
} from '@/features/social/api/social-api';
import { socialQueryKeys } from '@/features/social/query-keys';
import { getNextCursor } from '@/lib/pagination';
import { useErrorToast } from '@/providers/toast-provider';

export type FriendProfileTab = 'overview' | 'activities' | 'favorites' | 'badges';

/**
 * A friend's profile, public badges, shared activities and the friendship actions
 * (send, cancel, accept, decline, remove). `onFriendshipChanged` runs after an action succeeds.
 */
export function useFriendProfile(accountId: string, tab: FriendProfileTab, onFriendshipChanged: () => void) {
  const queryClient = useQueryClient();
  const profile = useQuery({
    queryKey: socialQueryKeys.friendProfile(accountId),
    queryFn: ({ signal }) => fetchFriendProfile(accountId, signal),
    enabled: Boolean(accountId),
  });
  const badges = useQuery({
    queryKey: gamificationQueryKeys.publicBadges(accountId),
    queryFn: ({ signal }) => fetchPublicGamificationBadges(accountId, signal),
    enabled: Boolean(profile.data),
  });
  const canSeeActivities = Boolean(profile.data?.watchHistoryShared || profile.data?.ratingsShared);
  const activities = useInfiniteQuery({
    queryKey: socialQueryKeys.friendActivities(accountId),
    queryFn: ({ pageParam, signal }) => fetchFriendActivities(accountId, pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: getNextCursor,
    enabled: canSeeActivities && (tab === 'overview' || tab === 'activities'),
  });
  const relationshipAction = useMutation({
    mutationFn: async (action: 'send' | 'cancel' | 'accept' | 'decline' | 'remove') => {
      if (action === 'send') return sendFriendRequest(accountId);
      const id = profile.data?.relationshipId;
      if (!id) throw new Error('Atualize o perfil antes de continuar.');
      if (action === 'cancel') return cancelFriendRequest(id);
      if (action === 'remove') return removeFriend(id);
      return respondToFriendRequest(id, action);
    },
    onSuccess: async (_, action) => {
      onFriendshipChanged();
      if (action === 'remove') {
        queryClient.setQueryData<FriendProfileData>(socialQueryKeys.friendProfile(accountId), (current) => current ? { ...current, relationshipStatus: 'removed', favorites: [], favoritesShared: false, watchHistoryShared: false, ratingsShared: false } : current);
      }
      await queryClient.cancelQueries({ queryKey: socialQueryKeys.friendActivitiesAll });
      await queryClient.cancelQueries({ queryKey: socialQueryKeys.feed });
      queryClient.removeQueries({ queryKey: socialQueryKeys.friendActivitiesAll });
      queryClient.removeQueries({ queryKey: socialQueryKeys.feed });
      await queryClient.invalidateQueries({ queryKey: socialQueryKeys.all });
    },
    onError: () => { void profile.refetch(); },
  });
  useErrorToast(profile.error, 'Não foi possível carregar o perfil do amigo.');
  useErrorToast(badges.error, 'Não foi possível carregar os badges compartilhados.');
  useErrorToast(activities.error, 'Não foi possível carregar as atividades compartilhadas.');
  useErrorToast(relationshipAction.error, 'Não foi possível atualizar a amizade.');

  return {
    profile,
    badges,
    activities,
    canSeeActivities,
    relationshipAction,
    allActivities: activities.data?.pages.flatMap((page) => page.items) ?? [],
  };
}
