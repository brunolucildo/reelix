import { Pressable, ScrollView, StyleSheet, Text } from 'react-native';

import type { CatalogSeasonSummary } from '@/features/catalog/api/catalog-api';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  seasons: CatalogSeasonSummary[];
  selected: number | null;
  onSelect: (seasonNumber: number) => void;
};

export function SeasonTabs({ seasons, selected, onSelect }: Props) {
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.tabs}>
      {seasons.map((season) => {
        const isSelected = season.seasonNumber === selected;
        return (
          <Pressable
            key={season.seasonNumber}
            accessibilityRole="tab"
            accessibilityState={{ selected: isSelected }}
            onPress={() => onSelect(season.seasonNumber)}
            style={[styles.tab, isSelected && styles.selectedTab]}
          >
            <Text style={[styles.tabText, isSelected && styles.selectedTabText]}>
              {season.seasonNumber === 0 ? 'Especiais' : `Temporada ${season.seasonNumber}`}
            </Text>
          </Pressable>
        );
      })}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  tabs: { gap: spacing.small, paddingEnd: spacing.medium },
  tab: {
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.medium,
    borderRadius: radii.pill,
    backgroundColor: colors.surface,
  },
  selectedTab: { backgroundColor: colors.brand },
  tabText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  selectedTabText: { color: colors.background },
});
