import { useQuery, useQueries } from '@tanstack/react-query';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { useErrorToast } from '@/providers/toast-provider';
import { fetchFavorites, fetchTitleDetails } from '@/services/lists-api';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import { TitlePosterCard } from './title-poster-card';

type Props = {
  type: 'movie' | 'series';
  title: string;
};

export function FavoriteTitleSection({ type, title }: Props) {
  const favorites = useQuery({
    queryKey: ['lists', 'favorites', 'home', type],
    queryFn: () => fetchFavorites(1, 100),
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

  useErrorToast(favorites.error, 'Não foi possível carregar seus favoritos. Tente novamente.');
  useErrorToast(detailError, 'Não foi possível carregar alguns títulos favoritos. Tente novamente.');

  const items = titleDetails.flatMap((result) => result.data ? [result.data] : []);
  const isLoadingDetails = titleDetails.some((result) => result.isPending);

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.heading}>{title}</Text>
      {favorites.isPending ? (
        <View style={styles.status}>
          <ActivityIndicator color={colors.brand} />
          <Text style={styles.statusText}>Carregando favoritos...</Text>
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
        <Text style={styles.statusText}>Você ainda não adicionou favoritos nesta categoria.</Text>
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
