import { Image } from 'expo-image';
import { StyleSheet, Text, View } from 'react-native';

import { IconAction } from '@/components/ui/icon-action';
import type { CatalogSeason } from '@/features/catalog/api/catalog-api';
import { formatCatalogDate } from '@/lib/format';
import CheckIcon from '@/assets/brand/navigation-icons/check.svg';
import EyeIcon from '@/assets/brand/navigation-icons/eye.svg';
import RatingIcon from '@/assets/brand/navigation-icons/rating.svg';
import SeriesIcon from '@/assets/brand/navigation-icons/series.svg';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  episode: CatalogSeason['episodes'][number];
  isWatched: boolean;
  isUpcoming: boolean;
  /** Any episode mutation in flight: locks every card. */
  isBusy: boolean;
  isLoadingStatus: boolean;
  isStatusReady: boolean;
  hasStatusError: boolean;
  /** A mutation for this very episode is in flight. */
  isMutating: boolean;
  onRetryStatus: () => void;
  onToggle: () => void;
  onRate: () => void;
};

export function EpisodeTrackingCard({
  episode,
  isWatched,
  isUpcoming,
  isBusy,
  isLoadingStatus,
  isStatusReady,
  hasStatusError,
  isMutating,
  onRetryStatus,
  onToggle,
  onRate,
}: Props) {
  return (
    <View style={styles.card}>
      {episode.stillUrl ? <Image accessibilityLabel={`Cena de ${episode.title}`} source={{ uri: episode.stillUrl }} contentFit="cover" style={styles.image} /> : <View style={[styles.image, styles.imageFallback]}><SeriesIcon width={dimensions.avatarSize} height={dimensions.avatarSize} /></View>}
      <View style={styles.copy}>
        <Text numberOfLines={2} style={styles.title}>
          {episode.episodeNumber === null
            ? episode.title
            : `E${episode.episodeNumber}: ${episode.title}`}
        </Text>
        {episode.releaseDate ? (
          <Text style={styles.secondaryText}>
            Lançamento: {formatCatalogDate(episode.releaseDate)}
          </Text>
        ) : null}
        {episode.overview ? (
          <Text numberOfLines={3} style={styles.secondaryText}>{episode.overview}</Text>
        ) : null}
      </View>
      {isUpcoming ? <Text style={styles.secondaryText}>Ainda não lançado</Text> : null}
      <View style={styles.actions}>
        {hasStatusError ? (
          <IconAction label={`Tentar carregar estado de ${episode.title}`} onPress={onRetryStatus}>
            <EyeIcon width={dimensions.iconSize} height={dimensions.iconSize} />
          </IconAction>
        ) : (
          <IconAction
            label={`${isWatched ? 'Desmarcar como assistido' : 'Marcar como assistido'}: ${episode.title}`}
            selected={isWatched}
            loading={isLoadingStatus || isMutating}
            disabled={!isStatusReady || isUpcoming || isBusy}
            onPress={onToggle}
          >
            {isWatched
              ? <CheckIcon width={dimensions.iconSize} height={dimensions.iconSize} />
              : <EyeIcon width={dimensions.iconSize} height={dimensions.iconSize} />}
          </IconAction>
        )}
        <IconAction
          label={isWatched ? `Avaliar ${episode.title}` : `Assista ao episódio para avaliar: ${episode.title}`}
          disabled={!isWatched || isBusy}
          onPress={onRate}
        >
          <RatingIcon width={dimensions.iconSize} height={dimensions.iconSize} />
        </IconAction>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    width: dimensions.posterWidth * 2,
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  image: { width: '100%', aspectRatio: 16 / 9, borderRadius: radii.small, backgroundColor: colors.surfaceElevated },
  imageFallback: { alignItems: 'center', justifyContent: 'center' },
  actions: { flexDirection: 'row', gap: spacing.small, marginTop: 'auto', zIndex: 1 },
  copy: { gap: spacing.extraSmall },
  title: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
});
