import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { View } from 'react-native';
import { router } from 'expo-router';

import { AuthInput } from '@/components/auth/auth-input';
import { FormMessage } from '@/components/auth/form-message';
import { PrimaryButton } from '@/components/auth/primary-button';
import { useToast } from '@/providers/toast-provider';
import { resetPasswordSchema, type ResetPasswordValues } from '@/schemas/auth-forms';
import { getAuthFailureMessage } from '@/services/auth-errors';
import { supabase } from '@/services/supabase';
import { spacing } from '@/theme/tokens';

type ResetPasswordFormProps = {
  completed: boolean;
  onCompleted: () => void;
};

export function ResetPasswordForm({ completed, onCompleted }: ResetPasswordFormProps) {
  const { showError } = useToast();
  const form = useForm<ResetPasswordValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { password: '', confirmPassword: '' },
  });

  const submit = form.handleSubmit(async ({ password }) => {
    try {
      const { error } = await supabase.auth.updateUser({ password });
      if (error) {
        showError(getAuthFailureMessage(error, 'Não foi possível atualizar a senha. Solicite um novo link e tente novamente.'));
        return;
      }
      onCompleted();
    } catch (error) {
      showError(getAuthFailureMessage(error, 'Não foi possível atualizar a senha agora. Tente novamente.'));
    }
  });

  if (completed) {
    return (
      <View style={{ gap: spacing.medium }}>
        <FormMessage message="Senha alterada com sucesso." tone="success" />
        <PrimaryButton label="Continuar" onPress={() => router.replace('/home')} />
      </View>
    );
  }

  return (
    <View style={{ gap: spacing.medium }}>
      <AuthInput control={form.control} label="Nova senha (mínimo 8 caracteres)" name="password" placeholder="Nova senha" autoComplete="new-password" secureTextEntry />
      <AuthInput control={form.control} label="Confirme a nova senha" name="confirmPassword" placeholder="Repita a nova senha" autoComplete="new-password" secureTextEntry returnKeyType="done" />
      <PrimaryButton label="Atualizar senha" loading={form.formState.isSubmitting} onPress={() => void submit()} />
    </View>
  );
}
