import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { StyleSheet, View } from 'react-native';

import { AuthInput } from '@/components/auth/auth-input';
import { AuthLink } from '@/components/auth/auth-link';
import { FormMessage } from '@/components/auth/form-message';
import { PrimaryButton } from '@/components/auth/primary-button';
import { signInSchema, type SignInValues } from '@/schemas/auth-forms';
import { getAuthFailureMessage } from '@/services/auth-errors';
import { supabase } from '@/services/supabase';

export function SignInForm() {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const form = useForm<SignInValues>({
    resolver: zodResolver(signInSchema),
    defaultValues: { email: '', password: '' },
  });

  const submit = form.handleSubmit(async ({ email, password }) => {
    setErrorMessage(null);
    try {
      const { error } = await supabase.auth.signInWithPassword({
        email: email.trim(),
        password,
      });
      if (error) {
        setErrorMessage(getAuthFailureMessage(error, 'E-mail ou senha inválidos. Confira os dados e tente novamente.'));
      }
    } catch (error) {
      setErrorMessage(getAuthFailureMessage(error, 'Não foi possível autenticar agora. Tente novamente.'));
    }
  });

  return (
    <View style={styles.container}>
      <AuthInput control={form.control} label="E-mail" name="email" autoComplete="email" keyboardType="email-address" />
      <AuthInput control={form.control} label="Senha" name="password" autoComplete="current-password" secureTextEntry returnKeyType="done" />
      <AuthLink href="/forgot-password" label="Esqueci minha senha" />
      {errorMessage ? <FormMessage message={errorMessage} tone="error" /> : null}
      <PrimaryButton label="Entrar" loading={form.formState.isSubmitting} onPress={() => void submit()} />
      <AuthLink href="/sign-up" label="Ainda não tem conta? Criar conta" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 14,
  },
});
