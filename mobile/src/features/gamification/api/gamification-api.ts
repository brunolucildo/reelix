import { z } from 'zod';

import { httpRequest } from '@/lib/http-client';

const gamificationPreferencesSchema = z.object({
  badgesPublic: z.boolean(),
  rankingEnabled: z.boolean(),
  updatedAt: z.string().datetime({ offset: true }),
});

const achievementProgressSchema = z.object({
  code: z.string(),
  title: z.string(),
  description: z.string(),
  currentValue: z.number().int().nonnegative(),
  threshold: z.number().int().positive(),
  isEarned: z.boolean(),
  isMetadataIncomplete: z.boolean(),
  status: z.enum(['earned', 'revoked']).nullable(),
  firstEarnedAt: z.string().datetime({ offset: true }).nullable(),
  lastRevokedAt: z.string().datetime({ offset: true }).nullable(),
  reactivatedAt: z.string().datetime({ offset: true }).nullable(),
});

const gamificationStatisticsSchema = z.object({
  periodStart: z.string().datetime({ offset: true }),
  periodEnd: z.string().datetime({ offset: true }),
  moviesWatched: z.number().int().nonnegative(),
  episodesWatched: z.number().int().nonnegative(),
  seriesExplored: z.number().int().nonnegative(),
  genresExplored: z.number().int().nonnegative(),
  moviesMissingGenreMetadata: z.number().int().nonnegative(),
  itemsWithRuntime: z.number().int().nonnegative(),
  itemsWithoutRuntime: z.number().int().nonnegative(),
  estimatedWatchMinutes: z.number().int().positive().nullable(),
  estimatedWatchHours: z.number().positive().nullable(),
  runtimeSources: z.array(z.string()),
  runtimeMetadataVersion: z.number().int().positive().nullable(),
  runtimeMetadataUpdatedAt: z.string().datetime({ offset: true }).nullable(),
  confirmedWatchMinutes: z.number().int().nonnegative().nullable(),
  calculatedAt: z.string().datetime({ offset: true }),
  runtimeCoverage: z.number().min(0).max(1),
});

const gamificationDashboardSchema = z.object({
  preferences: gamificationPreferencesSchema,
  lifetimeStatistics: gamificationStatisticsSchema,
  achievements: z.array(achievementProgressSchema),
});

const publicBadgesSchema = z.object({
  isPublic: z.boolean(),
  badges: z.array(z.object({
    code: z.string(),
    title: z.string(),
    description: z.string(),
    firstEarnedAt: z.string().datetime({ offset: true }),
  })),
});

const gamificationRankingSchema = z.object({
  isEnabled: z.boolean(),
  periodStart: z.string().datetime({ offset: true }),
  periodEnd: z.string().datetime({ offset: true }),
  totalParticipants: z.number().int().nonnegative(),
  items: z.array(z.object({
    position: z.number().int().positive(),
    accountId: z.string().uuid(),
    displayName: z.string(),
    moviesWatched: z.number().int().positive(),
  })),
  nextCursor: z.string().nullable(),
});

export type GamificationPreferences = z.infer<typeof gamificationPreferencesSchema>;

export type AchievementProgress = z.infer<typeof achievementProgressSchema>;

export type GamificationStatistics = z.infer<typeof gamificationStatisticsSchema>;

export type GamificationDashboard = z.infer<typeof gamificationDashboardSchema>;

export type PublicBadges = z.infer<typeof publicBadgesSchema>;

export type GamificationRanking = z.infer<typeof gamificationRankingSchema>;

export async function fetchGamificationDashboard(
  signal?: AbortSignal,
): Promise<GamificationDashboard> {
  return httpRequest('/api/v1/gamification/dashboard', gamificationDashboardSchema, { signal });
}

export async function saveGamificationPreferences(
  preferences: Pick<GamificationPreferences, 'badgesPublic' | 'rankingEnabled'>,
): Promise<GamificationPreferences> {
  return httpRequest('/api/v1/gamification/preferences', gamificationPreferencesSchema, {
    method: 'PUT',
    body: preferences,
  });
}

export async function fetchGamificationStatistics(
  start: Date,
  end: Date,
  signal?: AbortSignal,
): Promise<GamificationStatistics> {
  const query = new URLSearchParams({
    start: start.toISOString(),
    end: end.toISOString(),
  });
  return httpRequest(`/api/v1/gamification/statistics?${query}`, gamificationStatisticsSchema, { signal });
}

export async function fetchPublicGamificationBadges(
  accountId: string,
  signal?: AbortSignal,
): Promise<PublicBadges> {
  return httpRequest(
    `/api/v1/gamification/users/${accountId}/badges`,
    publicBadgesSchema,
    { signal },
  );
}

export async function fetchMonthlyFriendsRanking(
  year: number,
  month: number,
  cursor?: string,
  signal?: AbortSignal,
): Promise<GamificationRanking> {
  const query = new URLSearchParams({
    year: String(year),
    month: String(month),
    limit: '20',
    ...(cursor ? { cursor } : {}),
  });
  return httpRequest(`/api/v1/gamification/friends/ranking?${query}`, gamificationRankingSchema, { signal });
}
