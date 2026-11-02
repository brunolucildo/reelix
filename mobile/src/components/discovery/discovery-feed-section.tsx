import { useInfiniteQuery } from '@tanstack/react-query';
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

import { useErrorToast } from '@/providers/toast-provider';
import { fetchCatalogDiscovery, type CatalogDiscoveryCategory } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import { TitlePosterCard } from './title-poster-card';

type Props = {
  title: string;
  type: 'movie' | 'series';
  category: CatalogDiscoveryCategory;
  genreIds?: number[];
};

export function DiscoveryFeedSection({ title, type, category, genreIds = [] }: Props) {
  const normalizedGenreIds = [...genreIds].sort((a, b) => a - b);
  const feed = useInfiniteQuery({
    queryKey: ['catalog', 'discovery', type, category, normalizedGenreIds],
    queryFn: ({ pageParam, signal }) =>
      fetchCatalogDiscovery(type, category, normalizedGenreIds, pageParam, signal),
    initialPageParam: 1,
    getNextPageParam: (lastPage) =>
      lastPage.page < lastPage.totalPages ? lastPage.page + 1 : undefined,
    staleTime: 300_000,
  });
  useErrorToast(feed.error, `Não foi possível carregar "${title}". Tente novamente.`);

  const seenIds = new Set<string>();
  const items = (feed.data?.pages.flatMap((page) => page.items) ?? []).filter((item) => {
    if (seenIds.has(item.id)) {
      return false;
    }

    seenIds.add(item.id);
    return true;
  });

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.heading}>{title}</Text>
      {feed.isPending ? (
        <View style={styles.status}>
          <ActivityIndicator color={colors.brand} />
          <Text style={styles.statusText}>Carregando títulos...</Text>
        </View>
      ) : feed.isError && items.length === 0 ? (
        <View style={styles.status}>
          <Text style={styles.statusText}>Esta seção não está disponível agora.</Text>
          <Pressable
            accessibilityRole="button"
            onPress={() => void feed.refetch()}
            style={styles.retry}
          >
            <Text style={styles.retryText}>Tentar novamente</Text>
          </Pressable>
        </View>
      ) : items.length === 0 ? (
        <Text style={styles.statusText}>Nenhum título disponível nesta seção.</Text>
      ) : (
        <FlatList
          data={items}
          keyExtractor={(item) => item.id}
          renderItem={({ item }) => <TitlePosterCard title={item} />}
          contentContainerStyle={styles.cards}
          horizontal
          showsHorizontalScrollIndicator={false}
          onEndReachedThreshold={0.5}
          onEndReached={() => {
            if (feed.hasNextPage && !feed.isFetching && !feed.isFetchNextPageError) {
              void feed.fetchNextPage({ cancelRefetch: false });
            }
          }}
          ListFooterComponent={
            feed.isFetchingNextPage ? (
              <View style={styles.moreLoading}>
                <ActivityIndicator color={colors.brand} />
              </View>
            ) : feed.isFetchNextPageError ? (
              <Pressable
                accessibilityRole="button"
                accessibilityLabel={`Tentar carregar mais títulos de ${title}`}
                onPress={() => void feed.fetchNextPage({ cancelRefetch: false })}
                style={styles.retry}
              >
                <Text style={styles.retryText}>Tentar novamente</Text>
              </Pressable>
            ) : undefined
          }
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    gap: spacing.medium,
  },
  heading: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  cards: {
    gap: spacing.medium,
    paddingEnd: spacing.medium,
  },
  status: {
    minHeight: dimensions.posterHeight,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  statusText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  retry: {
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.medium,
  },
  retryText: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  moreLoading: {
    width: dimensions.posterWidth,
    height: dimensions.posterHeight,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
