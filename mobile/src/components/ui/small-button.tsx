import { Pressable, StyleSheet, Text } from 'react-native';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  label: string;
  onPress: () => void;
  disabled?: boolean;
  accessibilityLabel?: string;
  /** Larger glyph with a 44 px minimum width, used for arrow buttons. */
  iconOnly?: boolean;
};

export function SmallButton({
  label,
  onPress,
  disabled = false,
  accessibilityLabel = label,
  iconOnly = false,
}: Props) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel}
      accessibilityState={{ disabled }}
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [
        styles.button,
        iconOnly ? styles.iconButton : null,
        pressed ? styles.pressed : null,
        disabled ? styles.disabled : null,
      ]}
    >
      <Text style={[styles.label, iconOnly ? styles.iconLabel : null]}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: {
    minHeight: 44,
    justifyContent: 'center',
    borderRadius: radii.small,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.small,
    backgroundColor: colors.surfaceElevated,
  },
  iconButton: {
    minWidth: 44,
    paddingHorizontal: spacing.medium,
  },
  label: {
    color: colors.textPrimary,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
    textAlign: 'center',
  },
  iconLabel: {
    fontSize: typography.section,
    lineHeight: typography.section + 4,
  },
  pressed: {
    opacity: 0.75,
  },
  disabled: {
    opacity: 0.55,
  },
});
