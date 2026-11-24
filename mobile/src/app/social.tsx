import { ActivityIndicator, FlatList, RefreshControl, StyleSheet, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { ActivityHeader } from '@/features/social/components/activity-header';
import { DiscoverSection } from '@/features/social/components/discover-section';
import { FriendsSection } from '@/features/social/components/friends-section';
import { RemoveFriendDialog } from '@/features/social/components/remove-friend-dialog';
import { RequestsSection } from '@/features/social/components/requests-section';
import { SocialActivityCard } from '@/features/social/components/social-activity-card';
import { SocialHeader } from '@/features/social/components/social-header';
import { socialStyles } from '@/features/social/components/social-styles';
import { useSocialScreen } from '@/features/social/hooks/use-social-screen';
import { colors, spacing } from '@/theme/tokens';

export default function SocialScreen() {
  const screen = useSocialScreen();
  const { activeTab, setActiveTab, queries, mutations, visibleActivities } = screen;
  const { feed, activeQuery, canLoad, isFeed, allIncoming, incoming, allActivities } = queries;

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
          <View style={socialStyles.container}>
            <SocialHeader
              activeTab={activeTab}
              onTabChange={setActiveTab}
              pendingRequests={allIncoming.length}
              hasMorePendingRequests={Boolean(incoming.hasNextPage)}
            />

            {canLoad && activeQuery.isPending ? <ActivityIndicator accessibilityLabel="Carregando" color={colors.brand} /> : null}
            {canLoad && activeQuery.isError ? <PrimaryButton label="Tentar novamente" onPress={() => void activeQuery.refetch()} variant="secondary" /> : null}

            {activeTab === 'discover' ? (
              <DiscoverSection
                searchText={screen.searchText}
                onSearchTextChange={screen.setSearchText}
                results={queries.search.data}
                mutations={mutations}
              />
            ) : null}
            {activeTab === 'requests' ? (
              <RequestsSection
                direction={screen.requestDirection}
                onDirectionChange={screen.setRequestDirection}
                queries={queries}
                mutations={mutations}
              />
            ) : null}
            {activeTab === 'friends' ? (
              <FriendsSection
                friends={queries.friends}
                allFriends={queries.allFriends}
                busy={mutations.busy}
                onRemove={(id, name) => screen.setFriendToRemove({ id, name })}
              />
            ) : null}
            {activeTab === 'feed' || activeTab === 'activities' ? (
              <ActivityHeader
                tab={activeTab}
                filter={screen.activityFilter}
                onFilterChange={screen.setActivityFilter}
                loadedCount={allActivities.length}
                visibleCount={visibleActivities.length}
                isEmptyState={!feed.isPending && !feed.isError}
                onFindPeople={() => setActiveTab('discover')}
              />
            ) : null}
          </View>
        }
        ListFooterComponent={isFeed && feed.hasNextPage ? <View style={socialStyles.container}><PrimaryButton label="Mais atividades" loading={feed.isFetchingNextPage} onPress={() => void feed.fetchNextPage()} variant="secondary" /></View> : undefined}
      />
      <RemoveFriendDialog
        friend={screen.friendToRemove}
        isRemoving={mutations.remove.isPending}
        onConfirm={(id) => mutations.remove.mutate(id, { onSuccess: () => screen.setFriendToRemove(null) })}
        onClose={() => screen.setFriendToRemove(null)}
      />
    </>
  );
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    backgroundColor: colors.background,
    paddingHorizontal: spacing.medium,
    paddingVertical: spacing.large,
  },
});
