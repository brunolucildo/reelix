import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { ProfileEditor } from '@/features/profile/components/profile-editor';
import { useProfile } from '@/features/profile/hooks/use-profile';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

export default function ProfileScreen() {
  const profile = useProfile('Não foi possível carregar o perfil. Verifique sua conexão e tente novamente.');

  if (profile.isError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>Não foi possível carregar o perfil.</Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void profile.refetch()} />
      </View>
    );
  }

  if (profile.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.secondaryText}>Carregando perfil...</Text>
      </View>
    );
  }

  return <ProfileEditor initialProfile={profile.data} />;
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    gap: spacing.medium,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    lineHeight: 36,
    fontFamily: fontFamily.extraBold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    lineHeight: 21,
    fontFamily: fontFamily.regular,
  },
});
