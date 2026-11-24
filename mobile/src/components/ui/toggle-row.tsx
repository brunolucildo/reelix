import { StyleSheet, Switch, Text, View } from 'react-native';

import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

type Props = {
  label: string;
  description: string;
  value: boolean;
  onValueChange: (value: boolean) => void;
  disabled?: boolean;
  accessibilityHint?: string;
  /**
   * Keeps each screen's current look until the design specs unify them:
   * `divided` (gamification), `plain` (notifications) and `privacy` (profile).
   */
  variant?: 'divided' | 'plain' | 'privacy';
};

export function ToggleRow({
  label,
  description,
  value,
  onValueChange,
  disabled = false,
  accessibilityHint,
  variant = 'divided',
}: Props) {
  const isPlain = variant === 'plain';
  return (
    <View style={[styles.row, isPlain ? null : styles.rowDivided]}>
      <View style={[styles.copy, isPlain ? null : styles.copyDivided]}>
        <Text style={[styles.label, variantLabel[variant]]}>{label}</Text>
        <Text style={[styles.description, variantDescription[variant]]}>{description}</Text>
      </View>
      <Switch
        accessibilityLabel={label}
        accessibilityHint={accessibilityHint}
        disabled={disabled}
        onValueChange={onValueChange}
        thumbColor={colors.white}
        trackColor={{ false: colors.surfaceElevated, true: colors.brand }}
        value={value}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.medium,
  },
  rowDivided: {
    minHeight: 72,
    justifyContent: 'space-between',
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  copy: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  copyDivided: {
    paddingVertical: spacing.small,
  },
  label: {
    color: colors.textPrimary,
  },
  description: {
    color: colors.textSecondary,
  },
});

const variantLabel = StyleSheet.create({
  divided: { fontSize: typography.bodySmall, fontFamily: fontFamily.semiBold },
  plain: { fontSize: typography.body, fontFamily: fontFamily.semiBold },
  privacy: { fontSize: typography.bodySmall, fontFamily: fontFamily.medium },
});

const variantDescription = StyleSheet.create({
  divided: { fontSize: typography.caption, lineHeight: 18, fontFamily: fontFamily.regular },
  plain: { fontSize: typography.bodySmall, fontFamily: fontFamily.regular },
  privacy: { fontSize: typography.caption, lineHeight: 17, fontFamily: fontFamily.regular },
});
