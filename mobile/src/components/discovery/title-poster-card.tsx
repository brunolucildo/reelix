import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import type { ReactNode } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import type { CatalogTitle } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type PosterTitle = Pick<
  CatalogTitle,
  'id' | 'provider' | 'externalId' | 'contentType' | 'title' | 'releaseDate' | 'posterUrl'
> & {
  genres?: string[];
};

export function TitlePosterCard({
  title,
  children,
  actionLabel,
}: {
  title: PosterTitle;
  children?: ReactNode;
  actionLabel?: string;
}) {
  const router = useRouter();
  const releaseYear = title.releaseDate?.slice(0, 4);

  return (
    <Pressable
      accessibilityLabel={`${actionLabel ?? 'Abrir detalhes de'} ${title.title}`}
      accessibilityRole="button"
      onPress={() => router.push(
        `/title/${title.provider}/${title.contentType}/${encodeURIComponent(title.externalId)}`,
      )}
      style={({ pressed }) => [styles.card, pressed && styles.pressed]}
    >
      {title.posterUrl ? (
        <Image
          contentFit="cover"
          source={{ uri: title.posterUrl }}
          style={styles.poster}
          transition={150}
        />
      ) : (
        <View style={[styles.poster, styles.missingPoster]}>
          <Text style={styles.missingText}>Pôster indisponível</Text>
        </View>
      )}
      <Text numberOfLines={2} style={styles.title}>{title.title}</Text>
      {releaseYear ? <Text style={styles.year}>{releaseYear}</Text> : null}
      {actionLabel ? <Text style={styles.action}>{actionLabel}</Text> : null}
      {children}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  card: {
    width: dimensions.posterWidth,
    gap: spacing.small,
  },
  pressed: {
    opacity: 0.72,
  },
  poster: {
    width: dimensions.posterWidth,
    height: dimensions.posterHeight,
    borderRadius: radii.medium,
    backgroundColor: colors.surfaceElevated,
  },
  missingPoster: {
    alignItems: 'center',
    justifyContent: 'center',
    padding: spacing.small,
  },
  missingText: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.medium,
    textAlign: 'center',
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  year: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  action: {
    color: colors.brandSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
  },
});
