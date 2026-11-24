import { useState } from 'react';
import { type Control, Controller } from 'react-hook-form';
import { StyleSheet, Text, TextInput, View } from 'react-native';

import {
  BIOGRAPHY_MAX,
  DISPLAY_NAME_MAX,
  type ProfileFormValues,
} from '@/features/profile/schemas';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  control: Control<ProfileFormValues>;
  /** Called after either field changes (clears the "Perfil atualizado" line). */
  onChange: () => void;
  /** First validation message of the form, shown inline under the fields. */
  error?: string;
};

/** Display name and biography inputs with character counters. */
export function ProfileInfoFields({ control, onChange, error }: Props) {
  const [focused, setFocused] = useState<'displayName' | 'biography' | null>(null);

  return (
    <>
      <View style={styles.field}>
        <Text style={styles.label}>Nome de exibição</Text>
        <Controller
          control={control}
          name="displayName"
          render={({ field }) => (
            <>
              <TextInput
                accessibilityLabel="Nome de exibição"
                autoCapitalize="words"
                maxLength={DISPLAY_NAME_MAX}
                onBlur={() => {
                  setFocused(null);
                  field.onBlur();
                }}
                onChangeText={(value) => {
                  field.onChange(value);
                  onChange();
                }}
                onFocus={() => setFocused('displayName')}
                placeholder="Como você quer aparecer"
                placeholderTextColor={colors.textSecondary}
                style={[styles.input, focused === 'displayName' ? styles.inputFocused : null]}
                value={field.value}
              />
              <Text style={styles.counter}>{field.value.length}/{DISPLAY_NAME_MAX}</Text>
            </>
          )}
        />
      </View>
      <View style={styles.field}>
        <Text style={styles.label}>Biografia</Text>
        <Controller
          control={control}
          name="biography"
          render={({ field }) => (
            <>
              <TextInput
                accessibilityLabel="Biografia"
                maxLength={BIOGRAPHY_MAX}
                multiline
                onBlur={() => {
                  setFocused(null);
                  field.onBlur();
                }}
                onChangeText={(value) => {
                  field.onChange(value);
                  onChange();
                }}
                onFocus={() => setFocused('biography')}
                placeholder="Uma frase sobre você"
                placeholderTextColor={colors.textSecondary}
                style={[
                  styles.input,
                  styles.biographyInput,
                  focused === 'biography' ? styles.inputFocused : null,
                ]}
                textAlignVertical="top"
                value={field.value}
              />
              <Text style={styles.counter}>{field.value.length}/{BIOGRAPHY_MAX}</Text>
            </>
          )}
        />
      </View>
      {error ? <Text accessibilityRole="alert" style={styles.error}>{error}</Text> : null}
    </>
  );
}

const styles = StyleSheet.create({
  field: {
    gap: spacing.small,
  },
  label: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  input: {
    minHeight: 52,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  inputFocused: {
    borderColor: colors.brandSecondary,
  },
  biographyInput: {
    minHeight: 104,
    paddingTop: 12,
  },
  counter: {
    alignSelf: 'flex-end',
    color: colors.textSecondary,
    fontSize: typography.eyebrow,
    fontFamily: fontFamily.regular,
  },
  error: {
    color: colors.warning,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
});
