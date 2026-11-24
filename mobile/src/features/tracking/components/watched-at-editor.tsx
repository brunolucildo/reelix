import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { StyleSheet, Text, TextInput, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { searchInputStyle } from '@/features/tracking/components/search-field';
import { trackingStyles } from '@/features/tracking/components/tracking-styles';
import { watchedAtFormSchema, type WatchedAtFormValues } from '@/features/tracking/schemas';
import { parseLocalDateTimeInput, toLocalDateTimeInput } from '@/lib/format';
import { colors, spacing } from '@/theme/tokens';

type Props = {
  /** Current ISO timestamp of the entry; shown as the initial local text. */
  watchedAt: string;
  disabled: boolean;
  /** Receives the ISO timestamp; returns an inline error message, or null when it was submitted. */
  onSubmit: (watchedAtIso: string) => string | null;
  onCancel: () => void;
};

/** Assisted editor for the date and time a title was watched ("DD/MM/AAAA HH:mm"). */
export function WatchedAtEditor({ watchedAt, disabled, onSubmit, onCancel }: Props) {
  const form = useForm<WatchedAtFormValues>({
    resolver: zodResolver(watchedAtFormSchema),
    defaultValues: { watchedAt: toLocalDateTimeInput(watchedAt) },
  });
  const error = form.formState.errors.watchedAt?.message;

  const submit = form.handleSubmit(({ watchedAt: text }) => {
    const iso = parseLocalDateTimeInput(text);
    const message = iso ? onSubmit(iso) : null;
    if (message) form.setError('watchedAt', { message });
  });

  return (
    <View style={styles.container}>
      <Text style={trackingStyles.description}>Nova data e hora (horário local)</Text>
      <Controller
        control={form.control}
        name="watchedAt"
        render={({ field }) => (
          <TextInput
            accessibilityLabel="Nova data e hora da visualização"
            value={field.value}
            onBlur={field.onBlur}
            onChangeText={(value) => {
              field.onChange(value);
              form.clearErrors('watchedAt');
            }}
            placeholder="DD/MM/AAAA HH:mm"
            placeholderTextColor={colors.textSecondary}
            keyboardType="numbers-and-punctuation"
            style={searchInputStyle}
          />
        )}
      />
      {error ? <Text accessibilityRole="alert" style={trackingStyles.validation}>{error}</Text> : null}
      <PrimaryButton label="Salvar nova data" disabled={disabled} onPress={() => void submit()} />
      <PrimaryButton
        label="Cancelar alteração"
        variant="secondary"
        disabled={disabled}
        onPress={onCancel}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.small,
  },
});
