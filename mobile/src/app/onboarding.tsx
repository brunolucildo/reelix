import { Redirect, useLocalSearchParams } from 'expo-router';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { OnboardingFlow } from '@/features/onboarding/components/onboarding-flow';
import { onboardingStyles } from '@/features/onboarding/components/onboarding-styles';
import { useOnboardingPreferences } from '@/features/onboarding/hooks/use-onboarding-preferences';
import { colors, spacing } from '@/theme/tokens';

export default function OnboardingScreen() {
  const params = useLocalSearchParams<{ edit?: string }>();
  const isEditing = params.edit === '1';
  const preferences = useOnboardingPreferences(
    'Não foi possível carregar suas preferências. Verifique sua conexão e tente novamente.',
  );

  if (preferences.isError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={onboardingStyles.title}>Não foi possível carregar suas preferências.</Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void preferences.refetch()} />
      </View>
    );
  }

  if (preferences.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={onboardingStyles.secondaryText}>Preparando seu perfil...</Text>
      </View>
    );
  }

  if (preferences.data.isComplete && !isEditing) {
    return <Redirect href="/home" />;
  }

  return <OnboardingFlow preferences={preferences.data} isEditing={isEditing} />;
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
});
