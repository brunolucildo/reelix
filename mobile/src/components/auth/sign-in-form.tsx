import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { StyleSheet, View } from 'react-native';

import { AuthInput } from '@/components/auth/auth-input';
import { AuthLink } from '@/components/auth/auth-link';
import { PrimaryButton } from '@/components/auth/primary-button';
import { useToast } from '@/providers/toast-provider';
import { signInSchema, type SignInValues } from '@/schemas/auth-forms';
import { getAuthFailureMessage } from '@/services/auth-errors';
import { supabase } from '@/services/supabase';
import { spacing } from '@/theme/tokens';

export function SignInForm() {
  const { showError } = useToast();
  const form = useForm<SignInValues>({
    resolver: zodResolver(signInSchema),
    defaultValues: { email: '', password: '' },
  });

  const submit = form.handleSubmit(async ({ email, password }) => {
    try {
      const { error } = await supabase.auth.signInWithPassword({
        email: email.trim(),
        password,
      });
      if (error) {
        showError(getAuthFailureMessage(error, 'E-mail ou senha inválidos. Confira os dados e tente novamente.'));
      }
    } catch (error) {
      showError(getAuthFailureMessage(error, 'Não foi possível autenticar agora. Tente novamente.'));
    }
  });

  return (
    <View style={styles.container}>
      <AuthInput control={form.control} label="E-mail" name="email" placeholder="voce@exemplo.com" autoComplete="email" keyboardType="email-address" />
      <AuthInput control={form.control} label="Senha" name="password" placeholder="Sua senha" autoComplete="current-password" secureTextEntry returnKeyType="done" />
      <AuthLink href="/forgot-password" label="Esqueci minha senha" align="right" />
      <PrimaryButton label="Entrar" loading={form.formState.isSubmitting} onPress={() => void submit()} />
      <AuthLink href="/sign-up" label="Ainda não tem conta? Criar conta" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.medium,
  },
});
