import { useQueries } from '@tanstack/react-query';
import { useMemo } from 'react';

import {
  fetchCatalogSeason,
  fetchCatalogSeasons,
  fetchCatalogTitleDetails,
} from '@/features/catalog/api/catalog-api';
import { catalogQueryKeys } from '@/features/catalog/query-keys';
import type { TrackedSeriesPage } from '@/features/tracking/api/tracking-api';
import { useTrackedSeries } from '@/features/tracking/hooks/use-tracking';
import { trackingQueryKeys } from '@/features/tracking/query-keys';
import { localDateKey } from '@/lib/format';
import { staleTime } from '@/lib/query/stale-time';
import { useErrorToast } from '@/providers/toast-provider';

export type CalendarFilter = 'all' | 'premiere' | 'final';

export type CalendarEpisode = {
  series: TrackedSeriesPage['items'][number];
  seasonNumber: number;
  episodeNumber: number | null;
  episodeTitle: string;
  releaseDate: string;
  episodeId: string;
  lastEpisodeNumber: number | null;
};

/** Upcoming episodes of the tracked series (latest and future seasons), filtered for the Home calendar. */
export function useUpcomingCalendar(filter: CalendarFilter) {
  const tracked = useTrackedSeries(1, 12);
  const series = tracked.data?.items ?? [];
  const titleQueries = useQueries({
    queries: series.map((item) => ({
      queryKey: catalogQueryKeys.title(item.provider, 'series', item.externalId),
      queryFn: ({ signal }: { signal: AbortSignal }) =>
        fetchCatalogTitleDetails(item.provider!, 'series', item.externalId!, signal),
      enabled: Boolean(item.provider && item.externalId),
      staleTime: staleTime.catalog,
    })),
  });
  const seasonListQueries = useQueries({
    queries: series.map((item) => ({
      queryKey: trackingQueryKeys.calendarSeasons(item.seriesId),
      queryFn: ({ signal }: { signal: AbortSignal }) =>
        fetchCatalogSeasons(item.provider!, item.externalId!, signal),
      enabled: Boolean(item.provider && item.externalId),
      staleTime: staleTime.catalog,
    })),
  });
  const today = localDateKey(new Date());
  const seasonEntries = series.flatMap((item, index) => {
    if (!item.provider || !item.externalId) return [];
    const availableSeasons = seasonListQueries[index]?.data ?? [];
    const regularSeasons = availableSeasons.filter((season) => season.seasonNumber > 0);
    const latestSeasons = regularSeasons.slice(-2);
    const upcomingSeasons = availableSeasons.filter(
      (season) => season.releaseDate !== null && season.releaseDate >= today,
    );
    const selectedSeasons = new Map(
      [...latestSeasons, ...upcomingSeasons].map((season) => [season.seasonNumber, season]),
    );
    return Array.from(selectedSeasons.values())
      .sort((left, right) => left.seasonNumber - right.seasonNumber)
      .map((season) => ({ item, seasonNumber: season.seasonNumber }));
  });
  const seasonQueries = useQueries({
    queries: seasonEntries.map(({ item, seasonNumber }) => ({
      queryKey: trackingQueryKeys.calendarSeason(item.seriesId, seasonNumber),
      queryFn: ({ signal }: { signal: AbortSignal }) =>
        fetchCatalogSeason(item.provider!, item.externalId!, seasonNumber, signal),
      staleTime: staleTime.catalog,
    })),
  });
  const titleBySeries = new Map(
    series.flatMap((item, index) => {
      const title = titleQueries[index]?.data;
      return title ? [[item.seriesId, title] as const] : [];
    }),
  );
  const episodes = useMemo(() => {
    const todayKey = localDateKey(new Date());
    const deduplicated = new Map<string, CalendarEpisode>();
    seasonQueries.forEach((result, index) => {
      if (!result.data) return;
      const entry = seasonEntries[index];
      if (!entry) return;
      const datedFutureEpisodes = result.data.episodes.filter(
        (episode) => episode.releaseDate !== null && episode.releaseDate >= todayKey,
      );
      const lastEpisodeNumber = Math.max(
        0,
        ...result.data.episodes.map((episode) => episode.episodeNumber ?? 0),
      ) || null;
      for (const episode of datedFutureEpisodes) {
        deduplicated.set(episode.id, {
          series: entry.item,
          seasonNumber: result.data.seasonNumber,
          episodeNumber: episode.episodeNumber,
          episodeTitle: episode.title,
          releaseDate: episode.releaseDate!,
          episodeId: episode.id,
          lastEpisodeNumber,
        });
      }
    });
    return Array.from(deduplicated.values())
      .sort((left, right) => left.releaseDate.localeCompare(right.releaseDate));
  }, [seasonEntries, seasonQueries]);
  const visibleEpisodes = episodes.filter((episode) => {
    if (filter === 'premiere') return episode.episodeNumber === 1;
    if (filter === 'final') return episode.episodeNumber === episode.lastEpisodeNumber;
    return true;
  });
  const seasonError = seasonListQueries.find((result) => result.error)?.error ??
    seasonQueries.find((result) => result.error)?.error;
  const titleError = titleQueries.find((result) => result.error)?.error;

  useErrorToast(tracked.error, 'Não foi possível carregar suas séries para o calendário.');
  useErrorToast(seasonError, 'Não foi possível carregar alguns episódios do calendário.');
  useErrorToast(titleError, 'Não foi possível carregar algumas imagens do calendário.');

  const loading = tracked.isPending ||
    seasonListQueries.some(
      (result, index) =>
        Boolean(series[index]?.provider && series[index]?.externalId) && result.isPending,
    ) ||
    seasonQueries.some((result) => result.isPending);
  const failed = tracked.isError ||
    seasonListQueries.some((result) => result.isError) ||
    seasonQueries.some((result) => result.isError);

  const retry = () => {
    if (tracked.isError) void tracked.refetch();
    seasonListQueries.forEach((result) => {
      if (result.isError) void result.refetch();
    });
    seasonQueries.forEach((result) => {
      if (result.isError) void result.refetch();
    });
  };

  return { episodes, visibleEpisodes, titleBySeries, loading, failed, retry };
}
