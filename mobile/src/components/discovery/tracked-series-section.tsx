import { useQuery } from '@tanstack/react-query';
import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { useSeriesProgress, useTrackedSeries } from '@/hooks/use-tracking';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchCatalogSeason, fetchCatalogTitleDetails } from '@/services/api-client';
import type { TrackedSeriesPage } from '@/services/tracking-api';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  onOpenTracking: () => void;
};

export function TrackedSeriesSection({ onOpenTracking }: Props) {
  const series = useTrackedSeries(1, 8);

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.heading}>Continuar Assistindo</Text>
      {series.isPending ? (
        <View accessibilityLiveRegion="polite" style={styles.status}>
          <ActivityIndicator color={colors.brand} />
          <Text style={styles.statusText}>Carregando seu acompanhamento...</Text>
        </View>
      ) : series.isError && !series.data ? (
        <View style={styles.status}>
          <Pressable
            accessibilityRole="button"
            onPress={() => void series.refetch()}
            style={styles.action}
          >
            <Text style={styles.actionText}>Tentar novamente</Text>
          </Pressable>
        </View>
      ) : series.data ? (
        <View style={styles.section}>
          {series.data.totalCount === 0 ? (
            <Text style={styles.statusText}>Registre episódios assistidos para acompanhar suas séries aqui.</Text>
          ) : (
            <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.cards}>
              {series.data.items.map((item) => <TrackedSeriesCard key={item.seriesId} item={item} />)}
            </ScrollView>
          )}
          <Pressable
            accessibilityRole="button"
            onPress={onOpenTracking}
            style={styles.action}
          >
            <Text style={styles.actionText}>
              {series.data.totalCount === 0 ? 'Começar acompanhamento' : 'Ver acompanhamento'}
            </Text>
          </Pressable>
          {series.isError ? (
            <Pressable
              accessibilityRole="button"
              onPress={() => void series.refetch()}
              style={styles.action}
            >
              <Text style={styles.actionText}>Atualizar resumo</Text>
            </Pressable>
          ) : null}
        </View>
      ) : null}
    </View>
  );
}

function TrackedSeriesCard({ item }: { item: TrackedSeriesPage['items'][number] }) {
  const router = useRouter();
  const title = useQuery({
    queryKey: ['catalog', 'title', item.provider, 'series', item.externalId],
    queryFn: ({ signal }) => fetchCatalogTitleDetails(item.provider!, 'series', item.externalId!, signal),
    enabled: Boolean(item.provider && item.externalId),
    staleTime: 300_000,
  });
  const season = useQuery({
    queryKey: ['tracking', 'home-season', item.seriesId, item.latestEpisodeId, item.knownSeasonNumbers],
    queryFn: async ({ signal }) => {
      for (const number of item.knownSeasonNumbers ?? []) {
        const details = await fetchCatalogSeason(item.provider!, item.externalId!, number, signal);
        const episode = details.episodes.find((candidate) => candidate.id === item.latestEpisodeId);
        if (episode) {
          return { number, episode };
        }
      }
      return null;
    },
    enabled: Boolean(item.provider && item.externalId && item.latestEpisodeId),
  });
  const progress = useSeriesProgress(item.seriesId, item.provider ?? 'tmdb');
  useErrorToast(title.error, 'Não foi possível carregar o pôster da série. Tente novamente.');
  useErrorToast(season.error, 'Não foi possível identificar a temporada. Tente novamente.');

  const retry = () => {
    if (title.isError) void title.refetch();
    if (season.isError) void season.refetch();
    if (progress.isError) void progress.refetch();
  };
  return (
    <View style={styles.card}>
      {title.data ? (
        <Pressable
          accessibilityLabel={`Continuar ${title.data.title}`}
          accessibilityRole="button"
          onPress={() => router.push(
            `/title/${title.data.provider}/series/${encodeURIComponent(title.data.externalId)}`,
          )}
          style={({ pressed }) => [styles.continueCard, pressed && styles.pressed]}
        >
          <Image
            contentFit="cover"
            source={title.data.backdropUrl || title.data.posterUrl
              ? { uri: title.data.backdropUrl ?? title.data.posterUrl ?? undefined }
              : undefined}
            style={styles.continueImage}
          />
          <View style={styles.imageScrim} />
          <View style={styles.continueContent}>
            <Text numberOfLines={2} style={styles.continueTitle}>{title.data.title}</Text>
            <Text style={styles.continueEpisode}>
              {season.data
                ? `T${season.data.number} · E${season.data.episode?.episodeNumber ?? '?'}`
                : `${item.watchedEpisodeCount} episódios assistidos`}
            </Text>
            {progress.data?.progressPercentage !== null && progress.data?.progressPercentage !== undefined ? (
              <View
                accessibilityRole="progressbar"
                accessibilityLabel={`Progresso da série: ${progress.data.progressPercentage}%`}
                accessibilityValue={{ min: 0, max: 100, now: progress.data.progressPercentage }}
                style={styles.progressTrack}
              >
                <View style={[styles.progressFill, { width: `${progress.data.progressPercentage}%` }]} />
              </View>
            ) : null}
            <Text style={styles.continueMeta}>
              {progress.data?.watchedEpisodeCount ?? item.watchedEpisodeCount} assistidos
              {progress.data?.pendingEpisodeCount != null
                ? ` · ${progress.data.pendingEpisodeCount} restantes`
                : ' · total disponível não informado'}
            </Text>
          </View>
        </Pressable>
      ) : title.isFetching ? (
        <View style={styles.status}><ActivityIndicator color={colors.brand} /></View>
      ) : (
        <Text style={styles.statusText}>Detalhes da série ainda não disponíveis. {item.watchedEpisodeCount} episódios assistidos.</Text>
      )}
      {title.isError || season.isError || progress.isError ? (
        <Pressable accessibilityRole="button" accessibilityLabel="Atualizar dados da série" onPress={retry} style={styles.action}>
          <Text style={styles.actionText}>Tentar novamente</Text>
        </Pressable>
      ) : null}
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
  status: {
    minHeight: dimensions.posterHeight,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  card: { width: dimensions.posterWidth, gap: spacing.small },
  continueCard: {
    width: dimensions.posterWidth,
    height: dimensions.posterHeight * 0.78,
    overflow: 'hidden',
    justifyContent: 'flex-end',
    borderRadius: radii.medium,
    backgroundColor: colors.surfaceElevated,
  },
  continueImage: StyleSheet.absoluteFill,
  imageScrim: { ...StyleSheet.absoluteFill, backgroundColor: 'rgba(8, 11, 15, 0.58)' },
  continueContent: { gap: spacing.extraSmall, padding: spacing.small },
  continueTitle: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  continueEpisode: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  continueMeta: {
    color: colors.textPrimary,
    fontSize: typography.caption,
    fontFamily: fontFamily.medium,
  },
  pressed: { opacity: 0.76 },
  cards: { gap: spacing.medium, paddingEnd: spacing.medium },
  progressTrack: { height: spacing.small, borderRadius: radii.small, backgroundColor: colors.surfaceElevated, overflow: 'hidden' },
  progressFill: { height: '100%', backgroundColor: colors.brand, borderRadius: radii.small },
  statusText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  action: {
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.small,
  },
  actionText: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
});
