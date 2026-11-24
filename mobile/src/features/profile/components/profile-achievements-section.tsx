import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, StyleSheet, Text } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { AchievementGrid } from '@/features/gamification/components/achievement-grid';
import { useGamificationDashboard } from '@/features/gamification/hooks/use-gamification-dashboard';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Achievement medals of the profile, with a shortcut to the full progress screen. */
export function ProfileAchievementsSection() {
  const router = useRouter();
  const gamification = useGamificationDashboard();

  return (
    <>
      {gamification.isPending ? (
        <ActivityIndicator color={colors.brand} />
      ) : gamification.isError ? (
        <PrimaryButton
          label="Tentar carregar"
          onPress={() => void gamification.refetch()}
          variant="secondary"
        />
      ) : gamification.data.achievements.length === 0 ? (
        <Text style={styles.empty}>Suas conquistas aparecerão aqui.</Text>
      ) : (
        <AchievementGrid achievements={gamification.data.achievements} />
      )}
      <Pressable
        accessibilityRole="button"
        onPress={() => router.push('/gamification')}
        style={styles.button}
      >
        <Text style={styles.label}>Ver progresso e estatísticas</Text>
        <Text style={styles.action}>Abrir</Text>
      </Pressable>
    </>
  );
}

const styles = StyleSheet.create({
  empty: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    lineHeight: 17,
    fontFamily: fontFamily.regular,
  },
  button: {
    minHeight: 48,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    backgroundColor: colors.surface,
  },
  label: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.medium,
  },
  action: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
});
