import { useState } from 'react';
import { Controller, type Control, type FieldPath, type FieldValues } from 'react-hook-form';
import { Pressable, StyleSheet, Text, TextInput, type TextInputProps, View } from 'react-native';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type AuthInputProps<TValues extends FieldValues> = {
  control: Control<TValues>;
  name: FieldPath<TValues>;
  label: string;
  placeholder?: string;
  autoComplete?: TextInputProps['autoComplete'];
  keyboardType?: TextInputProps['keyboardType'];
  secureTextEntry?: boolean;
  returnKeyType?: TextInputProps['returnKeyType'];
};

export function AuthInput<TValues extends FieldValues>({
  control,
  name,
  label,
  placeholder,
  autoComplete,
  keyboardType,
  secureTextEntry = false,
  returnKeyType = 'next',
}: AuthInputProps<TValues>) {
  const [isFocused, setIsFocused] = useState(false);
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);

  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <View style={styles.container}>
          <Text style={styles.label}>{label}</Text>
          <View
            style={[
              styles.inputFrame,
              isFocused ? styles.inputFocused : null,
              fieldState.error ? styles.inputError : null,
            ]}
          >
            <TextInput
              accessibilityLabel={label}
              accessibilityHint={fieldState.error?.message}
              autoCapitalize="none"
              autoComplete={autoComplete}
              autoCorrect={false}
              keyboardType={keyboardType}
              onFocus={() => setIsFocused(true)}
              onBlur={() => {
                field.onBlur();
                setIsFocused(false);
              }}
              onChangeText={field.onChange}
              placeholder={placeholder}
              placeholderTextColor={colors.textSecondary}
              returnKeyType={returnKeyType}
              secureTextEntry={secureTextEntry && !isPasswordVisible}
              style={styles.input}
              textContentType={
                secureTextEntry
                  ? autoComplete === 'current-password'
                    ? 'password'
                    : 'newPassword'
                  : 'emailAddress'
              }
              value={typeof field.value === 'string' ? field.value : ''}
            />
            {secureTextEntry ? (
              <Pressable
                accessibilityLabel={isPasswordVisible ? 'Ocultar senha' : 'Mostrar senha'}
                accessibilityRole="button"
                accessibilityState={{ selected: isPasswordVisible }}
                onPress={() => setIsPasswordVisible((visible) => !visible)}
                style={({ pressed }) => [styles.visibilityToggle, pressed ? styles.visibilityTogglePressed : null]}
              >
                <Text style={styles.visibilityLabel}>{isPasswordVisible ? 'Ocultar' : 'Mostrar'}</Text>
              </Pressable>
            ) : null}
          </View>
          {fieldState.error ? (
            <Text accessibilityRole="alert" style={styles.error}>
              {fieldState.error.message}
            </Text>
          ) : null}
        </View>
      )}
    />
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.extraSmall,
  },
  label: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  inputFrame: {
    minHeight: 54,
    flexDirection: 'row',
    alignItems: 'center',
    borderColor: colors.border,
    borderWidth: 1,
    borderRadius: radii.small,
    backgroundColor: colors.surface,
  },
  input: {
    flex: 1,
    minHeight: 52,
    paddingHorizontal: spacing.medium,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  visibilityToggle: {
    minWidth: 76,
    minHeight: 44,
    marginRight: spacing.extraSmall,
    paddingHorizontal: spacing.small,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.small,
  },
  visibilityTogglePressed: {
    backgroundColor: colors.surfaceElevated,
  },
  visibilityLabel: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  inputFocused: {
    borderColor: colors.brandSecondary,
  },
  inputError: {
    borderColor: colors.error,
  },
  error: {
    color: colors.error,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
});
