import { useLocalSearchParams, useRouter } from 'expo-router';
import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, RefreshControl, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { PrimaryButton } from '@/components/auth/primary-button';
import { AchievementMedal } from '@/components/gamification/achievement-medal';
import { TitlePosterCard } from '@/components/discovery/title-poster-card';
import { ModalDialog } from '@/components/modal-dialog';
import { SocialActivityCard } from '@/components/social/social-activity-card';
import { UserAvatar } from '@/components/user-avatar';
import { BackButton } from '@/components/navigation/back-button';
import { useErrorToast } from '@/providers/toast-provider';
import { cancelFriendRequest, fetchCatalogTitleDetails, fetchFriendActivities, fetchFriendProfile, fetchPublicGamificationBadges, removeFriend, respondToFriendRequest, sendFriendRequest, type FriendProfileData, type PublicBadges } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import OverviewIcon from '../../../assets/brand/navigation-icons/profile.svg';
import ActivityIcon from '../../../assets/brand/navigation-icons/clock.svg';
import FavoritesIcon from '../../../assets/brand/navigation-icons/favorites.svg';
import AchievementsIcon from '../../../assets/brand/navigation-icons/achievements.svg';
import FriendsIcon from '../../../assets/brand/navigation-icons/friends.svg';
import MoreIcon from '../../../assets/brand/navigation-icons/more.svg';

const profileTabs = [
  { key: 'overview', label: 'Visão geral', Icon: OverviewIcon },
  { key: 'activities', label: 'Atividades', Icon: ActivityIcon },
  { key: 'favorites', label: 'Favoritos', Icon: FavoritesIcon },
  { key: 'badges', label: 'Conquistas', Icon: AchievementsIcon },
] as const;

export default function SocialProfileScreen() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<typeof profileTabs[number]['key']>('overview');
  const [selectedBadge, setSelectedBadge] = useState<PublicBadges['badges'][number] | null>(null);
  const [manageFriendship, setManageFriendship] = useState(false);
  const [favoriteFilter, setFavoriteFilter] = useState<'all' | 'movie' | 'series'>('all');
  const [favoriteLimit, setFavoriteLimit] = useState(12);
  const goBack = () => {
    if (router.canGoBack()) router.back();
    else router.replace('/social');
  };
  const { accountId } = useLocalSearchParams<{ accountId: string }>();
  const profile = useQuery({
    queryKey: ['social', 'friend-profile', accountId],
    queryFn: ({ signal }) => fetchFriendProfile(accountId, signal),
    enabled: Boolean(accountId),
  });
  const badges = useQuery({
    queryKey: ['gamification', 'public-badges', accountId],
    queryFn: ({ signal }) => fetchPublicGamificationBadges(accountId, signal),
    enabled: Boolean(profile.data),
  });
  const canSeeActivities = Boolean(profile.data?.watchHistoryShared || profile.data?.ratingsShared);
  const activities = useInfiniteQuery({
    queryKey: ['social', 'friend-activities', accountId],
    queryFn: ({ pageParam, signal }) => fetchFriendActivities(accountId, pageParam ?? undefined, signal),
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
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
      setManageFriendship(false);
      if (action === 'remove') {
        queryClient.setQueryData<FriendProfileData>(['social', 'friend-profile', accountId], (current) => current ? { ...current, relationshipStatus: 'removed', favorites: [], favoritesShared: false, watchHistoryShared: false, ratingsShared: false } : current);
      }
      await queryClient.cancelQueries({ queryKey: ['social', 'friend-activities'] });
      await queryClient.cancelQueries({ queryKey: ['social', 'feed'] });
      queryClient.removeQueries({ queryKey: ['social', 'friend-activities'] });
      queryClient.removeQueries({ queryKey: ['social', 'feed'] });
      await queryClient.invalidateQueries({ queryKey: ['social'] });
    },
    onError: () => { void profile.refetch(); },
  });
  useErrorToast(profile.error, 'Não foi possível carregar o perfil do amigo.');
  useErrorToast(badges.error, 'Não foi possível carregar os badges compartilhados.');
  useErrorToast(activities.error, 'Não foi possível carregar as atividades compartilhadas.');
  useErrorToast(relationshipAction.error, 'Não foi possível atualizar a amizade.');

  if (profile.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
      </View>
    );
  }

  if (profile.isError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>Perfil indisponível</Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void profile.refetch()} />
        <BackButton onPress={goBack} />
      </View>
    );
  }

  const allActivities = activities.data?.pages.flatMap((page) => page.items) ?? [];
  const favorites = profile.data.favorites.filter((favorite) => favoriteFilter === 'all' || favorite.contentType === favoriteFilter);
  const busy = relationshipAction.isPending || profile.isFetching;
  const status = profile.data.relationshipStatus;
  const refresh = async () => {
    await Promise.all([profile.refetch(), badges.refetch(), ...(canSeeActivities ? [activities.refetch()] : [])]);
  };

  return (
    <SafeAreaView style={styles.screen}>
    <FlatList
      data={canSeeActivities && (tab === 'overview' || tab === 'activities') ? allActivities.slice(0, tab === 'overview' ? 2 : undefined) : []}
      keyExtractor={(activity) => `${activity.activityType}-${activity.id}`}
      renderItem={({ item }) => <SocialActivityCard activity={item} />}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={profile.isRefetching} onRefresh={() => void refresh()} tintColor={colors.brand} />}
      onEndReached={() => { if (tab === 'activities' && activities.hasNextPage && !activities.isFetching && !activities.isError) void activities.fetchNextPage(); }}
      onEndReachedThreshold={0.4}
      ListHeaderComponent={
      <View style={styles.container}>
        <View style={styles.toolbar}>
          <BackButton onPress={goBack} />
          <Text style={styles.eyebrow}>PERFIL</Text>
          {status === 'accepted' ? <Pressable accessibilityRole="button" accessibilityLabel="Gerenciar amizade" onPress={() => setManageFriendship(true)} style={styles.iconButton}><MoreIcon width={dimensions.iconSize} height={dimensions.iconSize} /></Pressable> : null}
        </View>
        <View style={styles.profileHeader}>
        <View style={styles.avatarRing}>
        <UserAvatar name={profile.data.displayName} avatarKey={profile.data.avatarKey} uri={profile.data.avatarUrl} size={dimensions.minimumTouchTarget * 2} />
        </View>
        <View style={styles.identity}>
        <Text accessibilityRole="header" style={styles.title}>{profile.data.displayName}</Text>
        <Text style={styles.eyebrow}>{profile.data.isSelf ? 'Seu perfil' : status === 'accepted' ? 'Na sua rede' : 'Perfil Reelix'}</Text>
        </View>
        </View>
        {profile.data.biography ? <Text style={styles.body}>{profile.data.biography}</Text> : null}
        <View style={styles.statistics}>
          {profile.data.favoritesShared ? <Pressable accessibilityRole="button" accessibilityLabel="Ver favoritos compartilhados" onPress={() => setTab('favorites')} style={styles.stat}><Text style={styles.statValue}>{profile.data.favorites.length}{profile.data.favorites.length === 100 ? '+' : ''}</Text><Text style={styles.caption}>Favoritos compartilhados</Text></Pressable> : null}
          {badges.data?.isPublic ? <Pressable accessibilityRole="button" accessibilityLabel="Ver conquistas públicas" onPress={() => setTab('badges')} style={styles.stat}><Text style={styles.statValue}>{badges.data.badges.length}</Text><Text style={styles.caption}>Conquistas públicas</Text></Pressable> : null}
        </View>
        {profile.data.isSelf ? <PrimaryButton label="Editar meu perfil" variant="secondary" onPress={() => router.push('/profile')} /> : status === 'accepted' ? <Pressable accessibilityRole="button" accessibilityLabel="Amigos, gerenciar amizade" onPress={() => setManageFriendship(true)} style={styles.friendButton}><FriendsIcon width={dimensions.iconSize} height={dimensions.iconSize} /><Text style={styles.friendLabel}>Amigos</Text></Pressable> : status === 'pending' ? profile.data.requestSentByMe ? <>
          <PrimaryButton label="Solicitação enviada" disabled variant="secondary" onPress={() => {}} />
          <PrimaryButton label="Cancelar solicitação" disabled={busy} loading={relationshipAction.isPending} variant="quiet" onPress={() => relationshipAction.mutate('cancel')} />
        </> : <View style={styles.relationshipActions}><View style={styles.grow}><PrimaryButton label="Aceitar" disabled={busy} loading={relationshipAction.isPending} onPress={() => relationshipAction.mutate('accept')} /></View><View style={styles.grow}><PrimaryButton label="Recusar" disabled={busy} variant="secondary" onPress={() => relationshipAction.mutate('decline')} /></View></View> : <PrimaryButton label="Adicionar amigo" disabled={busy} loading={relationshipAction.isPending} onPress={() => relationshipAction.mutate('send')} />}
        <View accessibilityRole="tablist" style={styles.tabs}>
          {profileTabs.map(({ key, label, Icon }) => <Pressable key={key} accessibilityRole="tab" accessibilityLabel={label} accessibilityState={{ selected: tab === key }} onPress={() => setTab(key)} style={[styles.tab, tab === key && styles.tabSelected]}>
            <Icon width={dimensions.iconSize} height={dimensions.iconSize} />
            <Text style={[styles.tabLabel, tab === key && styles.tabLabelSelected]}>{label}</Text>
          </Pressable>)}
        </View>
        {tab === 'overview' || tab === 'badges' ? <>
        <Text style={styles.sectionTitle}>{tab === 'overview' ? 'Medalhas em destaque' : 'Conquistas públicas'}</Text>
        {badges.isPending ? (
          <ActivityIndicator color={colors.brand} />
        ) : badges.isError ? (
          <Pressable accessibilityRole="button" onPress={() => void badges.refetch()}>
            <Text style={styles.body}>Tentar carregar os badges novamente.</Text>
          </Pressable>
        ) : !badges.data.isPublic ? (
          <Text style={styles.body}>Este amigo não compartilha badges.</Text>
        ) : badges.data.badges.length === 0 ? (
          <Text style={styles.body}>Ainda não há conquistas públicas.</Text>
        ) : (
          <View style={styles.grid}>{badges.data.badges.slice(0, tab === 'overview' ? 4 : undefined).map((badge) => (
            <Pressable key={badge.code} accessibilityRole="button" accessibilityLabel={`Ver conquista ${badge.title}`} onPress={() => setSelectedBadge(badge)} style={styles.badge}>
              <AchievementMedal
                code={badge.code}
                title={badge.title}
                earned
                size={dimensions.avatarSize}
              />
            </Pressable>
          ))}</View>
        )}
        </> : null}
        {tab === 'overview' || tab === 'favorites' ? <>
        <Text style={styles.sectionTitle}>{tab === 'overview' ? 'Na estante de favoritos' : 'Favoritos compartilhados'}</Text>
        {tab === 'favorites' && profile.data.favoritesShared ? <View accessibilityRole="tablist" style={styles.filters}>
          {([{ key: 'all', label: 'Todos' }, { key: 'movie', label: 'Filmes' }, { key: 'series', label: 'Séries' }] as const).map(({ key, label }) => <Pressable key={key} accessibilityRole="tab" accessibilityState={{ selected: favoriteFilter === key }} onPress={() => setFavoriteFilter(key)} style={[styles.filter, favoriteFilter === key && styles.tabSelected]}><Text style={styles.tabLabel}>{label}</Text></Pressable>)}
        </View> : null}
        {profile.data.favorites.length === 0 ? (
          <Text style={styles.body}>{profile.data.favoritesShared ? 'Ainda não há favoritos compartilhados.' : 'Os favoritos não estão compartilhados com você.'}</Text>
        ) : (
          <View style={styles.grid}>{(tab === 'overview' ? profile.data.favorites.slice(0, 4) : favorites.slice(0, favoriteLimit)).map((favorite) => <FavoritePoster key={favorite.titleId} favorite={favorite} />)}</View>
        )}
        {tab === 'favorites' && favorites.length > favoriteLimit ? <PrimaryButton label="Mais favoritos" variant="secondary" onPress={() => setFavoriteLimit((limit) => limit + 12)} /> : null}
        {tab === 'favorites' && profile.data.favorites.length > 0 && favorites.length === 0 ? <Text style={styles.body}>Nenhum favorito deste tipo.</Text> : null}
        {tab === 'overview' && profile.data.favorites.length > 4 ? <PrimaryButton label="Ver todos os favoritos" variant="secondary" onPress={() => setTab('favorites')} /> : null}
        </> : null}
        {tab === 'overview' || tab === 'activities' ? <View style={styles.section}>
          <View style={styles.sectionHeading}><Text style={styles.sectionTitle}>Atividades recentes</Text>{tab === 'overview' && allActivities.length > 0 ? <Pressable accessibilityRole="button" accessibilityLabel="Ver todas as atividades compartilhadas" onPress={() => setTab('activities')} style={styles.iconButton}><ActivityIcon width={dimensions.iconSize} height={dimensions.iconSize} /></Pressable> : null}</View>
          {!canSeeActivities ? <Text style={styles.body}>As atividades não estão compartilhadas com você.</Text> : activities.isPending ? <ActivityIndicator accessibilityLabel="Carregando atividades" color={colors.brand} /> : activities.isError ? <PrimaryButton label="Tentar carregar atividades" variant="secondary" onPress={() => void activities.refetch()} /> : allActivities.length === 0 ? <Text style={styles.body}>Ainda não há atividades compartilhadas.</Text> : null}
        </View> : null}
      </View>
      }
      ListFooterComponent={tab === 'activities' && activities.hasNextPage ? <View style={styles.container}><PrimaryButton label="Mais atividades" variant="secondary" loading={activities.isFetchingNextPage} onPress={() => void activities.fetchNextPage()} /></View> : undefined}
    />
      <ModalDialog visible={selectedBadge !== null} title={selectedBadge?.title ?? 'Conquista'} onClose={() => setSelectedBadge(null)}>
        {selectedBadge ? <>
          <AchievementMedal code={selectedBadge.code} title={selectedBadge.title} earned size={dimensions.avatarSize * 2} />
          <Text style={styles.sectionTitle}>Critério de desbloqueio</Text>
          <Text style={styles.body}>{selectedBadge.description}</Text>
          <Text style={styles.badgeDate}>Conquistada em {new Date(selectedBadge.firstEarnedAt).toLocaleDateString('pt-BR')}</Text>
        </> : null}
      </ModalDialog>
      <ModalDialog visible={manageFriendship} title="Gerenciar amizade" onClose={() => setManageFriendship(false)} busy={relationshipAction.isPending}>
        <Text style={styles.body}>Ao remover {profile.data.displayName}, vocês deixarão de compartilhar atividades. O histórico de ambos será preservado.</Text>
        <PrimaryButton label="Remover amizade" loading={relationshipAction.isPending} disabled={profile.isFetching} onPress={() => relationshipAction.mutate('remove')} />
        <PrimaryButton label="Manter amizade" variant="secondary" disabled={relationshipAction.isPending} onPress={() => setManageFriendship(false)} />
      </ModalDialog>
    </SafeAreaView>
  );
}

function FavoritePoster({ favorite }: { favorite: FriendProfileData['favorites'][number] }) {
  const router = useRouter();
  const title = useQuery({
    queryKey: ['catalog', 'title', favorite.provider, favorite.contentType, favorite.externalId],
    queryFn: ({ signal }) => fetchCatalogTitleDetails(favorite.provider, favorite.contentType, favorite.externalId, signal),
    staleTime: 300_000,
  });
  useErrorToast(title.error, 'Não foi possível carregar um favorito compartilhado.');
  return <View style={styles.favorite}>
    {title.data ? <TitlePosterCard title={title.data}><Text style={styles.badgeDate}>{favorite.contentType === 'movie' ? 'Filme' : 'Série'}</Text></TitlePosterCard> : <View style={styles.posterPlaceholder}>
      {title.isPending ? <ActivityIndicator accessibilityLabel="Carregando favorito" color={colors.brand} /> : <>
        <PrimaryButton label="Tentar novamente" variant="quiet" onPress={() => void title.refetch()} />
        <Pressable accessibilityRole="button" accessibilityLabel="Abrir detalhes do favorito" onPress={() => router.push({ pathname: '/title/[provider]/[contentType]/[externalId]', params: { provider: favorite.provider, contentType: favorite.contentType, externalId: favorite.externalId } })} style={styles.tab}><Text style={styles.body}>Ver título</Text></Pressable>
      </>}
    </View>}
  </View>;
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background },
  content: {
    flexGrow: 1,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  container: {
    width: '100%',
    maxWidth: dimensions.drawerWidth * 2,
    alignSelf: 'center',
    gap: spacing.medium,
  },
  toolbar: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.small },
  iconButton: { width: dimensions.minimumTouchTarget, minHeight: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center' },
  profileHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.large },
  avatarRing: { borderWidth: 2, borderColor: colors.brand, borderRadius: radii.pill, padding: spacing.extraSmall },
  identity: { flex: 1, minWidth: 0, gap: spacing.small },
  eyebrow: { color: colors.textSecondary, fontSize: typography.caption, fontFamily: fontFamily.medium },
  statistics: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.large },
  stat: { minHeight: dimensions.minimumTouchTarget, flexBasis: dimensions.posterWidth, flexGrow: 1, gap: spacing.extraSmall },
  statValue: { color: colors.textPrimary, fontFamily: fontFamily.bold, fontSize: typography.section },
  caption: { color: colors.textSecondary, fontFamily: fontFamily.regular, fontSize: typography.caption },
  friendButton: { minHeight: dimensions.minimumTouchTarget, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: spacing.small, backgroundColor: colors.surface, borderRadius: radii.small, borderWidth: 1, borderColor: colors.border },
  friendLabel: { color: colors.textPrimary, fontFamily: fontFamily.semiBold, fontSize: typography.bodySmall },
  relationshipActions: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.small },
  grow: { flex: 1, minWidth: dimensions.posterWidth },
  section: { gap: spacing.medium },
  sectionHeading: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  filters: { flexDirection: 'row', gap: spacing.medium },
  filter: { minWidth: dimensions.minimumTouchTarget, minHeight: dimensions.minimumTouchTarget, justifyContent: 'center', borderBottomWidth: 2, borderBottomColor: colors.background, paddingHorizontal: spacing.small },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.medium,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.extraBold,
  },
  sectionTitle: {
    marginTop: spacing.small,
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  body: {
    color: colors.textSecondary,
    fontSize: typography.body,
    lineHeight: spacing.large,
    fontFamily: fontFamily.regular,
  },
  badge: {
    minWidth: dimensions.minimumTouchTarget,
    minHeight: dimensions.minimumTouchTarget,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    backgroundColor: colors.surface,
  },
  tabs: { flexDirection: 'row', borderBottomWidth: 1, borderBottomColor: colors.border },
  tab: { flex: 1, minHeight: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center', gap: spacing.small, paddingVertical: spacing.small, borderBottomWidth: 2, borderBottomColor: colors.background },
  tabSelected: { borderBottomColor: colors.brand },
  tabLabel: { fontSize: typography.caption, fontFamily: fontFamily.medium, color: colors.textSecondary, textAlign: 'center' },
  tabLabelSelected: { color: colors.brandSecondary, fontFamily: fontFamily.bold },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.medium },
  favorite: { width: dimensions.posterWidth, minHeight: dimensions.posterHeight + dimensions.minimumTouchTarget },
  posterPlaceholder: { width: dimensions.posterWidth, height: dimensions.posterHeight, borderRadius: radii.small, backgroundColor: colors.surface, justifyContent: 'center' },
  badgeTitle: {
    color: colors.brandSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  badgeDate: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  avatar: {
    width: dimensions.minimumTouchTarget * 2,
    height: dimensions.minimumTouchTarget * 2,
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
  },
  avatarFallback: {
    width: dimensions.minimumTouchTarget * 2,
    height: dimensions.minimumTouchTarget * 2,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
  },
  avatarInitial: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
});
