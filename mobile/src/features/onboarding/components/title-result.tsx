import { Image } from 'expo-image';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import type { CatalogTitle } from '@/features/catalog/api/catalog-api';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  title: CatalogTitle;
  selected: boolean;
  onPress: () => void;
};

/** Search result row of the favorites steps (checkbox semantics). */
export function TitleResult({ title, selected, onPress }: Props) {
  return (
    <Pressable
      accessibilityRole="checkbox"
      accessibilityState={{ checked: selected }}
      onPress={onPress}
      style={({ pressed }) => [
        styles.row,
        selected ? styles.rowSelected : null,
        pressed ? styles.pressed : null,
      ]}
    >
      {title.posterUrl ? (
        <Image contentFit="cover" source={title.posterUrl} style={styles.poster} />
      ) : (
        <View style={[styles.poster, styles.posterFallback]}>
          <Text style={styles.posterFallbackLabel}>R</Text>
        </View>
      )}
      <View style={styles.info}>
        <Text numberOfLines={2} style={styles.title}>{title.title}</Text>
        {title.releaseDate ? <Text style={styles.releaseDate}>{title.releaseDate.slice(0, 4)}</Text> : null}
      </View>
      <Text style={styles.mark}>{selected ? 'Selecionado' : 'Adicionar'}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  row: {
    minHeight: 96,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    padding: 8,
    backgroundColor: colors.surface,
  },
  rowSelected: {
    borderColor: colors.brand,
  },
  pressed: {
    opacity: 0.78,
  },
  poster: {
    width: 52,
    height: 76,
    borderRadius: radii.extraSmall,
    backgroundColor: colors.surfaceElevated,
  },
  posterFallback: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  posterFallbackLabel: {
    color: colors.brandSecondary,
    fontSize: typography.section,
    fontFamily: fontFamily.extraBold,
  },
  info: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  releaseDate: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  mark: {
    color: colors.brandSecondary,
    fontSize: typography.eyebrow,
    fontFamily: fontFamily.bold,
  },
});
