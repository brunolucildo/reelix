import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { AuthScreen } from '@/components/auth/auth-screen';
import { PrimaryButton } from '@/components/auth/primary-button';
import { fetchAuthenticatedAccount } from '@/services/api-client';
import { useAuthSession } from '@/providers/auth-provider';
import { colors, fontFamily } from '@/theme/tokens';

export default function AuthenticatedHomeScreen() {
  const { session, signOut } = useAuthSession();
  const [signOutError, setSignOutError] = useState<string | null>(null);
  const account = useQuery({
    queryKey: ['auth', 'account'],
    queryFn: fetchAuthenticatedAccount,
  });

  const handleSignOut = async () => {
    setSignOutError(null);
    try {
      await signOut();
    } catch {
      setSignOutError('A sessão foi removida da interface, mas o armazenamento local não pôde ser confirmado. Feche o app e tente novamente.');
    }
  };

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
      <PrimaryButton label="Sair da conta" onPress={() => void handleSignOut()} variant="secondary" />
    </AuthScreen>
  );
}

const styles = StyleSheet.create({
  accountCard: {
    width: '100%',
    gap: 12,
    padding: 18,
    borderColor: colors.border,
    borderWidth: 1,
    borderRadius: 16,
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
