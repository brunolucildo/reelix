import { useMutation, useQueryClient } from '@tanstack/react-query';

import { saveProfile } from '@/features/profile/api/profile-api';
import { profileQueryKeys } from '@/features/profile/query-keys';
import { ratingsQueryKeys } from '@/features/ratings/query-keys';
import { socialQueryKeys } from '@/features/social/query-keys';
import { useToast } from '@/providers/toast-provider';

/** Saves the profile, writes the result to the cache and refreshes what depends on it (social, reviews). */
export function useSaveProfile() {
  const queryClient = useQueryClient();
  const { showError } = useToast();

  return useMutation({
    mutationFn: saveProfile,
    onSuccess: (saved) => {
      queryClient.setQueryData(profileQueryKeys.profile, saved);
      void queryClient.invalidateQueries({ queryKey: socialQueryKeys.all });
      void queryClient.invalidateQueries({ queryKey: ratingsQueryKeys.all });
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível salvar o perfil.');
    },
  });
}
