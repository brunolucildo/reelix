import { useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import * as Notifications from 'expo-notifications';
import { useEffect } from 'react';

import { markNotificationRead } from '@/features/notifications/api/notifications-api';
import { notificationsQueryKeys } from '@/features/notifications/query-keys';

/**
 * While signed in, opens the screen a tapped push notification points to (cold start included)
 * and marks that notification as read.
 */
export function useOpenNotification(enabled: boolean) {
  const router = useRouter();
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const openNotification = (response: Notifications.NotificationResponse) => {
      const data = response.notification.request.content.data;
      if (!data) {
        return;
      }

      const notificationId = data.notificationId;
      if (typeof notificationId === 'string') {
        void markNotificationRead(notificationId)
          .then(async () => {
            await Promise.all([
              queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.inbox }),
              queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.unreadCount }),
            ]);
          })
          .catch((error: unknown) => {
            console.warn('Unable to mark the opened notification as read.', error);
          });
      }

      const deepLink = data.deepLink;
      if (deepLink === '/social' || deepLink === '/notifications') {
        router.push(deepLink);
      }
    };
    const subscription = Notifications.addNotificationResponseReceivedListener(openNotification);
    void Notifications.getLastNotificationResponseAsync()
      .then((response) => {
        if (response) {
          openNotification(response);
        }
      })
      .catch((error: unknown) => {
        console.warn('Unable to read the last notification response.', error);
      });

    return () => subscription.remove();
  }, [enabled, queryClient, router]);
}
