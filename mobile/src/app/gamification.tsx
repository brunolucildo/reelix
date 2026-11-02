import { useInfiniteQuery, useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query';
import { Image } from 'expo-image';
import { useMemo } from 'react';
import {
  ActivityIndicator,
  ScrollView,
  StyleSheet,
  Switch,
  Text,
  View,
} from 'react-native';

import { PrimaryButton } from '@/components/auth/primary-button';
import { BackButton } from '@/components/navigation/back-button';
import { useTrackingHistory } from '@/hooks/use-tracking';
import { useErrorToast, useToast } from '@/providers/toast-provider';
import {
  fetchGamificationDashboard,
  fetchGamificationStatistics,
  fetchMonthlyFriendsRanking,
  fetchCatalogTitleDetails,
  saveGamificationPreferences,
  type AchievementProgress,
  type GamificationStatistics,
} from '@/services/api-client';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export default function GamificationScreen() {
  const queryClient = useQueryClient();
  const { showError } = useToast();
  const dashboard = useQuery({
    queryKey: ['gamification', 'dashboard'],
    queryFn: ({ signal }) => fetchGamificationDashboard(signal),
  });
  const now = new Date();
  const year = now.getUTCFullYear();
  const month = now.getUTCMonth() + 1;
  const periodStart = new Date(Date.UTC(year, month - 1, 1));
  const periodEnd = new Date(Date.UTC(year, month, 1));
  const periodStatistics = useQuery({
    queryKey: ['gamification', 'statistics', periodStart.toISOString(), periodEnd.toISOString()],
    queryFn: ({ signal }) => fetchGamificationStatistics(periodStart, periodEnd, signal),
  });
  const activityHistory = useTrackingHistory(1, 20);
  const activityTitleQueries = useQueries({
    queries: (activityHistory.data?.items ?? []).map((activity) => {
      const contentType = activity.type === 'movie' ? 'movie' : 'series';
      const externalId = activity.type === 'movie' ? activity.externalId : activity.seriesExternalId;
      return {
        queryKey: ['gamification', 'activity-title', activity.id, activity.provider, externalId],
        queryFn: ({ signal }: { signal: AbortSignal }) => {
          if (!activity.provider || !externalId) {
            throw new Error('Os detalhes deste conteúdo não estão disponíveis.');
          }
          return fetchCatalogTitleDetails(activity.provider, contentType, externalId, signal);
        },
        enabled: Boolean(activity.provider && externalId),
        staleTime: 300_000,
      };
    }),
  });
  const activityTitleById = new Map(
    (activityHistory.data?.items ?? []).flatMap((activity, index) => {
      const title = activityTitleQueries[index]?.data;
      return title ? [[activity.id, title] as const] : [];
    }),
  );
  const activityGroups = useMemo(
    () => groupActivitiesByMonth(activityHistory.data?.items ?? []),
    [activityHistory.data?.items],
  );
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

  useErrorToast(dashboard.error, dashboard.error && 'status' in dashboard.error && dashboard.error.status === 401
    ? 'Sua sessão expirou. Entre novamente para continuar.'
    : 'Não foi possível carregar suas conquistas.');
  useErrorToast(periodStatistics.error, 'Não foi possível carregar as estatísticas do período.');
  useErrorToast(activityHistory.error, 'Não foi possível carregar o histórico de atividades.');
  useErrorToast(
    activityTitleQueries.find((result) => result.error)?.error,
    'Não foi possível carregar alguns títulos das atividades.',
  );
  useErrorToast(ranking.error, 'Não foi possível carregar o ranking entre amigos.');

  if (dashboard.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.body}>Carregando conquistas...</Text>
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

  const preferences = dashboard.data.preferences;

  return (
    <ScrollView contentContainerStyle={styles.content}>
      <View style={styles.container}>
        <BackButton />
        <Text accessibilityRole="header" style={styles.title}>Conquistas e estatísticas</Text>
        {dashboard.isError ? (
          <PrimaryButton label="Atualizar conquistas" onPress={() => void dashboard.refetch()} variant="secondary" />
        ) : null}
        <Text style={styles.body}>
          Progresso calculado a partir dos seus registros de acompanhamento.
        </Text>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Sua atividade total</Text>
          <StatisticsSummary statistics={dashboard.data.lifetimeStatistics} />
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>
            Este mês · {new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' }).format(now)}
          </Text>
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
          <Text style={styles.sectionTitle}>Atividade recente</Text>
          {activityHistory.isPending ? (
            <ActivityIndicator color={colors.brand} />
          ) : activityHistory.isError && !activityHistory.data ? (
            <PrimaryButton
              label="Tentar carregar atividades"
              onPress={() => void activityHistory.refetch()}
              variant="secondary"
            />
          ) : activityGroups.length === 0 ? (
            <Text style={styles.body}>Ainda não há atividades registradas.</Text>
          ) : (
            activityGroups.map((group) => (
              <View key={group.key} style={styles.activityGroup}>
                <Text style={styles.activityMonth}>{group.label}</Text>
                {group.items.map((item) => (
                  <View key={item.id} style={styles.activityRow}>
                    <Text style={styles.activityIcon}>{item.type === 'movie' ? '▶' : '✓'}</Text>
                    <View style={styles.activityCopy}>
                      <Text style={styles.activityLabel}>
                        {activityTitleById.get(item.id)?.title ??
                          (item.type === 'movie' ? 'Filme assistido' : 'Episódio assistido')}
                      </Text>
                      <Text style={styles.caption}>
                        {item.type === 'movie' ? 'Filme assistido' : 'Episódio assistido'}
                        {' · '}
                        {new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium', timeStyle: 'short' })
                          .format(new Date(item.watchedAt))}
                      </Text>
                    </View>
                    {activityTitleById.get(item.id)?.posterUrl ? (
                      <Image
                        contentFit="cover"
                        source={{ uri: activityTitleById.get(item.id)?.posterUrl ?? undefined }}
                        style={styles.activityPoster}
                      />
                    ) : null}
                  </View>
                ))}
              </View>
            ))
          )}
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Conquistas</Text>
          {dashboard.data.achievements.length === 0 ? <Text style={styles.body}>Ainda não há conquistas disponíveis.</Text> : null}
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
      <View style={styles.statGrid}>
        <StatTile label="Episódios" value={statistics.episodesWatched} icon="✓" />
        <StatTile label="Séries" value={statistics.seriesExplored} icon="▣" />
        <StatTile label="Filmes" value={statistics.moviesWatched} icon="▶" />
        <StatTile
          label="Registros"
          value={statistics.moviesWatched + statistics.episodesWatched}
          icon="◷"
        />
      </View>
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

function StatTile({ label, value, icon }: { label: string; value: number; icon: string }) {
  return (
    <View style={styles.statTile}>
      <Text style={styles.statIcon}>{icon}</Text>
      <Text style={styles.statValue}>{value}</Text>
      <Text style={styles.statLabel}>{label}</Text>
    </View>
  );
}

function groupActivitiesByMonth<T extends { id: string; type: 'movie' | 'episode'; watchedAt: string }>(
  activities: T[],
) {
  const groups = new Map<string, { key: string; label: string; items: T[] }>();
  for (const activity of activities) {
    const date = new Date(activity.watchedAt);
    const key = `${date.getFullYear()}-${date.getMonth()}`;
    let group = groups.get(key);
    if (!group) {
      group = {
        key,
        label: new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' }).format(date),
        items: [],
      };
      groups.set(key, group);
    }
    group.items.push(activity);
  }
  return Array.from(groups.values());
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
  statGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.small,
  },
  statTile: {
    flexGrow: 1,
    flexBasis: '45%',
    minHeight: 92,
    gap: spacing.extraSmall,
    justifyContent: 'center',
    padding: spacing.small,
    borderRadius: radii.small,
    backgroundColor: colors.surfaceElevated,
  },
  statIcon: { color: colors.brandSecondary, fontSize: typography.body, fontFamily: fontFamily.bold },
  statValue: { color: colors.textPrimary, fontSize: typography.section, fontFamily: fontFamily.bold },
  statLabel: { color: colors.textSecondary, fontSize: typography.caption, fontFamily: fontFamily.medium },
  statLine: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  activityGroup: {
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  activityMonth: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
    textTransform: 'capitalize',
  },
  activityRow: {
    minHeight: 56,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.medium,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  activityIcon: {
    width: 32,
    color: colors.brandSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  activityPoster: {
    width: 42,
    height: 62,
    borderRadius: radii.small,
    backgroundColor: colors.surfaceElevated,
  },
  activityCopy: { flex: 1, gap: spacing.extraSmall },
  activityLabel: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
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
