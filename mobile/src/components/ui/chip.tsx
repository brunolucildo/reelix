import { Pressable, StyleSheet, Text } from 'react-native';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type SelectableProps = {
  label: string;
  selected: boolean;
  onPress: () => void;
};

/** Multi-select option (checkbox semantics), e.g. genre choices. */
export function Chip({ label, selected, onPress }: SelectableProps) {
  return (
    <Pressable
      accessibilityRole="checkbox"
      accessibilityState={{ checked: selected }}
      onPress={onPress}
      style={({ pressed }) => [
        styles.chip,
        selected ? styles.selected : null,
        pressed ? styles.pressed : null,
      ]}
    >
      <Text style={[styles.label, selected ? styles.labelSelected : null]}>{label}</Text>
    </Pressable>
  );
}

/** Single-select option laid out inside a segmented row. */
export function SegmentButton({ label, selected, onPress }: SelectableProps) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected }}
      onPress={onPress}
      style={({ pressed }) => [
        styles.segment,
        selected ? styles.selected : null,
        pressed ? styles.pressed : null,
      ]}
    >
      <Text style={[styles.label, selected ? styles.labelSelected : null]}>{label}</Text>
    </Pressable>
  );
}

/** Pill-shaped tab (tab semantics) for switching between sections of a screen. */
export function TabButton({
  label,
  active,
  onPress,
}: {
  label: string;
  active: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="tab"
      accessibilityLabel={label}
      accessibilityState={{ selected: active }}
      onPress={onPress}
      style={[styles.tab, active ? styles.tabActive : null]}
    >
      <Text style={[styles.tabLabel, active ? styles.tabLabelActive : null]}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  chip: {
    minHeight: 44,
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    backgroundColor: colors.surface,
  },
  segment: {
    flex: 1,
    minHeight: 48,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    backgroundColor: colors.surface,
  },
  selected: {
    borderColor: colors.brand,
    backgroundColor: colors.brandSurface,
  },
  label: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  labelSelected: {
    color: colors.textPrimary,
  },
  pressed: {
    opacity: 0.75,
  },
  tab: {
    minHeight: 44,
    justifyContent: 'center',
    borderRadius: radii.pill,
    borderColor: colors.border,
    borderWidth: 1,
    paddingHorizontal: spacing.medium,
  },
  tabActive: {
    backgroundColor: colors.brand,
    borderColor: colors.brand,
  },
  tabLabel: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  tabLabelActive: {
    color: colors.background,
  },
});
