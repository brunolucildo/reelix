import { useRouter } from 'expo-router';
import { useState } from 'react';
import {
  ActivityIndicator,
  type LayoutChangeEvent,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import type { CatalogTitle } from '@/features/catalog/api/catalog-api';
import { SeasonTabs } from '@/features/catalog/components/season-tabs';
import { useCatalogSeason, useCatalogSeasons } from '@/features/catalog/hooks/use-catalog-seasons';
import { useSeasonSelection } from '@/features/catalog/hooks/use-season-selection';
import { EpisodeTrackingCard } from '@/features/tracking/components/episode-tracking-card';
import {
  useMarkEpisodeWatched,
  usePendingEpisodes,
  useRemoveWatchedEpisode,
  useSeasonProgress,
} from '@/features/tracking/hooks/use-tracking';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

type Props = {
  title: CatalogTitle;
  onLayout: (event: LayoutChangeEvent) => void;
};

/** Season tabs and the episode rail with per-episode watched and rating actions. */
export function SeriesSeasonsSection({ title, onLayout }: Props) {
  const router = useRouter();
  const [feedback, setFeedback] = useState<string | null>(null);
  const seasons = useCatalogSeasons(title.provider, title.externalId, true);
  const availableSeasons = seasons.data ?? [];
  const { seasonNumber, select } = useSeasonSelection(
    `${title.provider}:${title.externalId}`,
    availableSeasons,
  );
  const season = useCatalogSeason({
    provider: title.provider,
    externalSeriesId: title.externalId,
    seasonNumber,
    invalidMessage: 'Selecione uma temporada válida.',
    errorMessage: 'Não foi possível carregar os episódios desta temporada.',
  });
  const pendingEpisodes = usePendingEpisodes(season.data?.seriesId ?? '', seasonNumber ?? -1, title.provider);
  const progress = useSeasonProgress(season.data?.seriesId ?? '', seasonNumber ?? -1, title.provider);
  const markEpisode = useMarkEpisodeWatched();
  const removeEpisode = useRemoveWatchedEpisode();
  const todayUtc = new Date().toISOString().slice(0, 10);

  return (
    <View style={styles.seasons} onLayout={onLayout}>
      <Text accessibilityRole="header" style={styles.sectionTitle}>Temporadas e episódios</Text>
      {seasons.isPending ? <ActivityIndicator color={colors.brand} /> : null}
      {seasons.isError ? (
        <PrimaryButton
          label="Tentar carregar temporadas"
          variant="secondary"
          onPress={() => void seasons.refetch()}
        />
      ) : null}
      {!seasons.isPending && !seasons.isError && availableSeasons.length === 0 ? (
        <Text style={styles.secondaryText}>Nenhuma temporada disponível no catálogo.</Text>
      ) : null}
      {availableSeasons.length > 0 ? (
        <SeasonTabs seasons={availableSeasons} selected={seasonNumber} onSelect={select} />
      ) : null}
      {season.isPending && seasonNumber !== null ? (
        <View style={styles.loadingSeason}>
          <ActivityIndicator color={colors.brand} />
          <Text style={styles.secondaryText}>Carregando episódios...</Text>
        </View>
      ) : null}
      {season.isError ? (
        <PrimaryButton
          label="Tentar novamente"
          variant="secondary"
          onPress={() => void season.refetch()}
        />
      ) : null}
      {season.data ? (
        <View style={styles.episodeList}>
          {season.data.title ? <Text style={styles.secondaryText}>{season.data.title}</Text> : null}
          {progress.data ? (
            <Text style={styles.secondaryText}>
              {progress.data.watchedEpisodeCount} de {progress.data.availableEpisodeCount} episódios assistidos
            </Text>
          ) : null}
          {feedback ? (
            <Text accessibilityLiveRegion="polite" style={styles.feedback}>{feedback}</Text>
          ) : null}
          {season.data.episodes.length === 0 ? (
            <Text style={styles.secondaryText}>Nenhum episódio disponível nesta temporada.</Text>
          ) : (
            <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.episodeRail}>
              {season.data.episodes.map((episode) => {
                const isPendingEpisode = pendingEpisodes.data?.some(
                  (pending) => pending.episodeId === episode.id,
                );
                const isWatched = !pendingEpisodes.isPending && !pendingEpisodes.isError &&
                  isPendingEpisode === false;
                return (
                  <EpisodeTrackingCard
                    key={episode.id}
                    episode={episode}
                    isWatched={isWatched}
                    isUpcoming={episode.releaseDate !== null && episode.releaseDate > todayUtc}
                    isBusy={markEpisode.isPending || removeEpisode.isPending}
                    isLoadingStatus={pendingEpisodes.isPending}
                    isStatusReady={pendingEpisodes.isSuccess}
                    hasStatusError={pendingEpisodes.isError}
                    isMutating={(markEpisode.isPending && markEpisode.variables?.episodeId === episode.id) ||
                      (removeEpisode.isPending && removeEpisode.variables?.episodeId === episode.id)}
                    onRetryStatus={() => void pendingEpisodes.refetch()}
                    onToggle={() => {
                      setFeedback(null);
                      const variables = { seriesId: season.data.seriesId, episodeId: episode.id };
                      if (isPendingEpisode) {
                        markEpisode.mutate(variables, {
                          onSuccess: () => setFeedback('Episódio marcado como assistido.'),
                        });
                      } else {
                        removeEpisode.mutate(variables, {
                          onSuccess: () => setFeedback('Episódio removido do acompanhamento.'),
                        });
                      }
                    }}
                    onRate={() => router.push({
                      pathname: '/episode/[provider]/[externalSeriesId]/[seasonNumber]/[episodeId]',
                      params: {
                        provider: title.provider,
                        externalSeriesId: title.externalId,
                        seasonNumber: String(season.data.seasonNumber),
                        episodeId: episode.id,
                      },
                    })}
                  />
                );
              })}
            </ScrollView>
          )}
        </View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  seasons: { gap: spacing.small },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  loadingSeason: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
    paddingVertical: spacing.medium,
  },
  episodeList: { gap: spacing.small, marginTop: spacing.small },
  episodeRail: { gap: spacing.medium, alignItems: 'stretch', paddingBottom: spacing.extraLarge },
  feedback: {
    color: colors.success,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
});
