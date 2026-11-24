import { useQuery } from '@tanstack/react-query';

import { fetchNotificationUnreadCount } from '@/features/notifications/api/notifications-api';
import { notificationsQueryKeys } from '@/features/notifications/query-keys';
import { useErrorToast } from '@/providers/toast-provider';

/** Unread notifications for the header badge, refreshed every minute. */
export function useUnreadCount() {
  const unreadCount = useQuery({
    queryKey: notificationsQueryKeys.unreadCount,
    queryFn: ({ signal }) => fetchNotificationUnreadCount(signal),
    refetchInterval: 60_000,
  });
  useErrorToast(unreadCount.error, 'Não foi possível carregar o contador de notificações.');
  return unreadCount.data?.count ?? 0;
}
