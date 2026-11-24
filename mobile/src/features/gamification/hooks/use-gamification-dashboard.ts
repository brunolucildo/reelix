import { useQuery } from '@tanstack/react-query';

import { fetchGamificationDashboard } from '@/features/gamification/api/gamification-api';
import { gamificationQueryKeys } from '@/features/gamification/query-keys';
import { useErrorToast } from '@/providers/toast-provider';

/** Achievements dashboard shared by the drawer, the profile and the gamification screen. */
export function useGamificationDashboard(enabled = true) {
  const dashboard = useQuery({
    queryKey: gamificationQueryKeys.dashboard,
    queryFn: ({ signal }) => fetchGamificationDashboard(signal),
    enabled,
  });
  useErrorToast(dashboard.error, 'Não foi possível carregar suas conquistas.');
  return dashboard;
}
