import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { IconAction } from '@/components/ui/icon-action';
import ClockIcon from '@/assets/brand/navigation-icons/clock.svg';
import { useHomeCollection } from '@/features/lists/hooks/use-home-collection';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import { TitlePosterCard } from '@/features/catalog/components/title-poster-card';

type Props = {
  type: 'movie' | 'series';
  title: string;
  collection?: 'favorites' | 'watchlist';
};

export function FavoriteTitleSection({ type, title, collection = 'favorites' }: Props) {
  const router = useRouter();
  const isWatchlist = collection === 'watchlist';
  const { favorites, references, titleDetails, detailError, items, isLoadingDetails } =
    useHomeCollection(collection, type);

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
