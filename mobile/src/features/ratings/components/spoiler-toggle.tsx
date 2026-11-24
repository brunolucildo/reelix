import { Pressable, StyleSheet, Text, View } from 'react-native';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  selected: boolean;
  label: string;
  onPress: () => void;
};

/** Checkbox row used to flag a comment as containing spoilers. */
export function SpoilerToggle({ selected, label, onPress }: Props) {
  return (
    <Pressable
      accessibilityRole="checkbox"
      accessibilityState={{ checked: selected }}
      onPress={onPress}
      style={styles.toggle}
    >
      <View style={[styles.checkbox, selected ? styles.checkedBox : null]}>
        {selected ? <Text style={styles.checkmark}>✓</Text> : null}
      </View>
      <Text style={styles.label}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  toggle: {
    minHeight: 44,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
  },
  checkbox: {
    width: 22,
    height: 22,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.textSecondary,
    borderRadius: radii.extraSmall,
  },
  checkedBox: {
    borderColor: colors.brand,
    backgroundColor: colors.brand,
  },
  checkmark: {
    color: colors.white,
    fontSize: typography.caption,
    fontFamily: fontFamily.bold,
  },
  label: {
    flex: 1,
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
});
