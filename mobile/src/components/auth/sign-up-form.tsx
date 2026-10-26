import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { View } from 'react-native';

import { AuthInput } from '@/components/auth/auth-input';
import { AuthLink } from '@/components/auth/auth-link';
import { FormMessage } from '@/components/auth/form-message';
import { PrimaryButton } from '@/components/auth/primary-button';
import { signUpSchema, type SignUpValues } from '@/schemas/auth-forms';
import { getAuthFailureMessage } from '@/services/auth-errors';
import { createAuthRedirectUrl } from '@/services/auth-redirect';
import { supabase } from '@/services/supabase';

export function SignUpForm() {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const form = useForm<SignUpValues>({
    resolver: zodResolver(signUpSchema),
    defaultValues: { email: '', password: '' },
  });

  const submit = form.handleSubmit(async ({ email, password }) => {
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      const { error } = await supabase.auth.signUp({
        email: email.trim(),
        password,
        options: {
          emailRedirectTo: createAuthRedirectUrl('signup'),
        },
      });

      if (error) {
        setErrorMessage(
          getAuthFailureMessage(
            error,
            'Não foi possível concluir o cadastro com esses dados. Confira as informações e tente novamente.',
          ),
        );
        return;
      }

      setSuccessMessage(
        'Se os dados puderem ser usados para criar uma conta, enviaremos as instruções de confirmação para o e-mail informado.',
      );
    } catch (error) {
      setErrorMessage(getAuthFailureMessage(error, 'Não foi possível concluir o cadastro agora. Tente novamente.'));
    }
  });

  return (
    <View style={{ gap: 14 }}>
      <AuthInput control={form.control} label="E-mail" name="email" autoComplete="email" keyboardType="email-address" />
      <AuthInput control={form.control} label="Senha (mínimo 8 caracteres)" name="password" autoComplete="new-password" secureTextEntry returnKeyType="done" />
      {errorMessage ? <FormMessage message={errorMessage} tone="error" /> : null}
      {successMessage ? <FormMessage message={successMessage} tone="success" /> : null}
      <PrimaryButton label="Criar conta" loading={form.formState.isSubmitting} onPress={() => void submit()} />
      <AuthLink href="/" label="Já tem conta? Entrar" />
    </View>
  );
}
