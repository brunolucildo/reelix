import { useQueries } from '@tanstack/react-query';
import { Image } from 'expo-image';
import { useMemo, useState } from 'react';
import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { useTrackedSeries } from '@/hooks/use-tracking';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchCatalogSeason, fetchCatalogSeasons, fetchCatalogTitleDetails } from '@/services/api-client';
import type { TrackedSeriesPage } from '@/services/tracking-api';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type CalendarFilter = 'all' | 'premiere' | 'final';

export function UpcomingCalendarSection() {
  const [filter, setFilter] = useState<CalendarFilter>('all');
  const tracked = useTrackedSeries(1, 12);
  const series = tracked.data?.items ?? [];
  const titleQueries = useQueries({
    queries: series.map((item) => ({
      queryKey: ['catalog', 'title', item.provider, 'series', item.externalId],
      queryFn: ({ signal }: { signal: AbortSignal }) =>
        fetchCatalogTitleDetails(item.provider!, 'series', item.externalId!, signal),
      enabled: Boolean(item.provider && item.externalId),
      staleTime: 300_000,
    })),
  });
  const seasonListQueries = useQueries({
    queries: series.map((item) => ({
      queryKey: ['calendar', 'seasons', item.seriesId],
      queryFn: ({ signal }: { signal: AbortSignal }) =>
        fetchCatalogSeasons(item.provider!, item.externalId!, signal),
      enabled: Boolean(item.provider && item.externalId),
      staleTime: 300_000,
    })),
  });
  const today = localDateKey(new Date());
  const seasonEntries = series.flatMap((item, index) => {
    if (!item.provider || !item.externalId) return [];
    const availableSeasons = seasonListQueries[index]?.data ?? [];
    const regularSeasons = availableSeasons.filter((season) => season.seasonNumber > 0);
    const latestSeasons = regularSeasons.slice(-2);
    const upcomingSeasons = availableSeasons.filter(
      (season) => season.releaseDate !== null && season.releaseDate >= today,
    );
    const selectedSeasons = new Map(
      [...latestSeasons, ...upcomingSeasons].map((season) => [season.seasonNumber, season]),
    );
    return Array.from(selectedSeasons.values())
      .sort((left, right) => left.seasonNumber - right.seasonNumber)
      .map((season) => ({ item, seasonNumber: season.seasonNumber }));
  });
  const seasonQueries = useQueries({
    queries: seasonEntries.map(({ item, seasonNumber }) => ({
      queryKey: ['calendar', 'season', item.seriesId, seasonNumber],
      queryFn: ({ signal }: { signal: AbortSignal }) =>
        fetchCatalogSeason(item.provider!, item.externalId!, seasonNumber, signal),
      staleTime: 300_000,
    })),
  });
  const titleBySeries = new Map(
    series.flatMap((item, index) => {
      const title = titleQueries[index]?.data;
      return title ? [[item.seriesId, title] as const] : [];
    }),
  );
  const episodes = useMemo(() => {
    const today = localDateKey(new Date());
    const deduplicated = new Map<string, {
      series: TrackedSeriesPage['items'][number];
      seasonNumber: number;
      episodeNumber: number | null;
      episodeTitle: string;
      releaseDate: string;
      episodeId: string;
      lastEpisodeNumber: number | null;
    }>();
    seasonQueries.forEach((result, index) => {
      if (!result.data) return;
      const entry = seasonEntries[index];
      if (!entry) return;
      const datedFutureEpisodes = result.data.episodes.filter(
        (episode) => episode.releaseDate !== null && episode.releaseDate >= today,
      );
      const lastEpisodeNumber = Math.max(
        0,
        ...result.data.episodes.map((episode) => episode.episodeNumber ?? 0),
      ) || null;
      for (const episode of datedFutureEpisodes) {
        deduplicated.set(episode.id, {
          series: entry.item,
          seasonNumber: result.data.seasonNumber,
          episodeNumber: episode.episodeNumber,
          episodeTitle: episode.title,
          releaseDate: episode.releaseDate!,
          episodeId: episode.id,
          lastEpisodeNumber,
        });
      }
    });
    return Array.from(deduplicated.values())
      .sort((left, right) => left.releaseDate.localeCompare(right.releaseDate));
  }, [seasonEntries, seasonQueries]);
  const visibleEpisodes = episodes.filter((episode) => {
    if (filter === 'premiere') return episode.episodeNumber === 1;
    if (filter === 'final') return episode.episodeNumber === episode.lastEpisodeNumber;
    return true;
  });
  const seasonError = seasonListQueries.find((result) => result.error)?.error ??
    seasonQueries.find((result) => result.error)?.error;
  const titleError = titleQueries.find((result) => result.error)?.error;

  useErrorToast(tracked.error, 'Não foi possível carregar suas séries para o calendário.');
  useErrorToast(seasonError, 'Não foi possível carregar alguns episódios do calendário.');
  useErrorToast(titleError, 'Não foi possível carregar algumas imagens do calendário.');

  const loading = tracked.isPending ||
    seasonListQueries.some(
      (result, index) =>
        Boolean(series[index]?.provider && series[index]?.externalId) && result.isPending,
    ) ||
    seasonQueries.some((result) => result.isPending);
  const failed = tracked.isError ||
    seasonListQueries.some((result) => result.isError) ||
    seasonQueries.some((result) => result.isError);

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.heading}>Calendário</Text>
      <View accessibilityRole="tablist" style={styles.filters}>
        {([
          ['all', 'Todos'],
          ['premiere', 'Estreia'],
          ['final', 'Final'],
        ] as const).map(([value, label]) => {
          const selected = filter === value;
          return (
            <Pressable
              key={value}
              accessibilityRole="tab"
              accessibilityState={{ selected }}
              onPress={() => setFilter(value)}
              style={[styles.filter, selected && styles.selectedFilter]}
            >
              <Text style={[styles.filterText, selected && styles.selectedFilterText]}>{label}</Text>
            </Pressable>
          );
        })}
      </View>
      {loading ? (
        <View style={styles.status}>
          <ActivityIndicator color={colors.brand} />
          <Text style={styles.statusText}>Buscando próximos episódios...</Text>
        </View>
      ) : failed ? (
        <View style={styles.status}>
          <Text style={styles.statusText}>Não foi possível carregar o calendário.</Text>
          <Pressable
            accessibilityRole="button"
            onPress={() => {
              if (tracked.isError) void tracked.refetch();
              seasonListQueries.forEach((result) => {
                if (result.isError) void result.refetch();
              });
              seasonQueries.forEach((result) => {
                if (result.isError) void result.refetch();
              });
            }}
            style={styles.retry}
          >
            <Text style={styles.retryText}>Tentar novamente</Text>
          </Pressable>
        </View>
      ) : visibleEpisodes.length === 0 ? (
        <Text style={styles.statusText}>
          {episodes.length === 0
            ? 'Não há próximos episódios com data confirmada nas suas séries.'
            : 'Não há episódios para este filtro.'}
        </Text>
      ) : (
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.cards}>
          {visibleEpisodes.map((episode) => {
            const title = titleBySeries.get(episode.series.seriesId);
            return (
              <CalendarEpisodeCard
                key={episode.episodeId}
                episode={episode}
                imageUrl={title?.backdropUrl ?? title?.posterUrl ?? null}
                title={title?.title ?? 'Série acompanhada'}
              />
            );
          })}
        </ScrollView>
      )}
    </View>
  );
}

function CalendarEpisodeCard({
  episode,
  imageUrl,
  title,
}: {
  episode: {
    series: TrackedSeriesPage['items'][number];
    seasonNumber: number;
    episodeNumber: number | null;
    episodeTitle: string;
    releaseDate: string;
    episodeId: string;
    lastEpisodeNumber: number | null;
  };
  imageUrl: string | null;
  title: string;
}) {
  const router = useRouter();
  const date = new Date(`${episode.releaseDate}T12:00:00`);
  const formattedDate = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' }).format(date);
  const daysUntil = Math.max(0, Math.ceil(
    (new Date(`${episode.releaseDate}T00:00:00`).getTime() -
      new Date(`${localDateKey(new Date())}T00:00:00`).getTime()) / 86_400_000,
  ));
  const releaseLabel = daysUntil === 0 ? 'Hoje' : daysUntil === 1 ? 'Amanhã' : `Em ${daysUntil} dias`;
  const episodeStatus = episode.episodeNumber === 1
    ? 'Estreia'
    : episode.episodeNumber === episode.lastEpisodeNumber
      ? 'Final'
      : null;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`Abrir ${title}, temporada ${episode.seasonNumber}, episódio ${episode.episodeNumber ?? ''}`}
      onPress={() => {
        if (episode.series.provider && episode.series.externalId) {
          router.push(`/title/${episode.series.provider}/series/${encodeURIComponent(episode.series.externalId)}`);
        }
      }}
      style={({ pressed }) => [styles.episodeCard, pressed && styles.pressed]}
    >
      {imageUrl ? <Image contentFit="cover" source={{ uri: imageUrl }} style={styles.episodeImage} /> : null}
      <View style={styles.episodeCopy}>
        <Text numberOfLines={1} style={styles.episodeSeries}>{title}</Text>
        {episodeStatus ? <Text style={styles.episodeStatus}>{episodeStatus}</Text> : null}
        <Text numberOfLines={1} style={styles.statusText}>
          T{episode.seasonNumber} · E{episode.episodeNumber ?? '?'} · {episode.episodeTitle}
        </Text>
        <Text style={styles.releaseDate}>{formattedDate} · {releaseLabel}</Text>
      </View>
    </Pressable>
  );
}

function localDateKey(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

const styles = StyleSheet.create({
  section: { gap: spacing.medium },
  heading: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  filters: { flexDirection: 'row', gap: spacing.small },
  filter: {
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.medium,
    borderWidth: 1,
    borderColor: colors.surfaceElevated,
    borderRadius: radii.pill,
  },
  selectedFilter: { backgroundColor: colors.brand, borderColor: colors.brand },
  filterText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  selectedFilterText: { color: colors.background },
  cards: { gap: spacing.medium, paddingEnd: spacing.medium },
  episodeCard: {
    width: dimensions.posterWidth * 1.65,
    overflow: 'hidden',
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  episodeImage: {
    width: '100%',
    height: dimensions.posterHeight * 0.6,
    backgroundColor: colors.surfaceElevated,
  },
  episodeCopy: { gap: spacing.extraSmall, padding: spacing.small },
  episodeSeries: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  releaseDate: {
    color: colors.brandSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
  },
  episodeStatus: {
    alignSelf: 'flex-start',
    paddingHorizontal: spacing.small,
    paddingVertical: spacing.extraSmall,
    overflow: 'hidden',
    borderRadius: radii.pill,
    backgroundColor: colors.brandSurface,
    color: colors.brandSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
  },
  status: {
    minHeight: dimensions.posterHeight * 0.6,
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
    paddingHorizontal: spacing.small,
  },
  retryText: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  pressed: { opacity: 0.75 },
});
