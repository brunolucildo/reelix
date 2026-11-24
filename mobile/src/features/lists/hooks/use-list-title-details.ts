import { useQueries } from '@tanstack/react-query';
import { useEffect } from 'react';

import { catalogQueryKeys } from '@/features/catalog/query-keys';
import { fetchTitleDetails, type TitleReference } from '@/features/lists/api/lists-api';
import { staleTime } from '@/lib/query/stale-time';
import { useToast } from '@/providers/toast-provider';

/** Catalog details for each saved reference (same order), with one toast when any fails. */
export function useListTitleDetails(references: TitleReference[]) {
  const { showError } = useToast();
  const results = useQueries({
    queries: references.map((reference) => ({
      queryKey: catalogQueryKeys.title(reference.provider, reference.contentType, reference.externalId),
      queryFn: () => fetchTitleDetails(reference),
      staleTime: staleTime.catalog,
    })),
  });
  const failed = results.some((result) => result.isError);
  useEffect(() => {
    if (failed) {
      showError('Não foi possível carregar os detalhes de alguns títulos. Tente novamente.');
    }
  }, [failed, showError]);
  return results;
}
