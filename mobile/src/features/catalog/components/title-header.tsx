import { Image } from 'expo-image';
import { LinearGradient } from 'expo-linear-gradient';
import type { ReactNode } from 'react';
import { StyleSheet, Text, View } from 'react-native';

import { BackButton } from '@/components/navigation/back-button';
import type { CatalogTitle } from '@/features/catalog/api/catalog-api';
import { formatDateLong } from '@/lib/format';
import { colors, dimensions, fontFamily, spacing, typography } from '@/theme/tokens';

type Props = {
  title: CatalogTitle;
  /** Action row (collection and watched buttons), composed by the route from other features. */
  actions: ReactNode;
};

/** Hero image, names, metadata, action row, genres and synopsis of a catalog title. */
export function TitleHeader({ title, actions }: Props) {
  const releaseDate = title.releaseDate
    ? formatDateLong(new Date(`${title.releaseDate}T00:00:00`))
    : null;

  return (
    <>
      {title.backdropUrl || title.posterUrl ? <View style={styles.hero}>
        <Image accessibilityLabel={`Imagem de ${title.title}`} contentFit="cover" source={{ uri: title.backdropUrl ?? title.posterUrl! }} style={StyleSheet.absoluteFill} transition={150} />
        <LinearGradient pointerEvents="none" colors={[colors.backgroundTransparent, colors.backgroundTransparent, colors.background]} locations={[0, 0.35, 1]} style={StyleSheet.absoluteFill} />
        <View style={styles.heroNavigation}><BackButton /></View>
      </View> : <BackButton />}
      <Text accessibilityRole="header" style={styles.title}>{title.title}</Text>
      {title.originalTitle && title.originalTitle !== title.title ? (
        <Text style={styles.secondaryText}>{title.originalTitle}</Text>
      ) : null}
      <View style={styles.metadata}>
        <Text style={styles.metadataText}>
          {title.contentType === 'movie' ? 'Filme' : 'Série'}
        </Text>
        {releaseDate ? <Text style={styles.metadataText}>{releaseDate}</Text> : null}
        {title.runtimeMinutes ? (
          <Text style={styles.metadataText}>{title.runtimeMinutes} min</Text>
        ) : null}
      </View>
      <View style={styles.actions}>{actions}</View>
      {title.genres.length > 0 ? (
        <Text style={styles.secondaryText}>{title.genres.join(' · ')}</Text>
      ) : null}
      {title.overview ? (
        <View style={styles.overview}>
          <Text style={styles.sectionTitle}>Sinopse</Text>
          <Text style={styles.body}>{title.overview}</Text>
        </View>
      ) : null}
    </>
  );
}

const styles = StyleSheet.create({
  hero: { aspectRatio: 16 / 10, maxHeight: dimensions.posterHeight * 3, marginHorizontal: -spacing.medium, marginTop: -spacing.large, backgroundColor: colors.background },
  heroNavigation: { position: 'absolute', top: spacing.large, left: spacing.medium, zIndex: 2 },
  actions: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.small, zIndex: 1 },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.bold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  metadata: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.small,
  },
  metadataText: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.medium,
  },
  overview: {
    gap: spacing.small,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  body: {
    color: colors.textSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
    lineHeight: typography.body * 1.6,
  },
});
