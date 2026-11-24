import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, RefreshControl, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { PrimaryButton } from '@/components/ui/primary-button';
import { AchievementMedal } from '@/features/gamification/components/achievement-medal';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { BackButton } from '@/components/navigation/back-button';
import type { PublicBadges } from '@/features/gamification/api/gamification-api';
import { FavoritePoster } from '@/features/social/components/favorite-poster';
import { FriendProfileHeader } from '@/features/social/components/friend-profile-header';
import { SocialActivityCard } from '@/features/social/components/social-activity-card';
import { type FriendProfileTab, useFriendProfile } from '@/features/social/hooks/use-friend-profile';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import { formatDate } from '@/lib/format';
import OverviewIcon from '@/assets/brand/navigation-icons/profile.svg';
import ActivityIcon from '@/assets/brand/navigation-icons/clock.svg';
import FavoritesIcon from '@/assets/brand/navigation-icons/favorites.svg';
import AchievementsIcon from '@/assets/brand/navigation-icons/achievements.svg';

const profileTabs = [
  { key: 'overview', label: 'Visão geral', Icon: OverviewIcon },
  { key: 'activities', label: 'Atividades', Icon: ActivityIcon },
  { key: 'favorites', label: 'Favoritos', Icon: FavoritesIcon },
  { key: 'badges', label: 'Conquistas', Icon: AchievementsIcon },
] as const;

export default function SocialProfileScreen() {
  const router = useRouter();
  const [tab, setTab] = useState<FriendProfileTab>('overview');
  const [selectedBadge, setSelectedBadge] = useState<PublicBadges['badges'][number] | null>(null);
  const [manageFriendship, setManageFriendship] = useState(false);
  const [favoriteFilter, setFavoriteFilter] = useState<'all' | 'movie' | 'series'>('all');
  const [favoriteLimit, setFavoriteLimit] = useState(12);
  const goBack = () => {
    if (router.canGoBack()) router.back();
    else router.replace('/social');
  };
  const { accountId } = useLocalSearchParams<{ accountId: string }>();
  const { profile, badges, activities, canSeeActivities, relationshipAction, allActivities } =
    useFriendProfile(accountId, tab, () => setManageFriendship(false));

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

  const favorites = profile.data.favorites.filter((favorite) => favoriteFilter === 'all' || favorite.contentType === favoriteFilter);
  const busy = relationshipAction.isPending || profile.isFetching;
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
        <FriendProfileHeader
          profile={profile.data}
          badges={badges.data}
          busy={busy}
          isActing={relationshipAction.isPending}
          onAction={(action) => relationshipAction.mutate(action)}
          onManage={() => setManageFriendship(true)}
          onBack={goBack}
          onSelectTab={setTab}
        />
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
          <Text style={styles.badgeDate}>Conquistada em {formatDate(selectedBadge.firstEarnedAt)}</Text>
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
  iconButton: { width: dimensions.minimumTouchTarget, minHeight: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center' },
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
  badgeDate: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
});
