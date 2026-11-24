import { StyleSheet, Text, View } from 'react-native';

import type { AchievementProgress } from '@/features/gamification/api/gamification-api';
import { AchievementMedal } from '@/features/gamification/components/achievement-medal';
import { gamificationStyles as shared } from '@/features/gamification/components/gamification-styles';
import { formatDate } from '@/lib/format';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** One achievement: medal, progress bar and status notes. */
export function AchievementCard({ achievement }: { achievement: AchievementProgress }) {
  const progress = Math.min(achievement.currentValue / achievement.threshold, 1);
  return (
    <View style={styles.card}>
      <AchievementMedal
        code={achievement.code}
        title={achievement.title}
        earned={achievement.isEarned}
        size={72}
      />
      <View style={styles.heading}>
        <Text style={styles.title}>{achievement.title}</Text>
        <Text style={achievement.isEarned ? styles.earned : styles.progressLabel}>
          {achievement.isEarned ? 'Conquistada' : `${achievement.currentValue}/${achievement.threshold}`}
        </Text>
      </View>
      <Text style={shared.body}>{achievement.description}</Text>
      <View accessibilityLabel={`Progresso: ${Math.round(progress * 100)}%`} style={styles.progressTrack}>
        <View style={[styles.progressFill, { width: `${Math.round(progress * 100)}%` }]} />
      </View>
      {achievement.status === 'revoked' ? (
        <Text style={shared.caption}>Revogada após uma alteração no histórico.</Text>
      ) : null}
      {achievement.isMetadataIncomplete ? (
        <Text style={shared.caption}>O progresso pode mudar quando os metadados de gêneros estiverem disponíveis.</Text>
      ) : null}
      {achievement.firstEarnedAt ? (
        <Text style={shared.caption}>
          Primeira conquista em {formatDate(achievement.firstEarnedAt)}.
        </Text>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  heading: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
  },
  title: {
    flex: 1,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  earned: {
    color: colors.success,
    fontSize: typography.caption,
    fontFamily: fontFamily.bold,
  },
  progressLabel: {
    color: colors.brandSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.bold,
  },
  progressTrack: {
    height: 7,
    overflow: 'hidden',
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
  },
  progressFill: {
    height: '100%',
    borderRadius: radii.pill,
    backgroundColor: colors.brand,
  },
});
