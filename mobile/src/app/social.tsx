import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Image } from 'expo-image';
import { useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import { useRouter } from 'expo-router';

import { PrimaryButton } from '@/components/auth/primary-button';
import { BackButton } from '@/components/navigation/back-button';
import { useErrorToast } from '@/providers/toast-provider';
import {
  cancelFriendRequest,
  fetchFriendRequests,
  fetchFriends,
  fetchSocialFeed,
  removeFriend,
  respondToFriendRequest,
  searchSocialUsers,
  sendFriendRequest,
} from '@/services/api-client';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export default function SocialScreen() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [searchText, setSearchText] = useState('');
  const [searchQuery, setSearchQuery] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => setSearchQuery(searchText.trim()), 300);
    return () => clearTimeout(timer);
  }, [searchText]);

  const refreshSocial = async () => {
    queryClient.removeQueries({ queryKey: ['social', 'friend-profile'] });
    await queryClient.invalidateQueries({ queryKey: ['social'] });
  };
  const search = useQuery({
    queryKey: ['social', 'search', searchQuery],
    queryFn: ({ signal }) => searchSocialUsers(searchQuery, signal),
    enabled: searchQuery.length >= 2,
  });
  const friends = useInfiniteQuery({
    queryKey: ['social', 'friends'],
    queryFn: ({ pageParam, signal }) => fetchFriends(pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
  const incoming = useInfiniteQuery({
    queryKey: ['social', 'requests', 'incoming'],
    queryFn: ({ pageParam, signal }) => fetchFriendRequests('incoming', pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
  const outgoing = useInfiniteQuery({
    queryKey: ['social', 'requests', 'outgoing'],
    queryFn: ({ pageParam, signal }) => fetchFriendRequests('outgoing', pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
  const feed = useInfiniteQuery({
    queryKey: ['social', 'feed'],
    queryFn: ({ pageParam, signal }) => fetchSocialFeed(pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });

  useErrorToast(search.error, 'Não foi possível pesquisar pessoas.');
  useErrorToast(friends.error, 'Não foi possível carregar seus amigos.');
  useErrorToast(incoming.error, 'Não foi possível carregar as solicitações recebidas.');
  useErrorToast(outgoing.error, 'Não foi possível carregar as solicitações enviadas.');
  useErrorToast(feed.error, 'Não foi possível carregar o feed de atividades.');

  const request = useMutation({
    mutationFn: sendFriendRequest,
    onSuccess: refreshSocial,
  });
  const respond = useMutation({
    mutationFn: ({ id, response }: { id: string; response: 'accept' | 'decline' }) =>
      respondToFriendRequest(id, response),
    onSuccess: refreshSocial,
  });
  const cancel = useMutation({
    mutationFn: cancelFriendRequest,
    onSuccess: refreshSocial,
  });
  const remove = useMutation({
    mutationFn: removeFriend,
    onSuccess: refreshSocial,
  });
  useErrorToast(request.error, 'Não foi possível enviar a solicitação de amizade.');
  useErrorToast(respond.error, 'Não foi possível responder à solicitação.');
  useErrorToast(cancel.error, 'Não foi possível cancelar a solicitação.');
  useErrorToast(remove.error, 'Não foi possível remover a amizade.');

  const allFriends = friends.data?.pages.flatMap((page) => page.items) ?? [];
  const allIncoming = incoming.data?.pages.flatMap((page) => page.items) ?? [];
  const allOutgoing = outgoing.data?.pages.flatMap((page) => page.items) ?? [];
  const allActivities = feed.data?.pages.flatMap((page) => page.items) ?? [];

  const confirmRemove = (relationshipId: string, displayName: string) => {
    Alert.alert(
      'Remover amizade?',
      `Você deixará de compartilhar atividades com ${displayName}. O histórico de vocês não será apagado.`,
      [
        { text: 'Cancelar', style: 'cancel' },
        { text: 'Remover', style: 'destructive', onPress: () => remove.mutate(relationshipId) },
      ],
    );
  };

  return (
    <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <View style={styles.container}>
        <BackButton />
        <View style={styles.heading}>
          <Text accessibilityRole="header" style={styles.title}>Amigos</Text>
          <Text style={styles.secondaryText}>
            Encontre pessoas e acompanhe somente atividades compartilhadas com você.
          </Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Encontrar pessoas</Text>
          <TextInput
            accessibilityLabel="Buscar pessoas pelo nome"
            autoCapitalize="words"
            onChangeText={setSearchText}
            placeholder="Digite pelo menos 2 caracteres"
            placeholderTextColor={colors.textSecondary}
            style={styles.input}
            value={searchText}
          />
          {searchQuery.length >= 2 && search.isPending ? (
            <ActivityIndicator color={colors.brand} />
          ) : null}
          {search.data?.length === 0 ? (
            <Text style={styles.secondaryText}>Nenhuma pessoa encontrada.</Text>
          ) : null}
          {search.data?.map((user) => (
            <View key={user.accountId} style={styles.listRow}>
              <SocialAvatar name={user.displayName} uri={user.avatarUrl} />
              <Text style={styles.rowTitle}>{user.displayName}</Text>
              {user.relationshipStatus === 'accepted' ? (
                <Pressable
                  accessibilityRole="button"
                  onPress={() => router.push({
                    pathname: '/social-profile/[accountId]',
                    params: { accountId: user.accountId },
                  })}
                  style={styles.actionButton}
                >
                  <Text style={styles.actionText}>Ver perfil</Text>
                </Pressable>
              ) : user.relationshipStatus === 'pending' && user.relationshipId ? (
                user.requestSentByMe ? (
                  <Pressable
                    accessibilityRole="button"
                    onPress={() => cancel.mutate(user.relationshipId!)}
                    style={styles.actionButton}
                  >
                    <Text style={styles.actionText}>Cancelar pedido</Text>
                  </Pressable>
                ) : (
                  <View style={styles.inlineActions}>
                    <Pressable
                      accessibilityRole="button"
                      onPress={() => respond.mutate({ id: user.relationshipId!, response: 'accept' })}
                      style={styles.actionButton}
                    >
                      <Text style={styles.actionText}>Aceitar</Text>
                    </Pressable>
                    <Pressable
                      accessibilityRole="button"
                      onPress={() => respond.mutate({ id: user.relationshipId!, response: 'decline' })}
                      style={styles.actionButton}
                    >
                      <Text style={styles.actionText}>Recusar</Text>
                    </Pressable>
                  </View>
                )
              ) : (
                <>
                  {user.relationshipStatus === 'declined' ? (
                    <Text style={styles.secondaryText}>Solicitação recusada anteriormente</Text>
                  ) : user.relationshipStatus === 'cancelled' ? (
                    <Text style={styles.secondaryText}>Solicitação cancelada</Text>
                  ) : user.relationshipStatus === 'removed' ? (
                    <Text style={styles.secondaryText}>Amizade removida</Text>
                  ) : null}
                  <Pressable
                    accessibilityRole="button"
                    onPress={() => request.mutate(user.accountId)}
                    style={styles.actionButton}
                  >
                    <Text style={styles.actionText}>
                      {user.relationshipStatus === 'declined' || user.relationshipStatus === 'cancelled'
                        ? 'Enviar nova solicitação'
                        : 'Adicionar'}
                    </Text>
                  </Pressable>
                </>
              )}
            </View>
          ))}
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Solicitações recebidas</Text>
          {allIncoming.length === 0 ? (
            <Text style={styles.secondaryText}>Você não tem solicitações pendentes.</Text>
          ) : allIncoming.map((item) => (
            <View key={item.relationshipId} style={styles.listRow}>
              <SocialAvatar name={item.displayName} uri={item.avatarUrl} />
              <Text style={styles.rowTitle}>{item.displayName}</Text>
              <View style={styles.inlineActions}>
                <Pressable
                  accessibilityRole="button"
                  onPress={() => respond.mutate({ id: item.relationshipId, response: 'accept' })}
                  style={styles.actionButton}
                >
                  <Text style={styles.actionText}>Aceitar</Text>
                </Pressable>
                <Pressable
                  accessibilityRole="button"
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

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Solicitações enviadas</Text>
          {allOutgoing.length === 0 ? (
            <Text style={styles.secondaryText}>Você não tem solicitações enviadas pendentes.</Text>
          ) : allOutgoing.map((item) => (
            <View key={item.relationshipId} style={styles.listRow}>
              <SocialAvatar name={item.displayName} uri={item.avatarUrl} />
              <Text style={styles.rowTitle}>{item.displayName}</Text>
              <Pressable
                accessibilityRole="button"
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

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Seus amigos</Text>
          {allFriends.length === 0 ? (
            <Text style={styles.secondaryText}>Quando você aceitar uma solicitação, seus amigos aparecerão aqui.</Text>
          ) : allFriends.map((friend) => (
            <View key={friend.relationshipId} style={styles.listRow}>
              <SocialAvatar name={friend.displayName} uri={friend.avatarUrl} />
              <Pressable
                accessibilityRole="button"
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
                onPress={() => confirmRemove(friend.relationshipId, friend.displayName)}
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

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Atividades compartilhadas</Text>
          {allActivities.length === 0 ? (
            <Text style={styles.secondaryText}>Ainda não há atividades compartilhadas pelos seus amigos.</Text>
          ) : allActivities.map((activity) => (
            <View key={`${activity.activityType}-${activity.id}`} style={styles.activity}>
              <Text style={styles.rowTitle}>
                <Text
                  accessibilityRole="link"
                  onPress={() => router.push({
                    pathname: '/social-profile/[accountId]',
                    params: { accountId: activity.actorAccountId },
                  })}
                >
                  {activity.actorDisplayName}
                </Text>{' '}
                {activity.activityType === 'ratingUpdated' ? 'avaliou' : 'registrou uma visualização de'}{' '}
                {activity.targetType === 'movie' ? 'um filme' : activity.targetType === 'series' ? 'uma série' : 'um episódio'}
              </Text>
              <Text style={styles.secondaryText}>
                {new Date(activity.occurredAt).toLocaleDateString('pt-BR')}
              </Text>
              {activity.targetType !== 'episode' ? (
                <Pressable
                  accessibilityRole="button"
                  onPress={() => router.push({
                    pathname: '/title/[provider]/[contentType]/[externalId]',
                    params: {
                      provider: activity.provider,
                      contentType: activity.targetType,
                      externalId: activity.externalId,
                    },
                  })}
                  style={styles.actionButton}
                >
                  <Text style={styles.actionText}>Abrir título</Text>
                </Pressable>
              ) : null}
            </View>
          ))}
          {feed.hasNextPage ? (
            <PrimaryButton label="Mais atividades" loading={feed.isFetchingNextPage} onPress={() => void feed.fetchNextPage()} variant="secondary" />
          ) : null}
        </View>
      </View>
    </ScrollView>
  );
}

function SocialAvatar({ name, uri }: { name: string; uri?: string | null }) {
  const initials = name.trim().slice(0, 1).toLocaleUpperCase('pt-BR') || '?';
  return uri ? (
    <Image accessibilityLabel={`Avatar de ${name}`} contentFit="cover" source={{ uri }} style={styles.avatar} />
  ) : (
    <View accessibilityLabel={`Avatar de ${name}`} style={styles.avatarFallback}>
      <Text style={styles.avatarInitial}>{initials}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    backgroundColor: colors.background,
    paddingHorizontal: spacing.large,
    paddingVertical: spacing.extraLarge,
  },
  container: {
    width: '100%',
    maxWidth: 620,
    alignSelf: 'center',
    gap: spacing.large,
  },
  backButton: {
    minHeight: 44,
    justifyContent: 'center',
  },
  backLabel: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  heading: {
    gap: spacing.small,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.extraBold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    lineHeight: 21,
    fontFamily: fontFamily.regular,
  },
  section: {
    gap: spacing.small,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: spacing.medium,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  input: {
    minHeight: 52,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  listRow: {
    minHeight: 52,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
  },
  avatarFallback: {
    width: 40,
    height: 40,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
  },
  avatarInitial: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  rowTitle: {
    flexShrink: 1,
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.medium,
  },
  actionButton: {
    minHeight: 40,
    justifyContent: 'center',
    paddingHorizontal: spacing.small,
  },
  actionText: {
    color: colors.brandSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
  },
  inlineActions: {
    flexDirection: 'row',
    gap: spacing.extraSmall,
  },
  friendLink: {
    flex: 1,
    minHeight: 44,
    justifyContent: 'center',
  },
  activity: {
    gap: spacing.extraSmall,
    paddingVertical: spacing.small,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
});
