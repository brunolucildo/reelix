import { z } from 'zod';

import { httpRequest } from '@/lib/http-client';

const profileSchema = z.object({
  displayName: z.string().nullable(),
  biography: z.string().nullable(),
  avatarUrl: z.string().url().nullable(),
  avatarKey: z.string().nullable().optional(),
  profileIsPublic: z.boolean(),
  favoritesArePublic: z.boolean(),
  watchHistoryIsPublic: z.boolean(),
  isDiscoverable: z.boolean(),
  watchHistorySharedWithFriends: z.boolean(),
  favoritesSharedWithFriends: z.boolean(),
  ratingsSharedWithFriends: z.boolean(),
});

export type ProfileData = z.infer<typeof profileSchema>;

export async function fetchProfile(): Promise<ProfileData> {
  return httpRequest('/api/v1/profile', profileSchema);
}

export async function saveProfile(
  profile: Omit<ProfileData, 'avatarUrl'>,
): Promise<ProfileData> {
  return httpRequest('/api/v1/profile', profileSchema, {
    method: 'PUT',
    body: profile,
  });
}
