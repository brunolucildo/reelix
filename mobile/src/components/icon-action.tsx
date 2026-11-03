import type { ReactNode } from 'react';
import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  label: string;
  onPress: () => void;
  children: ReactNode;
  selected?: boolean;
  disabled?: boolean;
  loading?: boolean;
  radio?: boolean;
};

export function IconAction({ label, onPress, children, selected, disabled = false, loading = false, radio = false }: Props) {
  const [hintVisible, setHintVisible] = useState(false);
  return <View style={styles.container}>
    <Pressable
      accessibilityRole={radio ? 'radio' : selected === undefined ? 'button' : 'checkbox'}
      accessibilityLabel={label}
      accessibilityState={{ checked: radio ? undefined : selected, selected: radio ? selected : undefined, disabled: disabled || loading, busy: loading }}
      disabled={disabled || loading}
      onPress={onPress}
      onHoverIn={() => setHintVisible(true)}
      onHoverOut={() => setHintVisible(false)}
      onFocus={() => setHintVisible(true)}
      onBlur={() => setHintVisible(false)}
      onLongPress={() => setHintVisible(true)}
      onPressOut={() => setHintVisible(false)}
      style={({ pressed }) => [styles.button, selected && styles.selected, hintVisible && styles.focused, (pressed || disabled || loading) && styles.dimmed]}
    >
      {loading ? <ActivityIndicator color={colors.brandSecondary} /> : children}
    </Pressable>
    {hintVisible ? <View pointerEvents="none" style={styles.tooltip}><Text style={styles.hint}>{label}</Text></View> : null}
  </View>;
}

const styles = StyleSheet.create({
  container: { position: 'relative', zIndex: 1 },
  button: { width: dimensions.minimumTouchTarget, height: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center', borderRadius: radii.small, borderWidth: 2, borderColor: colors.surface, backgroundColor: colors.surface },
  selected: { borderColor: colors.brand, backgroundColor: colors.brandSurface },
  focused: { borderColor: colors.brandSecondary },
  dimmed: { opacity: 0.65 },
  tooltip: { position: 'absolute', top: dimensions.minimumTouchTarget + spacing.extraSmall, left: 0, width: dimensions.posterWidth + spacing.large, padding: spacing.small, borderRadius: radii.small, backgroundColor: colors.surfaceElevated, zIndex: 1 },
  hint: { color: colors.textPrimary, fontFamily: fontFamily.medium, fontSize: typography.caption },
});