import { z } from 'zod';

import { supabase } from '@/services/supabase';

const apiUrl = process.env.EXPO_PUBLIC_API_URL?.trim();

if (!apiUrl) {
  throw new Error('Configure EXPO_PUBLIC_API_URL in mobile/.env before starting the app.');
}

const parsedApiUrl = new URL(apiUrl);
if (
  parsedApiUrl.protocol !== 'https:' &&
  !(parsedApiUrl.protocol === 'http:' && ['localhost', '127.0.0.1', '10.0.2.2'].includes(parsedApiUrl.hostname))
) {
  throw new Error('EXPO_PUBLIC_API_URL must use HTTPS except for a local development server.');
}

const accountSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email().nullable(),
  createdAt: z.string().datetime({ offset: true }),
});

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

const genreSchema = z.object({
  id: z.number().int().positive(),
  name: z.string().min(1),
});

const catalogTitleSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  overview: z.string().nullable(),
  releaseDate: z.string().nullable(),
  posterUrl: z.string().url().nullable(),
});

const catalogSearchSchema = z.object({
  items: z.array(catalogTitleSchema),
});

const profileSchema = z.object({
  displayName: z.string().nullable(),
  biography: z.string().nullable(),
  avatarUrl: z.string().url().nullable(),
  profileIsPublic: z.boolean(),
  favoritesArePublic: z.boolean(),
  watchHistoryIsPublic: z.boolean(),
});

export type AuthenticatedAccount = z.infer<typeof accountSchema>;
export type OnboardingPreferences = z.infer<typeof onboardingSchema>;
export type CatalogGenre = z.infer<typeof genreSchema>;
export type CatalogTitle = z.infer<typeof catalogTitleSchema>;
export type ProfileData = z.infer<typeof profileSchema>;

export type OnboardingProgress = Omit<OnboardingPreferences, 'isComplete' | 'currentStep'> & {
  currentStep: Exclude<OnboardingPreferences['currentStep'], 'completed'>;
};

async function apiRequest<T>(
  path: string,
  schema: z.ZodType<T>,
  init?: RequestInit,
): Promise<T> {
  const { data, error } = await supabase.auth.getSession();
  if (error) {
    throw error;
  }

  if (!data.session) {
    throw new Error('Sua sessão expirou. Entre novamente para continuar.');
  }

  const response = await fetch(new URL(path, apiUrl), {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      Authorization: `Bearer ${data.session.access_token}`,
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let message = 'Não foi possível concluir a solicitação. Tente novamente.';
    try {
      const problem = await response.json() as { detail?: string; title?: string };
      message = problem.detail ?? problem.title ?? message;
    } catch {
      message = response.status === 401
        ? 'Sua sessão expirou. Entre novamente para continuar.'
        : message;
    }
    throw Object.assign(new Error(message), { status: response.status });
  }

  const result = schema.safeParse(await response.json());
  if (!result.success) {
    throw new Error('A API retornou dados em formato inesperado.');
  }
  return result.data;
}

export async function fetchAuthenticatedAccount(): Promise<AuthenticatedAccount> {
  return apiRequest('/api/v1/auth/me', accountSchema);
}

export async function fetchOnboarding(): Promise<OnboardingPreferences> {
  return apiRequest('/api/v1/onboarding', onboardingSchema);
}

export async function saveOnboardingProgress(
  progress: OnboardingProgress,
): Promise<OnboardingPreferences> {
  return apiRequest('/api/v1/onboarding/progress', onboardingSchema, {
    method: 'PUT',
    body: JSON.stringify(progress),
  });
}

export async function completeOnboarding(): Promise<OnboardingPreferences> {
  return apiRequest('/api/v1/onboarding/complete', onboardingSchema, { method: 'POST' });
}

export async function fetchCatalogGenres(type: 'movie' | 'series'): Promise<CatalogGenre[]> {
  return apiRequest(
    `/api/v1/catalog/genres?type=${type}&provider=tmdb&language=pt-BR`,
    z.array(genreSchema),
  );
}

export async function searchCatalog(
  query: string,
  type: 'movie' | 'series',
): Promise<CatalogTitle[]> {
  const search = new URLSearchParams({ query, type, provider: 'tmdb', language: 'pt-BR' });
  const result = await apiRequest(`/api/v1/catalog/search?${search}`, catalogSearchSchema);
  return result.items;
}

export async function fetchProfile(): Promise<ProfileData> {
  return apiRequest('/api/v1/profile', profileSchema);
}

export async function saveProfile(
  profile: Omit<ProfileData, 'avatarUrl'>,
): Promise<ProfileData> {
  return apiRequest('/api/v1/profile', profileSchema, {
    method: 'PUT',
    body: JSON.stringify(profile),
  });
}
