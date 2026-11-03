import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Image } from 'expo-image';
import { useEffect, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  RefreshControl,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import { useRouter } from 'expo-router';

import FeedIcon from '../../assets/brand/navigation-icons/home.svg';
import RequestsIcon from '../../assets/brand/navigation-icons/profile.svg';
import FriendsIcon from '../../assets/brand/navigation-icons/friends.svg';
import SearchIcon from '../../assets/brand/navigation-icons/search.svg';
import ActivityIcon from '../../assets/brand/navigation-icons/clock.svg';
import { PrimaryButton } from '@/components/auth/primary-button';
import { ModalDialog } from '@/components/modal-dialog';
import { SocialActivityCard } from '@/components/social/social-activity-card';
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
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

const socialTabs = [
  { key: 'feed', label: 'Feed', Icon: FeedIcon },
  { key: 'requests', label: 'Solicitações', Icon: RequestsIcon },
  { key: 'friends', label: 'Amigos', Icon: FriendsIcon },
  { key: 'discover', label: 'Descobrir', Icon: SearchIcon },
  { key: 'activities', label: 'Atividades', Icon: ActivityIcon },
] as const;

export default function SocialScreen() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [searchText, setSearchText] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [activeTab, setActiveTab] = useState<typeof socialTabs[number]['key']>('feed');
  const [requestDirection, setRequestDirection] = useState<'incoming' | 'outgoing'>('incoming');
  const [searchFocused, setSearchFocused] = useState(false);
  const [activityFilter, setActivityFilter] = useState<'all' | 'watched' | 'ratings'>('all');
  const [friendToRemove, setFriendToRemove] = useState<{ id: string; name: string } | null>(null);

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
    enabled: activeTab === 'discover' && searchQuery.length >= 2,
  });
  const friends = useInfiniteQuery({
    queryKey: ['social', 'friends'],
    queryFn: ({ pageParam, signal }) => fetchFriends(pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
    enabled: activeTab === 'friends',
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
    enabled: activeTab === 'requests' && requestDirection === 'outgoing',
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
    onSuccess: async () => {
      setFriendToRemove(null);
      await refreshSocial();
    },
  });
  useErrorToast(request.error, 'Não foi possível enviar a solicitação de amizade.');
  useErrorToast(respond.error, 'Não foi possível responder à solicitação.');
  useErrorToast(cancel.error, 'Não foi possível cancelar a solicitação.');
  useErrorToast(remove.error, 'Não foi possível remover a amizade.');

  const allFriends = friends.data?.pages.flatMap((page) => page.items) ?? [];
  const allIncoming = incoming.data?.pages.flatMap((page) => page.items) ?? [];
  const allOutgoing = outgoing.data?.pages.flatMap((page) => page.items) ?? [];
  const allActivities = feed.data?.pages.flatMap((page) => page.items) ?? [];
  const visibleActivities = activeTab !== 'activities' || activityFilter === 'all'
    ? allActivities
    : allActivities.filter((activity) => activityFilter === 'ratings'
      ? activity.activityType === 'ratingUpdated'
      : activity.activityType === 'movieWatched' || activity.activityType === 'episodeWatched');
  const isFeed = activeTab === 'feed' || activeTab === 'activities';
  const activeQuery = isFeed ? feed : activeTab === 'friends' ? friends
    : activeTab === 'requests' ? (requestDirection === 'incoming' ? incoming : outgoing) : search;
  const canLoad = activeTab !== 'discover' || searchQuery.length >= 2;
  const busy = request.isPending || respond.isPending || cancel.isPending || remove.isPending;

  const confirmRemove = (relationshipId: string, displayName: string) => {
    setFriendToRemove({ id: relationshipId, name: displayName });
  };

  return (
    <>
    <FlatList
      key={activeTab}
      data={isFeed ? visibleActivities : []}
      keyExtractor={(activity) => `${activity.activityType}-${activity.id}`}
      renderItem={({ item }) => <SocialActivityCard activity={item} compact={activeTab === 'activities'} />}
      contentContainerStyle={styles.content}
      keyboardShouldPersistTaps="handled"
      refreshControl={<RefreshControl refreshing={canLoad && activeQuery.isRefetching && !('isFetchingNextPage' in activeQuery && activeQuery.isFetchingNextPage)} onRefresh={() => { if (canLoad) void activeQuery.refetch(); }} tintColor={colors.brand} />}
      onEndReached={() => { if (isFeed && feed.hasNextPage && !feed.isFetching && !feed.isError) void feed.fetchNextPage(); }}
      onEndReachedThreshold={0.4}
      ListHeaderComponent={
      <View style={styles.container}>
        <BackButton />
        <View style={styles.headerRow}>
          <View style={styles.heading}>
            <Text accessibilityRole="header" style={styles.title}>Amigos</Text>
            <Text style={styles.secondaryText}>O que sua rede está assistindo?</Text>
          </View>
          <View style={styles.inlineActions}>
            <Pressable accessibilityRole="button" accessibilityLabel="Adicionar pessoas" accessibilityHint="Abre a descoberta de pessoas" onPress={() => setActiveTab('discover')} style={styles.iconButton}>
              <RequestsIcon width={dimensions.iconSize} height={dimensions.iconSize} />
              <Text style={styles.addSymbol}>+</Text>
            </Pressable>
            <Pressable accessibilityRole="button" accessibilityLabel="Pesquisar pessoas" onPress={() => setActiveTab('discover')} style={styles.iconButton}>
              <SearchIcon width={dimensions.iconSize} height={dimensions.iconSize} />
            </Pressable>
          </View>
        </View>

        <View accessibilityRole="tablist" style={styles.navigation}>
          {socialTabs.map(({ key, label, Icon }) => (
            <Pressable
              key={key}
              accessibilityRole="tab"
              accessibilityLabel={key === 'requests' ? `${label}, ${allIncoming.length}${incoming.hasNextPage ? ' ou mais' : ''} pendentes` : label}
              accessibilityState={{ selected: activeTab === key }}
              onPress={() => setActiveTab(key)}
              style={[styles.navigationTab, activeTab === key && styles.navigationTabActive]}
            >
              <View>
                <Icon width={dimensions.iconSize} height={dimensions.iconSize} />
                {key === 'requests' && allIncoming.length > 0 ? (
                  <View style={styles.badge}><Text style={styles.badgeText}>{allIncoming.length}{incoming.hasNextPage ? '+' : ''}</Text></View>
                ) : null}
              </View>
              <Text style={[styles.tabLabel, activeTab === key && styles.tabLabelActive]}>{label}</Text>
            </Pressable>
          ))}
        </View>

        {canLoad && activeQuery.isPending ? <ActivityIndicator accessibilityLabel="Carregando" color={colors.brand} /> : null}
        {canLoad && activeQuery.isError ? <PrimaryButton label="Tentar novamente" onPress={() => void activeQuery.refetch()} variant="secondary" /> : null}
        {activeTab === 'discover' ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Encontrar pessoas</Text>
          <TextInput
            accessibilityLabel="Buscar pessoas pelo nome"
            autoCapitalize="words"
            onChangeText={setSearchText}
            onFocus={() => setSearchFocused(true)}
            onBlur={() => setSearchFocused(false)}
            placeholder="Buscar por nome"
            placeholderTextColor={colors.textSecondary}
            style={[styles.input, searchFocused && styles.inputFocused]}
            value={searchText}
          />
          {searchText.trim().length < 2 ? <Text style={styles.secondaryText}>Digite pelo menos 2 caracteres.</Text> : null}
          {search.data?.length === 0 ? (
            <Text style={styles.secondaryText}>Nenhuma pessoa encontrada.</Text>
          ) : null}
          {search.data?.map((user) => (
            <View key={user.accountId} style={styles.listRow}>
              <SocialAvatar name={user.displayName} uri={user.avatarUrl} />
              <Pressable accessibilityRole="button" accessibilityLabel={`Ver perfil de ${user.displayName}`} onPress={() => router.push({ pathname: '/social-profile/[accountId]', params: { accountId: user.accountId } })} style={[styles.personDetails, styles.friendLink]}>
                <Text style={styles.rowTitle}>{user.displayName}</Text>
                {user.relationshipStatus === 'accepted' ? <Text style={styles.secondaryText}>Amigos</Text> : user.relationshipStatus === 'pending' ? <Text style={styles.secondaryText}>{user.requestSentByMe ? 'Solicitação enviada' : 'Quer ser seu amigo'}</Text> : null}
              </Pressable>
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
                    disabled={busy}
                    onPress={() => cancel.mutate(user.relationshipId!)}
                    style={styles.actionButton}
                  >
                    <Text style={styles.actionText}>Cancelar pedido</Text>
                  </Pressable>
                ) : (
                  <View style={styles.inlineActions}>
                    <Pressable
                      accessibilityRole="button"
                      disabled={busy}
                      onPress={() => respond.mutate({ id: user.relationshipId!, response: 'accept' })}
                      style={styles.actionButton}
                    >
                      <Text style={styles.actionText}>Aceitar</Text>
                    </Pressable>
                    <Pressable
                      accessibilityRole="button"
                      disabled={busy}
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
                    disabled={busy}
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
        ) : null}

        {activeTab === 'requests' ? (
          <View style={styles.inlineActions}>
            {(['incoming', 'outgoing'] as const).map((direction) => (
              <Pressable key={direction} accessibilityRole="tab" accessibilityState={{ selected: requestDirection === direction }} onPress={() => setRequestDirection(direction)} style={[styles.actionButton, requestDirection === direction && styles.navigationTabActive]}>
                <Text style={styles.actionText}>{direction === 'incoming' ? 'Recebidas' : 'Enviadas'}</Text>
              </Pressable>
            ))}
          </View>
        ) : null}
        {activeTab === 'requests' && requestDirection === 'incoming' ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Solicitações recebidas</Text>
          {allIncoming.length === 0 && !incoming.isPending && !incoming.isError ? (
            <Text style={styles.secondaryText}>Você não tem solicitações pendentes.</Text>
          ) : allIncoming.map((item) => (
            <View key={item.relationshipId} style={styles.listRow}>
              <SocialAvatar name={item.displayName} uri={item.avatarUrl} />
              <Pressable accessibilityRole="button" accessibilityLabel={`Ver perfil de ${item.displayName}`} onPress={() => router.push({ pathname: '/social-profile/[accountId]', params: { accountId: item.accountId } })} style={styles.friendLink}><Text style={styles.rowTitle}>{item.displayName}</Text></Pressable>
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
        ) : null}

        {activeTab === 'requests' && requestDirection === 'outgoing' ? (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Solicitações enviadas</Text>
          {allOutgoing.length === 0 && !outgoing.isPending && !outgoing.isError ? (
            <Text style={styles.secondaryText}>Você não tem solicitações enviadas pendentes.</Text>
          ) : allOutgoing.map((item) => (
            <View key={item.relationshipId} style={styles.listRow}>
              <SocialAvatar name={item.displayName} uri={item.avatarUrl} />
              <Pressable accessibilityRole="button" accessibilityLabel={`Ver perfil de ${item.displayName}`} onPress={() => router.push({ pathname: '/social-profile/[accountId]', params: { accountId: item.accountId } })} style={styles.friendLink}><Text style={styles.rowTitle}>{item.displayName}</Text></Pressable>
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
        ) : null}

        {activeTab === 'friends' ? (
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
        ) : null}

        {activeTab === 'feed' || activeTab === 'activities' ? (
          <View style={styles.heading}>
            <Text style={styles.sectionTitle}>{activeTab === 'feed' ? 'Na sua rede' : 'Histórico de atividades'}</Text>
            {activeTab === 'activities' ? <View accessibilityRole="tablist" style={styles.filterRow}>
              {([{ key: 'all', label: 'Todas' }, { key: 'watched', label: 'Assistidos' }, { key: 'ratings', label: 'Avaliações' }] as const).map(({ key, label }) => (
                <Pressable key={key} accessibilityRole="tab" accessibilityState={{ selected: activityFilter === key }} onPress={() => setActivityFilter(key)} style={[styles.actionButton, activityFilter === key && styles.navigationTabActive]}><Text style={styles.actionText}>{label}</Text></Pressable>
              ))}
            </View> : null}
            {visibleActivities.length === 0 && !feed.isPending && !feed.isError ? <View style={styles.emptyState}>
              <FriendsIcon width={dimensions.avatarSize} height={dimensions.avatarSize} />
              <Text style={styles.sectionTitle}>{allActivities.length ? 'Nenhuma atividade deste tipo' : 'Sua próxima descoberta vem de uma amizade'}</Text>
              <Text style={styles.secondaryText}>{allActivities.length ? 'Não há atividades deste tipo entre as carregadas.' : 'As atividades que seus amigos compartilharem com você aparecerão aqui.'}</Text>
              {!allActivities.length ? <PrimaryButton label="Encontrar pessoas" onPress={() => setActiveTab('discover')} /> : null}
            </View> : null}
          </View>
        ) : null}
      </View>
      }
      ListFooterComponent={isFeed && feed.hasNextPage ? <View style={styles.container}><PrimaryButton label="Mais atividades" loading={feed.isFetchingNextPage} onPress={() => void feed.fetchNextPage()} variant="secondary" /></View> : undefined}
    />
    <ModalDialog visible={friendToRemove !== null} title="Remover amizade?" onClose={() => setFriendToRemove(null)} busy={remove.isPending}>
      <Text style={styles.secondaryText}>Você deixará de compartilhar atividades com {friendToRemove?.name}. O histórico de vocês não será apagado.</Text>
      <PrimaryButton label="Remover amizade" loading={remove.isPending} onPress={() => { if (friendToRemove) remove.mutate(friendToRemove.id); }} />
      <PrimaryButton label="Cancelar" variant="secondary" disabled={remove.isPending} onPress={() => setFriendToRemove(null)} />
    </ModalDialog>
    </>
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
    paddingHorizontal: spacing.medium,
    paddingVertical: spacing.large,
  },
  container: {
    width: '100%',
    maxWidth: dimensions.drawerWidth * 2,
    alignSelf: 'center',
    gap: spacing.large,
  },
  headerRow: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: spacing.small },
  iconButton: { width: dimensions.minimumTouchTarget, height: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center' },
  addSymbol: { position: 'absolute', right: spacing.extraSmall, bottom: spacing.extraSmall, color: colors.brandSecondary, backgroundColor: colors.background, fontSize: typography.body, fontFamily: fontFamily.bold },
  personDetails: { flex: 1, minWidth: 0, gap: spacing.small },
  authorRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.small, minHeight: dimensions.minimumTouchTarget },
  authorName: { color: colors.textPrimary, fontFamily: fontFamily.bold, fontSize: typography.body },
  publication: { flexDirection: 'row', alignItems: 'center', gap: spacing.medium },
  poster: { width: dimensions.posterWidth, height: dimensions.posterHeight, borderRadius: radii.small, backgroundColor: colors.surface },
  posterFallback: { width: dimensions.posterWidth, height: dimensions.posterHeight, borderRadius: radii.small, backgroundColor: colors.surface, alignItems: 'center', justifyContent: 'center' },
  eyebrow: { color: colors.textSecondary, fontSize: typography.caption, fontFamily: fontFamily.medium },
  titleLink: { minHeight: dimensions.minimumTouchTarget, justifyContent: 'center', alignItems: 'flex-start' },
  filterRow: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.small },
  emptyState: { gap: spacing.medium, alignItems: 'flex-start', paddingVertical: spacing.extraLarge },
  inputFocused: { borderColor: colors.brandSecondary },
  heading: {
    gap: spacing.small,
  },
  navigation: { flexDirection: 'row', borderBottomWidth: 1, borderBottomColor: colors.border },
  navigationTab: { flex: 1, minHeight: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center', gap: spacing.small, paddingVertical: spacing.small, borderBottomWidth: 2, borderBottomColor: colors.background },
  navigationTabActive: { borderBottomWidth: 2, borderBottomColor: colors.brand },
  tabLabel: { color: colors.textSecondary, fontSize: typography.eyebrow, fontFamily: fontFamily.medium, textAlign: 'center' },
  tabLabelActive: { color: colors.brandSecondary, fontFamily: fontFamily.bold },
  badge: { position: 'absolute', top: -spacing.small, right: -spacing.medium, backgroundColor: colors.brand, borderRadius: radii.pill, paddingHorizontal: spacing.extraSmall },
  badgeText: { color: colors.background, fontSize: typography.eyebrow, fontFamily: fontFamily.bold },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.extraBold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    lineHeight: typography.bodySmall + spacing.small,
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
    minHeight: dimensions.minimumTouchTarget + spacing.small,
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
    minHeight: dimensions.avatarSize,
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
    paddingVertical: spacing.small,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  avatar: {
    width: dimensions.minimumTouchTarget,
    height: dimensions.minimumTouchTarget,
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
  },
  avatarFallback: {
    width: dimensions.minimumTouchTarget,
    height: dimensions.minimumTouchTarget,
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
    minHeight: dimensions.minimumTouchTarget,
    minWidth: dimensions.minimumTouchTarget,
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
    flexWrap: 'wrap',
    gap: spacing.extraSmall,
  },
  friendLink: {
    flex: 1,
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
  },
  activity: {
    gap: spacing.medium,
    paddingVertical: spacing.large,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
});
