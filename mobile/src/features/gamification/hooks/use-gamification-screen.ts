import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo } from 'react';

import {
  fetchGamificationStatistics,
  fetchMonthlyFriendsRanking,
  saveGamificationPreferences,
  type GamificationDashboard,
} from '@/features/gamification/api/gamification-api';
import { useGamificationDashboard } from '@/features/gamification/hooks/use-gamification-dashboard';
import { gamificationQueryKeys } from '@/features/gamification/query-keys';
import { useTrackingHistory } from '@/features/tracking/hooks/use-tracking';
import { useHistoryTitles } from '@/features/tracking/hooks/use-tracking-titles';
import { formatMonthYear } from '@/lib/format';
import { getNextCursor } from '@/lib/pagination';
import { useErrorToast, useToast } from '@/providers/toast-provider';

/** Data of the gamification screen: dashboard, current-month statistics, recent activity, ranking and preferences. */
export function useGamificationScreen() {
  const queryClient = useQueryClient();
  const { showError } = useToast();
  const dashboard = useGamificationDashboard();
  const now = new Date();
  const year = now.getUTCFullYear();
  const month = now.getUTCMonth() + 1;
  const periodStart = new Date(Date.UTC(year, month - 1, 1));
  const periodEnd = new Date(Date.UTC(year, month, 1));
  const periodStatistics = useQuery({
    queryKey: gamificationQueryKeys.statistics(periodStart.toISOString(), periodEnd.toISOString()),
    queryFn: ({ signal }) => fetchGamificationStatistics(periodStart, periodEnd, signal),
  });
  const activityHistory = useTrackingHistory(1, 20);
  const activityTitles = useHistoryTitles(
    activityHistory.data?.items ?? [],
    'Não foi possível carregar alguns títulos das atividades.',
  );
  const activityTitleById = new Map(
    (activityHistory.data?.items ?? []).flatMap((activity, index) => {
      const title = activityTitles[index]?.data;
      return title ? [[activity.id, title] as const] : [];
    }),
  );
  const activityGroups = useMemo(
    () => groupActivitiesByMonth(activityHistory.data?.items ?? []),
    [activityHistory.data?.items],
  );
  const ranking = useInfiniteQuery({
    queryKey: gamificationQueryKeys.ranking(year, month),
    initialPageParam: undefined as string | undefined,
    queryFn: ({ pageParam, signal }) =>
      fetchMonthlyFriendsRanking(year, month, pageParam, signal),
    getNextPageParam: getNextCursor,
    enabled: dashboard.data?.preferences.rankingEnabled === true,
  });
  const preferencesMutation = useMutation({
    mutationFn: saveGamificationPreferences,
    onSuccess: (preferences) => {
      queryClient.setQueryData(
        gamificationQueryKeys.dashboard,
        (current: GamificationDashboard | undefined) => current
          ? { ...current, preferences }
          : current,
      );
      void queryClient.invalidateQueries({ queryKey: gamificationQueryKeys.rankingAll });
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível salvar as preferências.');
      void queryClient.invalidateQueries({ queryKey: gamificationQueryKeys.dashboard });
    },
  });

  useErrorToast(periodStatistics.error, 'Não foi possível carregar as estatísticas do período.');
  useErrorToast(activityHistory.error, 'Não foi possível carregar o histórico de atividades.');
  useErrorToast(ranking.error, 'Não foi possível carregar o ranking entre amigos.');

  return {
    dashboard,
    monthLabel: formatMonthYear(now),
    periodStatistics,
    activityHistory,
    activityTitleById,
    activityGroups,
    ranking,
    preferencesMutation,
  };
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
        label: formatMonthYear(date),
        items: [],
      };
      groups.set(key, group);
    }
    group.items.push(activity);
  }
  return Array.from(groups.values());
}
