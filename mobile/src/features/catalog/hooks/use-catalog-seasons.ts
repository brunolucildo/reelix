import { useQuery } from '@tanstack/react-query';

import {
  fetchCatalogSeason,
  fetchCatalogSeasons,
  type CatalogProvider,
} from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

/** Season list of a series. `enabled` lets the caller wait for the title details first. */
export function useCatalogSeasons(
  provider: CatalogProvider | null,
  externalSeriesId: string | null,
  enabled: boolean,
) {
  const query = useQuery({
    queryKey: catalogQueryKeys.seasons(provider, externalSeriesId),
    queryFn: ({ signal }) => {
      if (!provider || !externalSeriesId) {
        throw new Error('O endereço desta série é inválido.');
      }

      return fetchCatalogSeasons(provider, externalSeriesId, signal);
    },
    enabled: provider !== null && externalSeriesId !== null && enabled,
    staleTime: staleTime.catalog,
  });
  useErrorToast(query.error, 'Não foi possível carregar as temporadas disponíveis.');
  return query;
}

type SeasonOptions = {
  provider: CatalogProvider | null;
  externalSeriesId: string | null;
  seasonNumber: number | null;
  enabled?: boolean;
  /** Thrown (and shown in the toast fallback) when a route param is invalid. */
  invalidMessage: string;
  errorMessage: string;
};

/** Episodes of one season; the title and episode screens differ only in their messages. */
export function useCatalogSeason({
  provider,
  externalSeriesId,
  seasonNumber,
  enabled = true,
  invalidMessage,
  errorMessage,
}: SeasonOptions) {
  const query = useQuery({
    queryKey: catalogQueryKeys.season(provider, externalSeriesId, seasonNumber),
    queryFn: ({ signal }) => {
      if (!provider || !externalSeriesId || seasonNumber === null) {
        throw new Error(invalidMessage);
      }

      return fetchCatalogSeason(provider, externalSeriesId, seasonNumber, signal);
    },
    enabled: provider !== null && externalSeriesId !== null && seasonNumber !== null && enabled,
    staleTime: staleTime.catalog,
  });
  useErrorToast(query.error, errorMessage);
  return query;
}
