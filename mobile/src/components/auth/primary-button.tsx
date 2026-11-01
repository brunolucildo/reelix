import { ActivityIndicator, Pressable, StyleSheet, Text } from 'react-native';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type PrimaryButtonProps = {
  label: string;
  onPress: () => void;
  loading?: boolean;
  disabled?: boolean;
  variant?: 'primary' | 'secondary' | 'quiet';
};

export function PrimaryButton({
  label,
  onPress,
  loading = false,
  disabled = false,
  variant = 'primary',
}: PrimaryButtonProps) {
  const isDisabled = disabled || loading;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ busy: loading, disabled: isDisabled }}
      disabled={isDisabled}
      onPress={onPress}
      style={({ pressed }) => [
        styles.button,
        variant === 'primary' ? styles.primary : null,
        variant === 'secondary' ? styles.secondary : null,
        variant === 'quiet' ? styles.quiet : null,
        pressed && !isDisabled ? styles.pressed : null,
        isDisabled ? styles.disabled : null,
      ]}
    >
      {loading ? (
        <ActivityIndicator color={variant === 'primary' ? colors.background : colors.brandSecondary} />
      ) : (
        <Text
          style={[
            styles.label,
            variant === 'secondary' ? styles.secondaryLabel : null,
            variant === 'quiet' ? styles.quietLabel : null,
          ]}
        >
          {label}
        </Text>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: {
    minHeight: 54,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.medium,
    paddingHorizontal: spacing.medium,
  },
  primary: {
    backgroundColor: colors.brand,
  },
  secondary: {
    borderColor: colors.border,
    borderWidth: 1,
    backgroundColor: colors.surfaceElevated,
  },
  quiet: {
    backgroundColor: 'transparent',
  },
  label: {
    color: colors.background,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  secondaryLabel: {
    color: colors.textPrimary,
  },
  quietLabel: {
    color: colors.error,
  },
  pressed: {
    opacity: 0.82,
  },
  disabled: {
    opacity: 0.55,
  },
});
