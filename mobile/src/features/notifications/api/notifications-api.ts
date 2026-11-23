import { z } from 'zod';

import { httpRequest } from '@/lib/http-client';

const notificationPreferencesSchema = z.object({
  socialEnabled: z.boolean(),
  newEpisodeEnabled: z.boolean(),
  premiereEnabled: z.boolean(),
});

const notificationInboxItemSchema = z.object({
  id: z.string().uuid(),
  category: z.enum(['social', 'newEpisode', 'premiere']),
  title: z.string(),
  body: z.string(),
  deepLink: z.enum(['/social', '/notifications']),
  createdAt: z.string().datetime({ offset: true }),
  readAt: z.string().datetime({ offset: true }).nullable(),
});

const notificationInboxPageSchema = z.object({
  items: z.array(notificationInboxItemSchema),
  nextCursor: z.string().nullable(),
});

const notificationUnreadCountSchema = z.object({
  count: z.number().int().nonnegative(),
});

const notificationDeviceSchema = z.object({
  id: z.string().uuid(),
  platform: z.enum(['ios', 'android']),
  createdAt: z.string().datetime({ offset: true }),
  lastRegisteredAt: z.string().datetime({ offset: true }),
});

export type NotificationPreferences = z.infer<typeof notificationPreferencesSchema>;

export type NotificationInboxItem = z.infer<typeof notificationInboxItemSchema>;

export type NotificationInboxPage = z.infer<typeof notificationInboxPageSchema>;

export async function fetchNotificationPreferences(): Promise<NotificationPreferences> {
  return httpRequest('/api/v1/notifications/preferences', notificationPreferencesSchema);
}

export async function saveNotificationPreferences(
  preferences: NotificationPreferences,
): Promise<NotificationPreferences> {
  return httpRequest('/api/v1/notifications/preferences', notificationPreferencesSchema, {
    method: 'PUT',
    body: preferences,
  });
}

export async function registerNotificationDevice(registration: {
  installationId: string;
  expoPushToken: string;
  platform: 'ios' | 'android';
  projectId: string;
}): Promise<z.infer<typeof notificationDeviceSchema>> {
  return httpRequest('/api/v1/notifications/devices', notificationDeviceSchema, {
    method: 'POST',
    body: registration,
  });
}

export async function deactivateNotificationDevice(deviceId: string): Promise<void> {
  await httpRequest(
    `/api/v1/notifications/devices/${deviceId}`,
    z.void(),
    { method: 'DELETE' },
  );
}

export async function fetchNotificationInbox(
  cursor?: string,
  signal?: AbortSignal,
): Promise<NotificationInboxPage> {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : '';
  return httpRequest(`/api/v1/notifications/inbox${query}`, notificationInboxPageSchema, { signal });
}

export async function fetchNotificationUnreadCount(
  signal?: AbortSignal,
): Promise<{ count: number }> {
  return httpRequest(
    '/api/v1/notifications/inbox/unread-count',
    notificationUnreadCountSchema,
    { signal },
  );
}

export async function markNotificationRead(notificationId: string): Promise<void> {
  await httpRequest(
    `/api/v1/notifications/inbox/${notificationId}/read`,
    z.void(),
    { method: 'PUT' },
  );
}
