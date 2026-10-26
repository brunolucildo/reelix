import { Controller, type Control, type FieldPath, type FieldValues } from 'react-hook-form';
import { StyleSheet, Text, TextInput, type TextInputProps, View } from 'react-native';

import { colors, fontFamily } from '@/theme/tokens';

type AuthInputProps<TValues extends FieldValues> = {
  control: Control<TValues>;
  name: FieldPath<TValues>;
  label: string;
  autoComplete?: TextInputProps['autoComplete'];
  keyboardType?: TextInputProps['keyboardType'];
  secureTextEntry?: boolean;
  returnKeyType?: TextInputProps['returnKeyType'];
};

export function AuthInput<TValues extends FieldValues>({
  control,
  name,
  label,
  autoComplete,
  keyboardType,
  secureTextEntry = false,
  returnKeyType = 'next',
}: AuthInputProps<TValues>) {
  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <View style={styles.container}>
          <Text style={styles.label}>{label}</Text>
          <TextInput
            accessibilityLabel={label}
            accessibilityHint={fieldState.error?.message}
            autoCapitalize="none"
            autoComplete={autoComplete}
            autoCorrect={false}
            keyboardType={keyboardType}
            onBlur={field.onBlur}
            onChangeText={field.onChange}
            returnKeyType={returnKeyType}
            secureTextEntry={secureTextEntry}
            style={[styles.input, fieldState.error ? styles.inputError : null]}
            textContentType={
              secureTextEntry
                ? autoComplete === 'current-password'
                  ? 'password'
                  : 'newPassword'
                : 'emailAddress'
            }
            value={typeof field.value === 'string' ? field.value : ''}
          />
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
    gap: 7,
  },
  label: {
    color: colors.textPrimary,
    fontSize: 14,
    fontFamily: fontFamily.semiBold,
  },
  input: {
    minHeight: 52,
    paddingHorizontal: 15,
    borderColor: colors.border,
    borderWidth: 1,
    borderRadius: 12,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: 16,
    fontFamily: fontFamily.regular,
  },
  inputError: {
    borderColor: colors.error,
  },
  error: {
    color: colors.error,
    fontSize: 12,
    fontFamily: fontFamily.regular,
  },
});
