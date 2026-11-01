import { useQuery } from '@tanstack/react-query';
import { Redirect, useRouter } from 'expo-router';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { AuthScreen } from '@/components/auth/auth-screen';
import { PrimaryButton } from '@/components/auth/primary-button';
import { fetchAuthenticatedAccount, fetchOnboarding } from '@/services/api-client';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast, useToast } from '@/providers/toast-provider';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export default function AuthenticatedHomeScreen() {
  const router = useRouter();
  const { session, signOut } = useAuthSession();
  const { showError } = useToast();
  const account = useQuery({
    queryKey: ['auth', 'account'],
    queryFn: fetchAuthenticatedAccount,
  });
  const onboarding = useQuery({
    queryKey: ['onboarding'],
    queryFn: fetchOnboarding,
  });
  useErrorToast(
    account.error,
    account.error instanceof Error && 'status' in account.error && account.error.status === 401
      ? 'A API não aceitou a sessão. Encerre a sessão e entre novamente.'
      : 'Não foi possível consultar sua conta. Verifique a conexão e tente novamente.',
  );
  useErrorToast(onboarding.error, 'Não foi possível carregar suas preferências. Verifique sua conexão e tente novamente.');

  const handleSignOut = async () => {
    try {
      await signOut();
    } catch {
      showError('A sessão foi removida da interface, mas o armazenamento local não pôde ser confirmado. Feche o app e tente novamente.');
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
      title="Seu próximo capítulo começa aqui."
      subtitle="Seu espaço está pronto. Suas escolhas podem mudar quando quiser."
    >
      <View style={styles.accountCard}>
        <Text style={styles.cardLabel}>SEU REELIX</Text>
        <Text style={styles.email}>{session?.user.email ?? 'E-mail não disponível'}</Text>
        {account.isPending ? (
          <View style={styles.accountStatus}>
            <ActivityIndicator color={colors.brand} size="small" />
            <Text style={styles.statusText}>Conferindo sua conta...</Text>
          </View>
        ) : account.isError ? (
          <View style={styles.queryError}>
            <Text style={styles.statusText}>Conta não verificada</Text>
            <PrimaryButton label="Tentar novamente" onPress={() => void account.refetch()} variant="secondary" />
          </View>
        ) : account.data ? (
          <Text style={styles.statusText}>Conta conectada</Text>
        ) : null}
        <View style={styles.stats}>
          <PreferenceCount label="Gêneros" value={onboarding.data.movieGenreIds.length + onboarding.data.seriesGenreIds.length} />
          <PreferenceCount label="Filmes" value={onboarding.data.favoriteMovieIds.length} />
          <PreferenceCount label="Séries" value={onboarding.data.favoriteSeriesIds.length} />
        </View>
      </View>
      <PrimaryButton
        label="Editar preferências"
        onPress={() => router.push({ pathname: '/onboarding', params: { edit: '1' } })}
      />
      <PrimaryButton label="Meu perfil" onPress={() => router.push('/profile')} variant="secondary" />
      <PrimaryButton label="Sair da conta" onPress={() => void handleSignOut()} variant="quiet" />
    </AuthScreen>
  );
}

function PreferenceCount({ label, value }: { label: string; value: number }) {
  return (
    <View style={styles.stat}>
      <Text style={styles.statValue}>{value}</Text>
      <Text style={styles.statLabel}>{label}</Text>
    </View>
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
    gap: spacing.medium,
    padding: spacing.medium,
    borderColor: colors.border,
    borderWidth: 1,
    borderRadius: radii.large,
    backgroundColor: colors.surface,
  },
  cardLabel: {
    color: colors.textSecondary,
    fontSize: typography.eyebrow,
    fontFamily: fontFamily.bold,
    letterSpacing: 1.2,
  },
  email: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  accountStatus: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
  },
  statusText: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  stats: {
    flexDirection: 'row',
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: spacing.medium,
  },
  stat: {
    flex: 1,
    alignItems: 'center',
    gap: spacing.extraSmall,
  },
  statValue: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  statLabel: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  queryError: {
    gap: spacing.small,
  },
});
