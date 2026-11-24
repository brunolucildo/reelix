import { Pressable, StyleSheet, Text, View } from 'react-native';

import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export type HomeContentType = 'movie' | 'series';

type Props = {
  selected: HomeContentType;
  onSelect: (type: HomeContentType) => void;
};

/** Séries / Filmes switch at the top of the Home. */
export function HomeContentTabs({ selected, onSelect }: Props) {
  return (
    <View accessibilityRole="tablist" style={styles.tabs}>
      {(['series', 'movie'] as const).map((type) => {
        const isSelected = type === selected;
        return (
          <Pressable
            key={type}
            accessibilityRole="tab"
            accessibilityState={{ selected: isSelected }}
            onPress={() => onSelect(type)}
            style={[styles.tab, isSelected && styles.selectedTab]}
          >
            <Text style={[styles.tabText, isSelected && styles.selectedTabText]}>
              {type === 'movie' ? 'Filmes' : 'Séries'}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  tabs: {
    flexDirection: 'row',
    gap: spacing.small,
  },
  tab: {
    minHeight: dimensions.minimumTouchTarget,
    minWidth: dimensions.posterWidth,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    backgroundColor: colors.surface,
  },
  selectedTab: {
    backgroundColor: colors.brand,
  },
  tabText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  selectedTabText: {
    color: colors.background,
  },
});
