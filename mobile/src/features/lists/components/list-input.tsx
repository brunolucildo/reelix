import { useState } from 'react';
import { StyleSheet, TextInput, type TextInputProps } from 'react-native';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Text input of the lists screen; owns its focus state for the brand border. */
export function ListInput({ multiline, style, ...props }: TextInputProps) {
  const [focused, setFocused] = useState(false);
  return (
    <TextInput
      placeholderTextColor={colors.textSecondary}
      multiline={multiline}
      {...props}
      onFocus={(event) => {
        setFocused(true);
        props.onFocus?.(event);
      }}
      onBlur={(event) => {
        setFocused(false);
        props.onBlur?.(event);
      }}
      style={[styles.input, multiline ? styles.multiline : null, focused ? styles.focused : null, style]}
    />
  );
}

const styles = StyleSheet.create({
  input: {
    minHeight: 48,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    color: colors.textPrimary,
    backgroundColor: colors.surfaceElevated,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  multiline: {
    minHeight: 88,
    paddingTop: spacing.medium,
    textAlignVertical: 'top',
  },
  focused: {
    borderColor: colors.brandSecondary,
    borderWidth: 2,
  },
});
