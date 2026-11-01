import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { View } from 'react-native';

import { AuthInput } from '@/components/auth/auth-input';
import { AuthLink } from '@/components/auth/auth-link';
import { FormMessage } from '@/components/auth/form-message';
import { PrimaryButton } from '@/components/auth/primary-button';
import { useToast } from '@/providers/toast-provider';
import { emailSchema, type EmailValues } from '@/schemas/auth-forms';
import { getAuthFailureMessage } from '@/services/auth-errors';
import { createAuthRedirectUrl } from '@/services/auth-redirect';
import { supabase } from '@/services/supabase';
import { spacing } from '@/theme/tokens';

export function ForgotPasswordForm() {
  const { showError } = useToast();
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const form = useForm<EmailValues>({
    resolver: zodResolver(emailSchema),
    defaultValues: { email: '' },
  });

  const submit = form.handleSubmit(async ({ email }) => {
    setSuccessMessage(null);

    try {
      const { error } = await supabase.auth.resetPasswordForEmail(email.trim(), {
        redirectTo: createAuthRedirectUrl('recovery'),
      });
      if (error) {
        showError(
          getAuthFailureMessage(error, 'Não foi possível solicitar a recuperação agora. Tente novamente mais tarde.'),
        );
        return;
      }

      setSuccessMessage(
        'Se houver uma conta associada a esse endereço, enviaremos as instruções para redefinir a senha.',
      );
    } catch (error) {
      showError(getAuthFailureMessage(error, 'Não foi possível solicitar a recuperação agora. Tente novamente.'));
    }
  });

  return (
    <View style={{ gap: spacing.medium }}>
      <AuthInput control={form.control} label="E-mail" name="email" placeholder="voce@exemplo.com" autoComplete="email" keyboardType="email-address" returnKeyType="done" />
      {successMessage ? <FormMessage message={successMessage} tone="success" /> : null}
      <PrimaryButton label="Enviar instruções" loading={form.formState.isSubmitting} onPress={() => void submit()} />
      <AuthLink href="/" label="Voltar para entrar" />
    </View>
  );
}
