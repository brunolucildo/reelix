import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { View } from 'react-native';
import { router } from 'expo-router';

import { AuthInput } from '@/components/auth/auth-input';
import { FormMessage } from '@/components/auth/form-message';
import { PrimaryButton } from '@/components/auth/primary-button';
import { resetPasswordSchema, type ResetPasswordValues } from '@/schemas/auth-forms';
import { getAuthFailureMessage } from '@/services/auth-errors';
import { supabase } from '@/services/supabase';

type ResetPasswordFormProps = {
  completed: boolean;
  onCompleted: () => void;
};

export function ResetPasswordForm({ completed, onCompleted }: ResetPasswordFormProps) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const form = useForm<ResetPasswordValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { password: '', confirmPassword: '' },
  });

  const submit = form.handleSubmit(async ({ password }) => {
    setErrorMessage(null);
    try {
      const { error } = await supabase.auth.updateUser({ password });
      if (error) {
        setErrorMessage(getAuthFailureMessage(error, 'Não foi possível atualizar a senha. Solicite um novo link e tente novamente.'));
        return;
      }
      onCompleted();
    } catch (error) {
      setErrorMessage(getAuthFailureMessage(error, 'Não foi possível atualizar a senha agora. Tente novamente.'));
    }
  });

  if (completed) {
    return (
      <View style={{ gap: 14 }}>
        <FormMessage message="Senha alterada com sucesso." tone="success" />
        <PrimaryButton label="Continuar" onPress={() => router.replace('/home')} />
      </View>
    );
  }

  return (
    <View style={{ gap: 14 }}>
      <AuthInput control={form.control} label="Nova senha (mínimo 8 caracteres)" name="password" autoComplete="new-password" secureTextEntry />
      <AuthInput control={form.control} label="Confirme a nova senha" name="confirmPassword" autoComplete="new-password" secureTextEntry returnKeyType="done" />
      {errorMessage ? <FormMessage message={errorMessage} tone="error" /> : null}
      <PrimaryButton label="Atualizar senha" loading={form.formState.isSubmitting} onPress={() => void submit()} />
    </View>
  );
}
