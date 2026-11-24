import { useQuery } from '@tanstack/react-query';

import { fetchOnboarding } from '@/features/onboarding/api/onboarding-api';
import { onboardingQueryKeys } from '@/features/onboarding/query-keys';
import { toUserMessage } from '@/lib/api-error';
import { useErrorToast } from '@/providers/toast-provider';

/**
 * Saved onboarding preferences. The Home and the onboarding screen share this cache entry and
 * differ only in the toast text; `translateError` runs the error through `toUserMessage` first.
 */
export function useOnboardingPreferences(fallbackMessage: string, translateError = false) {
  const query = useQuery({
    queryKey: onboardingQueryKeys.preferences,
    queryFn: fetchOnboarding,
  });
  useErrorToast(
    query.error,
    translateError ? toUserMessage(query.error, fallbackMessage) : fallbackMessage,
  );
  return query;
}
