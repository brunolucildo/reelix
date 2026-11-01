import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { useMemo } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Switch,
  Text,
  View,
} from 'react-native';

import { PrimaryButton } from '@/components/auth/primary-button';
import { useErrorToast, useToast } from '@/providers/toast-provider';
import {
  fetchGamificationDashboard,
  fetchGamificationStatistics,
  fetchMonthlyFriendsRanking,
  saveGamificationPreferences,
  type AchievementProgress,
  type GamificationStatistics,
} from '@/services/api-client';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export default function GamificationScreen() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { showError } = useToast();
  const dashboard = useQuery({
    queryKey: ['gamification', 'dashboard'],
    queryFn: ({ signal }) => fetchGamificationDashboard(signal),
  });
  const now = new Date();
  const year = now.getUTCFullYear();
  const month = now.getUTCMonth() + 1;
  const periodStart = useMemo(
    () => new Date(Date.UTC(year, month - 1, 1)),
    [year, month],
  );
  const periodEnd = useMemo(
    () => new Date(Date.UTC(year, month, 1)),
    [year, month],
  );
  const periodStatistics = useQuery({
    queryKey: ['gamification', 'statistics', periodStart.toISOString(), periodEnd.toISOString()],
    queryFn: ({ signal }) => fetchGamificationStatistics(periodStart, periodEnd, signal),
  });
  const ranking = useInfiniteQuery({
    queryKey: ['gamification', 'ranking', year, month],
    initialPageParam: undefined as string | undefined,
    queryFn: ({ pageParam, signal }) =>
      fetchMonthlyFriendsRanking(year, month, pageParam, signal),
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
    enabled: dashboard.data?.preferences.rankingEnabled === true,
  });
  const preferencesMutation = useMutation({
    mutationFn: saveGamificationPreferences,
    onSuccess: (preferences) => {
      queryClient.setQueryData(
        ['gamification', 'dashboard'],
        (current: typeof dashboard.data) => current
          ? { ...current, preferences }
          : current,
      );
      void queryClient.invalidateQueries({ queryKey: ['gamification', 'ranking'] });
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível salvar as preferências.');
      void queryClient.invalidateQueries({ queryKey: ['gamification', 'dashboard'] });
    },
  });

  useErrorToast(dashboard.error, 'Não foi possível carregar suas conquistas.');
  useErrorToast(periodStatistics.error, 'Não foi possível carregar as estatísticas do período.');
  useErrorToast(ranking.error, 'Não foi possível carregar o ranking entre amigos.');

  if (dashboard.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.body}>Carregando conquistas...</Text>
      </View>
    );
  }

  if (dashboard.isError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>Gamificação indisponível</Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void dashboard.refetch()} />
        <PrimaryButton label="Voltar" onPress={() => router.back()} variant="secondary" />
      </View>
    );
  }

  const preferences = dashboard.data.preferences;

  return (
    <ScrollView contentContainerStyle={styles.content}>
      <View style={styles.container}>
        <Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.backButton}>
          <Text style={styles.backLabel}>Voltar</Text>
        </Pressable>
        <Text accessibilityRole="header" style={styles.title}>Conquistas e estatísticas</Text>
        <Text style={styles.body}>
          Progresso calculado a partir dos seus registros de acompanhamento.
        </Text>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Sua atividade total</Text>
          <StatisticsSummary statistics={dashboard.data.lifetimeStatistics} />
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Neste mês</Text>
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

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Conquistas</Text>
          {dashboard.data.achievements.map((achievement) => (
            <AchievementCard key={achievement.code} achievement={achievement} />
          ))}
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Privacidade e ranking</Text>
          <PreferenceToggle
            label="Exibir badges no perfil"
            description="Compartilha somente conquistas obtidas, nunca seu histórico."
            value={preferences.badgesPublic}
            disabled={preferencesMutation.isPending}
            onChange={(badgesPublic) => preferencesMutation.mutate({
              badgesPublic,
              rankingEnabled: preferences.rankingEnabled,
            })}
          />
          <PreferenceToggle
            label="Participar do ranking entre amigos"
            description="Ranking mensal de filmes distintos assistidos. A participação é opcional."
            value={preferences.rankingEnabled}
            disabled={preferencesMutation.isPending}
            onChange={(rankingEnabled) => preferencesMutation.mutate({
              badgesPublic: preferences.badgesPublic,
              rankingEnabled,
            })}
          />
          {preferences.rankingEnabled ? (
            <View style={styles.ranking}>
              <Text style={styles.rankingTitle}>Filmes assistidos no mês</Text>
              {ranking.isPending ? (
                <ActivityIndicator color={colors.brand} />
              ) : ranking.isError ? (
                <PrimaryButton
                  label="Tentar novamente"
                  onPress={() => void ranking.refetch()}
                  variant="secondary"
                />
              ) : (
                <>
                  {(ranking.data.pages[0]?.items ?? []).length === 0 ? (
                    <Text style={styles.body}>Ainda não há filmes no ranking deste mês.</Text>
                  ) : null}
                  {ranking.data.pages.flatMap((page) => page.items).map((entry) => (
                    <View key={entry.accountId} style={styles.rankingRow}>
                      <Text style={styles.rankingName}>
                        {entry.position}. {entry.displayName}
                      </Text>
                      <Text style={styles.rankingCount}>{entry.moviesWatched}</Text>
                    </View>
                  ))}
                  {ranking.hasNextPage ? (
                    <PrimaryButton
                      label="Carregar mais"
                      loading={ranking.isFetchingNextPage}
                      onPress={() => void ranking.fetchNextPage()}
                      variant="secondary"
                    />
                  ) : null}
                </>
              )}
            </View>
          ) : (
            <Text style={styles.body}>
              Ative a participação para aparecer no ranking mensal apenas entre amigos que também optaram por participar.
            </Text>
          )}
        </View>
      </View>
    </ScrollView>
  );
}

function StatisticsSummary({ statistics }: { statistics: GamificationStatistics }) {
  return (
    <View style={styles.statistics}>
      <Text style={styles.statLine}>
        {statistics.moviesWatched} filmes · {statistics.episodesWatched} episódios · {statistics.seriesExplored} séries
      </Text>
      <Text style={styles.statLine}>{statistics.genresExplored} gêneros de filmes conhecidos</Text>
      <Text style={styles.body}>
        {statistics.estimatedWatchHours === null
          ? 'Tempo estimado indisponível: ainda não há durações conhecidas.'
          : `Tempo estimado: ${statistics.estimatedWatchHours.toLocaleString('pt-BR', {
            maximumFractionDigits: 2,
          })} h`}
      </Text>
      <Text style={styles.body}>
        Cobertura de duração: {statistics.itemsWithRuntime}/{statistics.itemsWithRuntime + statistics.itemsWithoutRuntime} itens
        {' '}({Math.round(statistics.runtimeCoverage * 100)}%).
      </Text>
      <Text style={styles.caption}>
        Tempo confirmado não é medido. Fontes: {statistics.runtimeSources.length > 0
          ? statistics.runtimeSources.join(', ')
          : 'nenhuma duração disponível'}
        {statistics.runtimeMetadataVersion === null
          ? ''
          : ` · snapshot v${statistics.runtimeMetadataVersion}`}
      </Text>
      {statistics.moviesMissingGenreMetadata > 0 ? (
        <Text style={styles.caption}>
          Alguns filmes ainda não têm gêneros conhecidos; eles não contam para essa estatística.
        </Text>
      ) : null}
    </View>
  );
}

function AchievementCard({ achievement }: { achievement: AchievementProgress }) {
  const progress = Math.min(achievement.currentValue / achievement.threshold, 1);
  return (
    <View style={styles.achievement}>
      <View style={styles.achievementHeading}>
        <Text style={styles.achievementTitle}>{achievement.title}</Text>
        <Text style={achievement.isEarned ? styles.earned : styles.progressLabel}>
          {achievement.isEarned ? 'Conquistada' : `${achievement.currentValue}/${achievement.threshold}`}
        </Text>
      </View>
      <Text style={styles.body}>{achievement.description}</Text>
      <View accessibilityLabel={`Progresso: ${Math.round(progress * 100)}%`} style={styles.progressTrack}>
        <View style={[styles.progressFill, { width: `${Math.round(progress * 100)}%` }]} />
      </View>
      {achievement.status === 'revoked' ? (
        <Text style={styles.caption}>Revogada após uma alteração no histórico.</Text>
      ) : null}
      {achievement.isMetadataIncomplete ? (
        <Text style={styles.caption}>O progresso pode mudar quando os metadados de gêneros estiverem disponíveis.</Text>
      ) : null}
      {achievement.firstEarnedAt ? (
        <Text style={styles.caption}>
          Primeira conquista em {new Date(achievement.firstEarnedAt).toLocaleDateString('pt-BR')}.
        </Text>
      ) : null}
    </View>
  );
}

function PreferenceToggle({
  label,
  description,
  value,
  disabled,
  onChange,
}: {
  label: string;
  description: string;
  value: boolean;
  disabled: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <View style={styles.toggleRow}>
      <View style={styles.toggleCopy}>
        <Text style={styles.toggleLabel}>{label}</Text>
        <Text style={styles.caption}>{description}</Text>
      </View>
      <Switch
        accessibilityLabel={label}
        disabled={disabled}
        onValueChange={onChange}
        thumbColor={colors.white}
        trackColor={{ false: colors.surfaceElevated, true: colors.brand }}
        value={value}
      />
    </View>
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
  backButton: {
    minHeight: 44,
    justifyContent: 'center',
  },
  backLabel: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.extraBold,
  },
  section: {
    gap: spacing.medium,
    paddingTop: spacing.large,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  statistics: {
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  statLine: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  body: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    lineHeight: 22,
    fontFamily: fontFamily.regular,
  },
  caption: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    lineHeight: 18,
    fontFamily: fontFamily.regular,
  },
  achievement: {
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  achievementHeading: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
  },
  achievementTitle: {
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
  toggleRow: {
    minHeight: 72,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.medium,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  toggleCopy: {
    flex: 1,
    gap: spacing.extraSmall,
    paddingVertical: spacing.small,
  },
  toggleLabel: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  ranking: {
    gap: spacing.small,
    paddingTop: spacing.medium,
  },
  rankingTitle: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  rankingRow: {
    minHeight: 44,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  rankingName: {
    flex: 1,
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.medium,
  },
  rankingCount: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
});
