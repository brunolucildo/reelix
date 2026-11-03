import { useQuery, useQueries } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { useErrorToast } from '@/providers/toast-provider';
import { IconAction } from '@/components/icon-action';
import ClockIcon from '../../../assets/brand/navigation-icons/clock.svg';
import { fetchFavorites, fetchTitleDetails, fetchWatchlist } from '@/services/lists-api';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import { TitlePosterCard } from './title-poster-card';

type Props = {
  type: 'movie' | 'series';
  title: string;
  collection?: 'favorites' | 'watchlist';
};

export function FavoriteTitleSection({ type, title, collection = 'favorites' }: Props) {
  const router = useRouter();
  const isWatchlist = collection === 'watchlist';
  const favorites = useQuery({
    queryKey: ['lists', collection, 'home', type],
    queryFn: async () => {
      if (!isWatchlist) return fetchFavorites(1, 100);
      const firstPage = await fetchWatchlist(1, false);
      const items = [...firstPage.items];
      let page = 1;
      while (items.filter((item) => item.contentType === type).length < 8 && page * firstPage.pageSize < firstPage.totalCount) {
        page += 1;
        const nextPage = await fetchWatchlist(page, false);
        items.push(...nextPage.items);
      }
      return { ...firstPage, items };
    },
    staleTime: 60_000,
  });
  const references = (favorites.data?.items ?? [])
    .filter((reference) => reference.contentType === type)
    .slice(0, 8);
  const titleDetails = useQueries({
    queries: references.map((reference) => ({
      queryKey: ['catalog', 'title', reference.provider, reference.contentType, reference.externalId],
      queryFn: ({ signal }: { signal: AbortSignal }) => fetchTitleDetails(reference, signal),
      staleTime: 300_000,
    })),
  });
  const detailError = titleDetails.find((result) => result.error)?.error ?? null;

  useErrorToast(favorites.error, isWatchlist ? 'Não foi possível carregar Quero assistir. Tente novamente.' : 'Não foi possível carregar seus favoritos. Tente novamente.');
  useErrorToast(detailError, isWatchlist ? 'Não foi possível carregar alguns títulos de Quero assistir. Tente novamente.' : 'Não foi possível carregar alguns títulos favoritos. Tente novamente.');

  const items = titleDetails.flatMap((result) => result.data ? [result.data] : []);
  const isLoadingDetails = titleDetails.some((result) => result.isPending);

  return (
    <View style={styles.section}>
      <View style={styles.headingRow}>
        <Text accessibilityRole="header" style={styles.heading}>{title}</Text>
        {isWatchlist ? <IconAction label="Ver toda a watchlist" onPress={() => router.push({ pathname: '/lists', params: { section: 'watchlist' } })}><ClockIcon width={dimensions.iconSize} height={dimensions.iconSize} /></IconAction> : null}
      </View>
      {favorites.isPending ? (
        <View style={styles.status}>
          <ActivityIndicator color={colors.brand} />
          <Text style={styles.statusText}>{isWatchlist ? 'Carregando Quero assistir...' : 'Carregando favoritos...'}</Text>
        </View>
      ) : favorites.isError ? (
        <View style={styles.status}>
          <Text style={styles.statusText}>Não foi possível abrir esta seção.</Text>
          <Pressable
            accessibilityRole="button"
            onPress={() => void favorites.refetch()}
            style={styles.retry}
          >
            <Text style={styles.retryText}>Tentar novamente</Text>
          </Pressable>
        </View>
      ) : references.length === 0 ? (
        <Text style={styles.statusText}>{isWatchlist ? 'Você ainda não adicionou filmes a Quero assistir.' : 'Você ainda não adicionou favoritos nesta categoria.'}</Text>
      ) : (
        <>
          <ScrollView
            contentContainerStyle={styles.cards}
            horizontal
            showsHorizontalScrollIndicator={false}
          >
            {items.map((item) => <TitlePosterCard key={item.id} title={item} />)}
            {isLoadingDetails ? (
              <View style={styles.loadingMore}>
                <ActivityIndicator color={colors.brand} />
              </View>
            ) : null}
          </ScrollView>
          {detailError ? (
            <Pressable
              accessibilityRole="button"
              onPress={() => {
                for (const result of titleDetails) {
                  if (result.isError) {
                    void result.refetch();
                  }
                }
              }}
              style={styles.retry}
            >
              <Text style={styles.retryText}>Tentar novamente</Text>
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
  headingRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.small, zIndex: 1 },
  heading: {
    flexShrink: 1,
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
    alignItems: 'center',
    justifyContent: 'center',
    alignSelf: 'flex-start',
    paddingHorizontal: spacing.medium,
  },
  retryText: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  loadingMore: {
    width: dimensions.posterWidth,
    height: dimensions.posterHeight,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
