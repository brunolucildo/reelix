import { ActivityIndicator, ScrollView, StyleSheet, Text, View } from 'react-native';

import { BackButton } from '@/components/navigation/back-button';
import { PrimaryButton } from '@/components/ui/primary-button';
import { AchievementCard } from '@/features/gamification/components/achievement-card';
import { ActivityHistorySection } from '@/features/gamification/components/activity-history-section';
import { gamificationStyles as shared } from '@/features/gamification/components/gamification-styles';
import { PrivacyRankingSection } from '@/features/gamification/components/privacy-ranking-section';
import { StatisticsSummary } from '@/features/gamification/components/statistics-summary';
import { useGamificationScreen } from '@/features/gamification/hooks/use-gamification-screen';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

export default function GamificationScreen() {
  const {
    dashboard, monthLabel, periodStatistics, activityHistory, activityTitleById,
    activityGroups, ranking, preferencesMutation,
  } = useGamificationScreen();

  if (dashboard.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={shared.body}>Carregando conquistas...</Text>
      </View>
    );
  }

  if (!dashboard.data) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>Conquistas e estatísticas</Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void dashboard.refetch()} />
        <BackButton />
      </View>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.content}>
      <View style={styles.container}>
        <BackButton />
        <Text accessibilityRole="header" style={styles.title}>Conquistas e estatísticas</Text>
        {dashboard.isError ? (
          <PrimaryButton label="Atualizar conquistas" onPress={() => void dashboard.refetch()} variant="secondary" />
        ) : null}
        <Text style={shared.body}>
          Progresso calculado a partir dos seus registros de acompanhamento.
        </Text>

        <View style={shared.section}>
          <Text style={shared.sectionTitle}>Sua atividade total</Text>
          <StatisticsSummary statistics={dashboard.data.lifetimeStatistics} />
        </View>

        <View style={shared.section}>
          <Text style={shared.sectionTitle}>Este mês · {monthLabel}</Text>
          {periodStatistics.isPending ? (
            <ActivityIndicator color={colors.brand} />
          ) : periodStatistics.isError ? (
            <PrimaryButton
              label="Tentar novamente"
              onPress={() => void periodStatistics.refetch()}
              variant="secondary"
            />
          ) : (
            <StatisticsSummary statistics={periodStatistics.data} />
          )}
        </View>

        <ActivityHistorySection history={activityHistory} groups={activityGroups} titleById={activityTitleById} />

        <View style={shared.section}>
          <Text style={shared.sectionTitle}>Conquistas</Text>
          {dashboard.data.achievements.length === 0 ? <Text style={shared.body}>Ainda não há conquistas disponíveis.</Text> : null}
          {dashboard.data.achievements.map((achievement) => (
            <AchievementCard key={achievement.code} achievement={achievement} />
          ))}
        </View>

        <PrivacyRankingSection
          preferences={dashboard.data.preferences}
          mutation={preferencesMutation}
          ranking={ranking}
        />
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    backgroundColor: colors.background,
    padding: spacing.large,
  },
  container: {
    width: '100%',
    maxWidth: 620,
    alignSelf: 'center',
    gap: spacing.large,
  },
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
    fontFamily: fontFamily.extraBold,
  },
});
