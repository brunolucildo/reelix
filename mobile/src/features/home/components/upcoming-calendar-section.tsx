import { Image } from 'expo-image';
import { useState } from 'react';
import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import {
  type CalendarEpisode,
  type CalendarFilter,
  useUpcomingCalendar,
} from '@/features/home/hooks/use-upcoming-calendar';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import { formatDateMedium, localDateKey } from '@/lib/format';

export function UpcomingCalendarSection() {
  const [filter, setFilter] = useState<CalendarFilter>('all');
  const { episodes, visibleEpisodes, titleBySeries, loading, failed, retry } = useUpcomingCalendar(filter);

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
            onPress={retry}
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
  episode: CalendarEpisode;
  imageUrl: string | null;
  title: string;
}) {
  const router = useRouter();
  const date = new Date(`${episode.releaseDate}T12:00:00`);
  const formattedDate = formatDateMedium(date);
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
