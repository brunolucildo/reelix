import { useRouter } from 'expo-router';
import { Pressable, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { SocialAvatar } from '@/features/social/components/social-avatar';
import { socialStyles as styles } from '@/features/social/components/social-styles';
import type { useSocialQueries } from '@/features/social/hooks/use-social-queries';

type Props = {
  friends: ReturnType<typeof useSocialQueries>['friends'];
  allFriends: ReturnType<typeof useSocialQueries>['allFriends'];
  busy: boolean;
  onRemove: (relationshipId: string, displayName: string) => void;
};

/** "Amigos": accepted friends, with a shortcut to each profile and the remove action. */
export function FriendsSection({ friends, allFriends, busy, onRemove }: Props) {
  const router = useRouter();

  return (
    <View style={styles.section}>
      <Text style={styles.sectionTitle}>Seus amigos</Text>
      {allFriends.length === 0 && !friends.isPending && !friends.isError ? (
        <Text style={styles.secondaryText}>Quando você aceitar uma solicitação, seus amigos aparecerão aqui.</Text>
      ) : allFriends.map((friend) => (
        <View key={friend.relationshipId} style={styles.listRow}>
          <SocialAvatar name={friend.displayName} uri={friend.avatarUrl} />
          <Pressable
            accessibilityRole="button"
            disabled={busy}
            onPress={() => router.push({
              pathname: '/social-profile/[accountId]',
              params: { accountId: friend.accountId },
            })}
            style={styles.friendLink}
          >
            <Text style={styles.rowTitle}>{friend.displayName}</Text>
          </Pressable>
          <Pressable
            accessibilityRole="button"
            disabled={busy}
            onPress={() => onRemove(friend.relationshipId, friend.displayName)}
            style={styles.actionButton}
          >
            <Text style={styles.actionText}>Remover</Text>
          </Pressable>
        </View>
      ))}
      {friends.hasNextPage ? (
        <PrimaryButton label="Mais amigos" loading={friends.isFetchingNextPage} onPress={() => void friends.fetchNextPage()} variant="secondary" />
      ) : null}
    </View>
  );
}
