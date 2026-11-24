import { useRouter } from 'expo-router';
import { Pressable, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { SocialAvatar } from '@/features/social/components/social-avatar';
import { socialStyles as styles } from '@/features/social/components/social-styles';
import type { SocialMutations } from '@/features/social/hooks/use-social-mutations';
import type { RequestDirection, useSocialQueries } from '@/features/social/hooks/use-social-queries';

type Props = {
  direction: RequestDirection;
  onDirectionChange: (direction: RequestDirection) => void;
  queries: ReturnType<typeof useSocialQueries>;
  mutations: SocialMutations;
};

/** "Solicitações": received and sent friend requests. */
export function RequestsSection({ direction, onDirectionChange, queries, mutations }: Props) {
  const router = useRouter();
  const { incoming, outgoing, allIncoming, allOutgoing } = queries;
  const { busy, respond, cancel } = mutations;
  const openProfile = (accountId: string) =>
    router.push({ pathname: '/social-profile/[accountId]', params: { accountId } });

  return (
    <>
      <View style={styles.inlineActions}>
        {(['incoming', 'outgoing'] as const).map((value) => (
          <Pressable key={value} accessibilityRole="tab" accessibilityState={{ selected: direction === value }} onPress={() => onDirectionChange(value)} style={[styles.actionButton, direction === value && styles.navigationTabActive]}>
            <Text style={styles.actionText}>{value === 'incoming' ? 'Recebidas' : 'Enviadas'}</Text>
          </Pressable>
        ))}
      </View>
      {direction === 'incoming' ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Solicitações recebidas</Text>
          {allIncoming.length === 0 && !incoming.isPending && !incoming.isError ? (
            <Text style={styles.secondaryText}>Você não tem solicitações pendentes.</Text>
          ) : allIncoming.map((item) => (
            <View key={item.relationshipId} style={styles.listRow}>
              <SocialAvatar name={item.displayName} uri={item.avatarUrl} />
              <Pressable accessibilityRole="button" accessibilityLabel={`Ver perfil de ${item.displayName}`} onPress={() => openProfile(item.accountId)} style={styles.friendLink}><Text style={styles.rowTitle}>{item.displayName}</Text></Pressable>
              <View style={styles.inlineActions}>
                <Pressable
                  accessibilityRole="button"
                  disabled={busy}
                  onPress={() => respond.mutate({ id: item.relationshipId, response: 'accept' })}
                  style={styles.actionButton}
                >
                  <Text style={styles.actionText}>Aceitar</Text>
                </Pressable>
                <Pressable
                  accessibilityRole="button"
                  disabled={busy}
                  onPress={() => respond.mutate({ id: item.relationshipId, response: 'decline' })}
                  style={styles.actionButton}
                >
                  <Text style={styles.actionText}>Recusar</Text>
                </Pressable>
              </View>
            </View>
          ))}
          {incoming.hasNextPage ? (
            <PrimaryButton label="Mais solicitações" loading={incoming.isFetchingNextPage} onPress={() => void incoming.fetchNextPage()} variant="secondary" />
          ) : null}
        </View>
      ) : (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Solicitações enviadas</Text>
          {allOutgoing.length === 0 && !outgoing.isPending && !outgoing.isError ? (
            <Text style={styles.secondaryText}>Você não tem solicitações enviadas pendentes.</Text>
          ) : allOutgoing.map((item) => (
            <View key={item.relationshipId} style={styles.listRow}>
              <SocialAvatar name={item.displayName} uri={item.avatarUrl} />
              <Pressable accessibilityRole="button" accessibilityLabel={`Ver perfil de ${item.displayName}`} onPress={() => openProfile(item.accountId)} style={styles.friendLink}><Text style={styles.rowTitle}>{item.displayName}</Text></Pressable>
              <Pressable
                accessibilityRole="button"
                disabled={busy}
                onPress={() => cancel.mutate(item.relationshipId)}
                style={styles.actionButton}
              >
                <Text style={styles.actionText}>Cancelar</Text>
              </Pressable>
            </View>
          ))}
          {outgoing.hasNextPage ? (
            <PrimaryButton label="Mais solicitações" loading={outgoing.isFetchingNextPage} onPress={() => void outgoing.fetchNextPage()} variant="secondary" />
          ) : null}
        </View>
      )}
    </>
  );
}
