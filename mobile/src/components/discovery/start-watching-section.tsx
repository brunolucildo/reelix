import { useQueries, useQuery } from '@tanstack/react-query';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { useTrackedSeries } from '@/hooks/use-tracking';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchCatalogSeason, fetchCatalogSeasons } from '@/services/api-client';
import {
  fetchFavorites,
  fetchTitleDetails,
  fetchWatchlist,
  type TitleReference,
} from '@/services/lists-api';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import { TitlePosterCard } from './title-poster-card';

export function StartWatchingSection() {
  const favorites = useQuery({
    queryKey: ['lists', 'favorites', 'start-watching'],
    queryFn: () => fetchFavorites(1, 100),
    staleTime: 60_000,
  });
  const watchlist = useQuery({
    queryKey: ['lists', 'watchlist', 'start-watching'],
    queryFn: () => fetchWatchlist(1, false),
    staleTime: 60_000,
  });
  const tracked = useTrackedSeries(1, 100);
  const trackedKeys = new Set((tracked.data?.items ?? [])
    .filter((series) => series.provider && series.externalId)
    .map((series) => `${series.provider}:${series.externalId}`));
  const references = mergeSeriesReferences([
    ...(favorites.data?.items ?? []),
    ...(watchlist.data?.items ?? []),
  ]).filter((reference) => !trackedKeys.has(`${reference.provider}:${reference.externalId}`))
    .slice(0, 8);
  const details = useQueries({
    queries: references.map((reference) => ({
      queryKey: ['catalog', 'title', reference.provider, 'series', reference.externalId],
      queryFn: ({ signal }: { signal: AbortSignal }) => fetchTitleDetails(reference, signal),
      staleTime: 300_000,
    })),
  });
  const items = details.flatMap((result) => result.data ? [result.data] : []);
  const episodeSuggestions = useQueries({
    queries: items.map((item) => ({
      queryKey: ['catalog', 'first-episode', item.provider, item.externalId],
      queryFn: async ({ signal }: { signal: AbortSignal }) => {
        const seasons = await fetchCatalogSeasons(item.provider, item.externalId, signal);
        const firstSeason = seasons.find((season) => season.seasonNumber > 0) ?? seasons[0];
        if (!firstSeason) return null;

        const season = await fetchCatalogSeason(
          item.provider,
          item.externalId,
          firstSeason.seasonNumber,
          signal,
        );
        const firstEpisode = [...season.episodes]
          .filter((episode) => episode.episodeNumber !== null)
          .sort((left, right) => (left.episodeNumber ?? 0) - (right.episodeNumber ?? 0))[0];
        return firstEpisode
          ? { seasonNumber: season.seasonNumber, episode: firstEpisode }
          : null;
      },
      staleTime: 300_000,
    })),
  });

  useErrorToast(favorites.error, 'Não foi possível carregar os favoritos para começar a assistir.');
  useErrorToast(watchlist.error, 'Não foi possível carregar a watchlist para começar a assistir.');
  useErrorToast(tracked.error, 'Não foi possível verificar as séries já acompanhadas.');
  const detailsError = details.find((result) => result.error)?.error;
  useErrorToast(detailsError, 'Não foi possível carregar alguns títulos para começar a assistir.');
  const suggestionError = episodeSuggestions.find((result) => result.error)?.error;
  useErrorToast(suggestionError, 'Não foi possível carregar a sugestão de primeiro episódio.');

  const isPending = favorites.isPending || watchlist.isPending || tracked.isPending;
  const hasError = favorites.isError || watchlist.isError || tracked.isError;
  const isLoadingDetails = details.some((result) => result.isPending);

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

function mergeSeriesReferences(references: TitleReference[]) {
  const unique = new Map<string, TitleReference>();
  for (const reference of references) {
    if (reference.contentType !== 'series') continue;
    unique.set(`${reference.provider}:${reference.externalId}`, reference);
  }
  return Array.from(unique.values());
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
