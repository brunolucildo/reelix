import { z } from 'zod';

import type { ProfileData } from '@/features/profile/api/profile-api';

export const DISPLAY_NAME_MAX = 50;
export const BIOGRAPHY_MAX = 280;

export const profileFormSchema = z.object({
  displayName: z.string().max(DISPLAY_NAME_MAX, `O nome pode ter no máximo ${DISPLAY_NAME_MAX} caracteres.`),
  biography: z.string().max(BIOGRAPHY_MAX, `A biografia pode ter no máximo ${BIOGRAPHY_MAX} caracteres.`),
  avatarKey: z.string(),
  profileIsPublic: z.boolean(),
  favoritesArePublic: z.boolean(),
  watchHistoryIsPublic: z.boolean(),
  isDiscoverable: z.boolean(),
  watchHistorySharedWithFriends: z.boolean(),
  favoritesSharedWithFriends: z.boolean(),
  ratingsSharedWithFriends: z.boolean(),
});

export type ProfileFormValues = z.infer<typeof profileFormSchema>;

/** Form values for a saved profile (empty text instead of null, "initials" avatar by default). */
export function toProfileFormValues(profile: ProfileData): ProfileFormValues {
  return {
    displayName: profile.displayName ?? '',
    biography: profile.biography ?? '',
    avatarKey: profile.avatarKey ?? 'initials',
    profileIsPublic: profile.profileIsPublic,
    favoritesArePublic: profile.favoritesArePublic,
    watchHistoryIsPublic: profile.watchHistoryIsPublic,
    isDiscoverable: profile.isDiscoverable,
    watchHistorySharedWithFriends: profile.watchHistorySharedWithFriends,
    favoritesSharedWithFriends: profile.favoritesSharedWithFriends,
    ratingsSharedWithFriends: profile.ratingsSharedWithFriends,
  };
}
