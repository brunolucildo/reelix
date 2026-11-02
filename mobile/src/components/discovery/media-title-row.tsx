import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import type { ReactNode } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import type { CatalogTitle } from '@/services/api-client';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export function MediaTitleRow({
  title,
  children,
}: {
  title: CatalogTitle;
  children?: ReactNode;
}) {
  const router = useRouter();

  return (
    <View style={styles.card}>
      <Pressable
        accessibilityLabel={`Abrir detalhes de ${title.title}`}
        accessibilityRole="button"
        onPress={() => router.push(
          `/title/${title.provider}/${title.contentType}/${encodeURIComponent(title.externalId)}`,
        )}
        style={({ pressed }) => [styles.titleRow, pressed && styles.pressed]}
      >
        {title.posterUrl ? (
          <Image contentFit="cover" source={{ uri: title.posterUrl }} style={styles.poster} />
        ) : (
          <View style={[styles.poster, styles.missingPoster]}>
            <Text style={styles.muted}>Sem pôster</Text>
          </View>
        )}
        <View style={styles.titleCopy}>
          <Text numberOfLines={2} style={styles.title}>{title.title}</Text>
          <Text style={styles.muted}>
            {title.contentType === 'movie' ? 'Filme' : 'Série'}
            {title.releaseDate ? ` · ${title.releaseDate.slice(0, 4)}` : ''}
          </Text>
          {title.genres.length > 0 ? (
            <Text numberOfLines={1} style={styles.muted}>{title.genres.join(' · ')}</Text>
          ) : null}
        </View>
      </Pressable>
      {children ? <View style={styles.actions}>{children}</View> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.small,
    padding: spacing.small,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  titleRow: {
    minHeight: 96,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.medium,
  },
  poster: {
    width: 64,
    height: 96,
    borderRadius: radii.small,
    backgroundColor: colors.surfaceElevated,
  },
  missingPoster: { alignItems: 'center', justifyContent: 'center', padding: spacing.extraSmall },
  titleCopy: { flex: 1, gap: spacing.extraSmall },
  title: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  muted: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  actions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.small,
  },
  pressed: { opacity: 0.75 },
});
