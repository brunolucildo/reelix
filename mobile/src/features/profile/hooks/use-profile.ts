import { useQuery } from '@tanstack/react-query';

import { fetchProfile } from '@/features/profile/api/profile-api';
import { profileQueryKeys } from '@/features/profile/query-keys';
import { useErrorToast } from '@/providers/toast-provider';

/** The viewer's profile; the drawer and the profile screen share it and differ only in the toast text. */
export function useProfile(errorMessage: string, enabled = true) {
  const profile = useQuery({ queryKey: profileQueryKeys.profile, queryFn: fetchProfile, enabled });
  useErrorToast(profile.error, errorMessage);
  return profile;
}
