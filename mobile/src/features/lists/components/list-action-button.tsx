import type { ComponentType } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';
import type { SvgProps } from 'react-native-svg';

import CheckIcon from '@/assets/brand/navigation-icons/check.svg';
import { colors, dimensions, radii, spacing } from '@/theme/tokens';

type Props = {
  Icon: ComponentType<SvgProps>;
  label: string;
  selected?: boolean;
  disabled?: boolean;
  onPress: () => void;
};

/** 44 px icon button used on title cards; shows a check badge while `selected`. */
export function ListActionButton({ Icon, label, selected = false, disabled = false, onPress }: Props) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ selected, disabled }}
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [
        styles.button,
        selected && styles.selected,
        pressed && styles.pressed,
        disabled && styles.disabled,
      ]}
    >
      <Icon width={dimensions.iconSize} height={dimensions.iconSize} />
      {selected ? (
        <View pointerEvents="none" style={styles.check}>
          <CheckIcon width={dimensions.iconSize / 2} height={dimensions.iconSize / 2} />
        </View>
      ) : null}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: {
    width: dimensions.minimumTouchTarget,
    height: dimensions.minimumTouchTarget,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.small,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceElevated,
  },
  selected: {
    borderWidth: 1,
    borderColor: colors.brandSecondary,
    backgroundColor: colors.brandSurface,
  },
  check: {
    position: 'absolute',
    right: spacing.extraSmall,
    bottom: spacing.extraSmall,
    borderRadius: radii.pill,
    backgroundColor: colors.surface,
  },
  pressed: { opacity: 0.75 },
  disabled: { opacity: 0.55 },
});
