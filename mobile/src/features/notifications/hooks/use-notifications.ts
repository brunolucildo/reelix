import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import {
  fetchNotificationInbox,
  fetchNotificationPreferences,
  markNotificationRead,
  saveNotificationPreferences,
} from '@/features/notifications/api/notifications-api';
import { notificationsQueryKeys } from '@/features/notifications/query-keys';
import { getNextCursor } from '@/lib/pagination';
import { useViewerId } from '@/providers/auth-provider';
import { useErrorToast, useToast } from '@/providers/toast-provider';
import { registerCurrentPushDevice } from '@/services/push-registration';

/** Preferences, inbox and the actions of the notifications screen (save, mark read, enable push). */
export function useNotifications() {
  const queryClient = useQueryClient();
  const viewerId = useViewerId();
  const { showError } = useToast();
  const [feedback, setFeedback] = useState<string | null>(null);
  const preferences = useQuery({
    queryKey: notificationsQueryKeys.preferences,
    queryFn: fetchNotificationPreferences,
  });
  const inbox = useInfiniteQuery({
    queryKey: notificationsQueryKeys.inbox,
    initialPageParam: undefined as string | undefined,
    queryFn: ({ pageParam, signal }) => fetchNotificationInbox(pageParam, signal),
    getNextPageParam: getNextCursor,
  });
  useErrorToast(preferences.error, 'Não foi possível carregar as preferências de notificações.');
  useErrorToast(inbox.error, 'Não foi possível carregar a caixa de entrada.');

  const savePreferences = useMutation({
    mutationFn: saveNotificationPreferences,
    onSuccess: (saved) => {
      queryClient.setQueryData(notificationsQueryKeys.preferences, saved);
      setFeedback('Preferências atualizadas.');
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível salvar as preferências.');
    },
  });
  const markRead = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.inbox }),
        queryClient.invalidateQueries({ queryKey: notificationsQueryKeys.unreadCount }),
      ]);
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível atualizar a notificação.');
    },
  });
  const enablePush = useMutation({
    mutationFn: async () => {
      if (!viewerId) {
        throw new Error('Entre novamente para ativar notificações neste dispositivo.');
      }
      await registerCurrentPushDevice(viewerId);
    },
    onSuccess: () => setFeedback('Este dispositivo foi registrado para receber notificações push.'),
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível ativar notificações push.');
    },
  });

  return {
    preferences,
    inbox,
    items: inbox.data?.pages.flatMap((page) => page.items) ?? [],
    savePreferences,
    markRead,
    enablePush,
    feedback,
    clearFeedback: () => setFeedback(null),
  };
}
