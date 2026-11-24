import { useState } from 'react';
import { StyleSheet, TextInput, type TextInputProps } from 'react-native';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Text input with the brand focus border; owns its focus state. */
export function SearchField(props: TextInputProps) {
  const [focused, setFocused] = useState(false);
  return (
    <TextInput
      placeholderTextColor={colors.textSecondary}
      {...props}
      onFocus={(event) => {
        setFocused(true);
        props.onFocus?.(event);
      }}
      onBlur={(event) => {
        setFocused(false);
        props.onBlur?.(event);
      }}
      style={[styles.input, focused ? styles.focused : null]}
    />
  );
}

const styles = StyleSheet.create({
  input: {
    minHeight: spacing.extraLarge + spacing.medium,
    padding: spacing.medium,
    borderWidth: spacing.extraSmall / spacing.extraSmall,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surfaceElevated,
    color: colors.textPrimary,
    fontFamily: fontFamily.regular,
    fontSize: typography.body,
  },
  focused: {
    borderColor: colors.brandSecondary,
  },
});

/** The unfocused input look, for inputs that never show the focus border. */
export const searchInputStyle = styles.input;
