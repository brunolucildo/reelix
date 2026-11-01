import { useInfiniteQuery } from '@tanstack/react-query';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

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

  const items = feed.data?.pages.flatMap((page) => page.items) ?? [];
  const hasMore = feed.hasNextPage;

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
        <>
          <ScrollView
            contentContainerStyle={styles.cards}
            horizontal
            showsHorizontalScrollIndicator={false}
          >
            {items.map((item) => <TitlePosterCard key={item.id} title={item} />)}
            {feed.isFetchingNextPage ? (
              <View style={styles.moreLoading}>
                <ActivityIndicator color={colors.brand} />
              </View>
            ) : null}
          </ScrollView>
          {hasMore ? (
            <Pressable
              accessibilityRole="button"
              onPress={() => void feed.fetchNextPage()}
              style={styles.moreButton}
            >
              <Text style={styles.retryText}>Mostrar mais</Text>
            </Pressable>
          ) : null}
        </>
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
  moreButton: {
    minHeight: dimensions.minimumTouchTarget,
    alignItems: 'center',
    justifyContent: 'center',
    alignSelf: 'flex-start',
    paddingHorizontal: spacing.medium,
    borderRadius: radii.pill,
    backgroundColor: colors.surface,
  },
  moreLoading: {
    width: dimensions.posterWidth,
    height: dimensions.posterHeight,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
