import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { useStartWatching } from '@/features/home/hooks/use-start-watching';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import { TitlePosterCard } from '@/features/catalog/components/title-poster-card';

export function StartWatchingSection() {
  const {
    favorites, watchlist, tracked, references, details, items, episodeSuggestions,
    detailsError, suggestionError, isPending, hasError, isLoadingDetails,
  } = useStartWatching();

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.heading}>Começar a Assistir</Text>
      {isPending ? (
        <View style={styles.status}>
          <ActivityIndicator color={colors.brand} />
          <Text style={styles.statusText}>Buscando séries para começar...</Text>
        </View>
      ) : hasError ? (
        <View style={styles.status}>
          <Text style={styles.statusText}>Não foi possível abrir esta seção.</Text>
          <Pressable
            accessibilityRole="button"
            onPress={() => {
              if (favorites.isError) void favorites.refetch();
              if (watchlist.isError) void watchlist.refetch();
              if (tracked.isError) void tracked.refetch();
            }}
            style={styles.retry}
          >
            <Text style={styles.retryText}>Tentar novamente</Text>
          </Pressable>
        </View>
      ) : references.length === 0 ? (
        <Text style={styles.statusText}>
          Adicione séries aos favoritos ou à watchlist para começar a acompanhá-las.
        </Text>
      ) : (
        <>
          <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.cards}>
            {items.map((item, index) => {
              const suggestion = episodeSuggestions[index];
              const episode = suggestion?.data;
              return (
                <TitlePosterCard
                  key={`${item.provider}:${item.externalId}`}
                  title={item}
                  actionLabel="Abrir para começar"
                >
                  <Text numberOfLines={2} style={styles.episodeSuggestion}>
                    {episode
                      ? `T${episode.seasonNumber} · E${episode.episode.episodeNumber} — ${episode.episode.title}`
                      : suggestion?.isPending
                        ? 'Buscando primeiro episódio...'
                        : 'Primeiro episódio indisponível.'}
                  </Text>
                </TitlePosterCard>
              );
            })}
            {isLoadingDetails ? (
              <View style={styles.loadingMore}><ActivityIndicator color={colors.brand} /></View>
            ) : null}
          </ScrollView>
          {detailsError ? (
            <Pressable
              accessibilityRole="button"
              onPress={() => {
                for (const result of details) {
                  if (result.isError) void result.refetch();
                }
              }}
              style={styles.retry}
            >
              <Text style={styles.retryText}>Atualizar títulos</Text>
            </Pressable>
          ) : null}
          {suggestionError ? (
            <Pressable
              accessibilityRole="button"
              onPress={() => {
                for (const result of episodeSuggestions) {
                  if (result.isError) void result.refetch();
                }
              }}
              style={styles.retry}
            >
              <Text style={styles.retryText}>Atualizar sugestões</Text>
            </Pressable>
          ) : null}
        </>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  section: { gap: spacing.medium },
  heading: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  cards: { gap: spacing.medium, paddingEnd: spacing.medium },
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
    alignSelf: 'flex-start',
    paddingHorizontal: spacing.small,
  },
  retryText: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  episodeSuggestion: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.medium,
  },
  loadingMore: {
    width: dimensions.posterWidth,
    height: dimensions.posterHeight,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
