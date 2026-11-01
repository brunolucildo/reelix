import { useQuery } from '@tanstack/react-query';
import { Redirect, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { AuthScreen } from '@/components/auth/auth-screen';
import { PrimaryButton } from '@/components/auth/primary-button';
import { fetchAuthenticatedAccount, fetchOnboarding } from '@/services/api-client';
import { useAuthSession } from '@/providers/auth-provider';
import { colors, fontFamily, radii } from '@/theme/tokens';

export default function AuthenticatedHomeScreen() {
  const router = useRouter();
  const { session, signOut } = useAuthSession();
  const [signOutError, setSignOutError] = useState<string | null>(null);
  const account = useQuery({
    queryKey: ['auth', 'account'],
    queryFn: fetchAuthenticatedAccount,
  });
  const onboarding = useQuery({
    queryKey: ['onboarding'],
    queryFn: fetchOnboarding,
  });

  const handleSignOut = async () => {
    setSignOutError(null);
    try {
      await signOut();
    } catch {
      setSignOutError('A sessão foi removida da interface, mas o armazenamento local não pôde ser confirmado. Feche o app e tente novamente.');
    }
  };

  if (onboarding.isPending) {
    return (
      <View style={styles.loading}>
        <ActivityIndicator color={colors.brand} size="large" />
      </View>
    );
  }

  if (onboarding.isError) {
    return (
      <AuthScreen title="Não foi possível abrir o Reelix." subtitle="Verifique sua conexão e tente novamente.">
        <PrimaryButton label="Tentar novamente" onPress={() => void onboarding.refetch()} />
      </AuthScreen>
    );
  }

  if (!onboarding.data.isComplete) {
    return <Redirect href="/onboarding" />;
  }

  return (
    <AuthScreen
      title="Tudo pronto."
      subtitle="Sua conta Reelix está autenticada."
    >
      <View style={styles.accountCard}>
        <Text style={styles.cardLabel}>CONTA AUTENTICADA</Text>
        <Text style={styles.email}>{session?.user.email ?? 'E-mail não disponível'}</Text>
        {account.isPending ? (
          <ActivityIndicator color={colors.brand} />
        ) : account.isError ? (
          <View style={styles.queryError}>
            <Text accessibilityRole="alert" style={styles.error}>
              {account.error instanceof Error && 'status' in account.error && account.error.status === 401
                ? 'A API não aceitou a sessão. Encerre a sessão e entre novamente.'
                : 'Não foi possível consultar sua conta. Verifique a conexão e tente novamente.'}
            </Text>
            <PrimaryButton label="Tentar novamente" onPress={() => void account.refetch()} variant="secondary" />
          </View>
        ) : account.data ? (
          <Text style={styles.accountId}>ID Reelix: {account.data.id}</Text>
        ) : null}
      </View>
      {signOutError ? (
        <Text accessibilityRole="alert" style={styles.error}>
          {signOutError}
        </Text>
      ) : null}
      <PrimaryButton label="Meu perfil" onPress={() => router.push('/profile')} variant="secondary" />
      <PrimaryButton
        label="Editar preferências"
        onPress={() => router.push({ pathname: '/onboarding', params: { edit: '1' } })}
        variant="secondary"
      />
      <PrimaryButton label="Sair da conta" onPress={() => void handleSignOut()} variant="secondary" />
    </AuthScreen>
  );
}

const styles = StyleSheet.create({
  loading: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.background,
  },
  accountCard: {
    width: '100%',
    gap: 12,
    padding: 18,
    borderColor: colors.border,
    borderWidth: 1,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  cardLabel: {
    color: colors.textSecondary,
    fontSize: 11,
    fontFamily: fontFamily.bold,
    letterSpacing: 1.6,
  },
  email: {
    color: colors.textPrimary,
    fontSize: 17,
    fontFamily: fontFamily.bold,
  },
  accountId: {
    color: colors.textSecondary,
    fontSize: 12,
    fontFamily: fontFamily.regular,
  },
  queryError: {
    gap: 12,
  },
  error: {
    color: colors.error,
    fontSize: 14,
    fontFamily: fontFamily.regular,
    lineHeight: 21,
  },
});
