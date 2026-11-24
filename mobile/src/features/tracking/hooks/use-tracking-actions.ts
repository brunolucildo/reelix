import {
  useMarkEpisodeWatched,
  useMarkMovieWatched,
  useRemoveWatchedEpisode,
  useRemoveWatchedMovie,
} from '@/features/tracking/hooks/use-tracking';

/** The four tracking mutations plus a flag that is true while any of them is in flight. */
export function useTrackingActions() {
  const markMovie = useMarkMovieWatched();
  const removeMovie = useRemoveWatchedMovie();
  const markEpisode = useMarkEpisodeWatched();
  const removeEpisode = useRemoveWatchedEpisode();
  const isSaving = markMovie.isPending || removeMovie.isPending ||
    markEpisode.isPending || removeEpisode.isPending;

  return { markMovie, removeMovie, markEpisode, removeEpisode, isSaving };
}

export type TrackingActions = ReturnType<typeof useTrackingActions>;
