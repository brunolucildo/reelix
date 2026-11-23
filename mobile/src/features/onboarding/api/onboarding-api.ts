import { z } from 'zod';

import { httpRequest } from '@/lib/http-client';

const onboardingSchema = z.object({
  isComplete: z.boolean(),
  currentStep: z.enum([
    'welcome',
    'genres',
    'favoriteMovies',
    'favoriteSeries',
    'review',
    'completed',
  ]),
  movieGenreIds: z.array(z.number().int().positive()),
  seriesGenreIds: z.array(z.number().int().positive()),
  favoriteMovieIds: z.array(z.string().uuid()),
  favoriteSeriesIds: z.array(z.string().uuid()),
});

export type OnboardingPreferences = z.infer<typeof onboardingSchema>;

export type OnboardingProgress = Omit<OnboardingPreferences, 'isComplete' | 'currentStep'> & {
  currentStep: Exclude<OnboardingPreferences['currentStep'], 'completed'>;
};

export async function fetchOnboarding(): Promise<OnboardingPreferences> {
  return httpRequest('/api/v1/onboarding', onboardingSchema);
}

export async function saveOnboardingProgress(
  progress: OnboardingProgress,
): Promise<OnboardingPreferences> {
  return httpRequest('/api/v1/onboarding/progress', onboardingSchema, {
    method: 'PUT',
    body: progress,
  });
}

export async function completeOnboarding(): Promise<OnboardingPreferences> {
  return httpRequest('/api/v1/onboarding/complete', onboardingSchema, { method: 'POST' });
}
