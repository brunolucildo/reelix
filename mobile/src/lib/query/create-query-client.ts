import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';

import { isUnauthorizedError } from '@/lib/api-error';
import { staleTime } from '@/lib/query/stale-time';

/**
 * Creates the app QueryClient. A 401 from any query or mutation is reported once to
 * `onUnauthorized`, which ends the local session; 401 is never retried.
 */
export function createQueryClient(onUnauthorized: () => void): QueryClient {
  const handleError = (error: unknown) => {
    if (isUnauthorizedError(error)) {
      // Deferred so the cache is not cleared from inside its own error callback.
      setTimeout(onUnauthorized, 0);
    }
  };

  return new QueryClient({
    queryCache: new QueryCache({ onError: handleError }),
    mutationCache: new MutationCache({ onError: handleError }),
    defaultOptions: {
      queries: {
        retry: (failureCount, error) => !isUnauthorizedError(error) && failureCount < 1,
        staleTime: staleTime.default,
      },
    },
  });
}
