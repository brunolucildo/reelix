import { z } from 'zod';

import type { OnboardingPreferences } from '@/features/onboarding/api/onboarding-api';

export const MAX_GENRES = 20;
export const MAX_FAVORITES = 10;

/** What the viewer picks along the steps; the limits mirror the notices shown when they are reached. */
export const onboardingSelectionSchema = z.object({
  movieGenreIds: z.array(z.number().int().positive()).max(MAX_GENRES),
  seriesGenreIds: z.array(z.number().int().positive()).max(MAX_GENRES),
  favoriteMovieIds: z.array(z.string()).max(MAX_FAVORITES),
  favoriteSeriesIds: z.array(z.string()).max(MAX_FAVORITES),
});

export type OnboardingSelection = z.infer<typeof onboardingSelectionSchema>;

export function toSelection(preferences: OnboardingPreferences): OnboardingSelection {
  return {
    movieGenreIds: preferences.movieGenreIds,
    seriesGenreIds: preferences.seriesGenreIds,
    favoriteMovieIds: preferences.favoriteMovieIds,
    favoriteSeriesIds: preferences.favoriteSeriesIds,
  };
}
