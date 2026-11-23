import { z } from 'zod';

import { httpRequest } from '@/lib/http-client';
export * from '@/features/account/api/account-api';
export * from '@/features/onboarding/api/onboarding-api';
export * from '@/features/catalog/api/catalog-api';
export * from '@/features/profile/api/profile-api';
export * from '@/features/social/api/social-api';
export * from '@/features/notifications/api/notifications-api';

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

const reviewTargetTypeSchema = z.enum(['movie', 'series', 'episode']);

const reviewAudienceSchema = z.enum(['private', 'public']);

const commentModerationStatusSchema = z.enum(['pending', 'published', 'blocked']);

const ownRatingSchema = z.object({
  id: z.string().uuid(),
  score: z.number().min(0).max(5),
  audience: reviewAudienceSchema,
  createdAt: z.string().datetime({ offset: true }),
  updatedAt: z.string().datetime({ offset: true }),
});

const ratingSummarySchema = z.object({
  targetId: z.string().uuid(),
  targetType: reviewTargetTypeSchema,
  averageScore: z.number().min(0).max(5).nullable(),
  publicCount: z.number().int().nonnegative(),
  myRating: ownRatingSchema.nullable(),
});

const ownRatingsPageSchema = z.object({
  items: z.array(z.object({
    id: z.string().uuid(),
    targetId: z.string().uuid(),
    targetType: reviewTargetTypeSchema,
    score: z.number().min(0).max(5),
    audience: reviewAudienceSchema,
    updatedAt: z.string().datetime({ offset: true }),
    provider: z.enum(['tmdb', 'omdb']),
    externalId: z.string(),
    seriesExternalId: z.string().nullable(),
  })),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
});

export type OwnRatingEntry = z.infer<typeof ownRatingsPageSchema>['items'][number];

export function fetchOwnRatings(page: number, signal?: AbortSignal) {
  return httpRequest(`/api/v1/ratings/mine?page=${page}&pageSize=20`, ownRatingsPageSchema, { signal });
}

const commentItemSchema = z.object({
  id: z.string().uuid(),
  targetId: z.string().uuid(),
  targetType: reviewTargetTypeSchema,
  seriesId: z.string().uuid().nullable(),
  authorName: z.string(),
  authorAvatarKey: z.string().nullable().optional(),
  content: z.string().nullable(),
  containsSpoiler: z.boolean(),
  possibleSpoiler: z.boolean(),
  audience: reviewAudienceSchema,
  moderationStatus: commentModerationStatusSchema,
  isOwn: z.boolean(),
  createdAt: z.string().datetime({ offset: true }),
  updatedAt: z.string().datetime({ offset: true }),
});

const pagedCommentsSchema = z.object({
  items: z.array(commentItemSchema),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
});

const revealedCommentSchema = z.object({
  id: z.string().uuid(),
  content: z.string(),
});

export type GamificationPreferences = z.infer<typeof gamificationPreferencesSchema>;

export type AchievementProgress = z.infer<typeof achievementProgressSchema>;

export type GamificationStatistics = z.infer<typeof gamificationStatisticsSchema>;

export type GamificationDashboard = z.infer<typeof gamificationDashboardSchema>;

export type PublicBadges = z.infer<typeof publicBadgesSchema>;

export type GamificationRanking = z.infer<typeof gamificationRankingSchema>;

export type ReviewTargetType = z.infer<typeof reviewTargetTypeSchema>;

export type ReviewAudience = z.infer<typeof reviewAudienceSchema>;

export type RatingSummary = z.infer<typeof ratingSummarySchema>;

export type CommentItem = z.infer<typeof commentItemSchema>;

export type PagedComments = z.infer<typeof pagedCommentsSchema>;

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

export async function fetchRatingSummary(
  targetType: ReviewTargetType,
  targetId: string,
  seriesId?: string,
  signal?: AbortSignal,
): Promise<RatingSummary> {
  const query = seriesId ? `?seriesId=${encodeURIComponent(seriesId)}` : '';
  return httpRequest(
    `/api/v1/ratings/${targetType}/${targetId}${query}`,
    ratingSummarySchema,
    { signal },
  );
}

export async function saveRating(
  targetType: ReviewTargetType,
  targetId: string,
  score: number,
  audience: ReviewAudience,
  seriesId?: string,
): Promise<RatingSummary> {
  return httpRequest(
    `/api/v1/ratings/${targetType}/${targetId}`,
    ratingSummarySchema,
    {
      method: 'PUT',
      body: { score, audience, ...(seriesId ? { seriesId } : {}) },
    },
  );
}

export async function deleteRating(
  targetType: ReviewTargetType,
  targetId: string,
): Promise<void> {
  return httpRequest(
    `/api/v1/ratings/${targetType}/${targetId}`,
    z.void(),
    { method: 'DELETE' },
  );
}

export async function fetchComments(
  targetType: ReviewTargetType,
  targetId: string,
  page: number,
  seriesId?: string,
  signal?: AbortSignal,
): Promise<PagedComments> {
  const query = new URLSearchParams({ page: String(page), pageSize: '20' });
  if (seriesId) {
    query.set('seriesId', seriesId);
  }
  return httpRequest(
    `/api/v1/comments/${targetType}/${targetId}?${query}`,
    pagedCommentsSchema,
    { signal },
  );
}

export async function createComment(
  targetType: ReviewTargetType,
  targetId: string,
  comment: {
    content: string;
    containsSpoiler: boolean;
    audience: ReviewAudience;
    requestAutomatedAnalysis: boolean;
    seriesId?: string;
  },
): Promise<CommentItem> {
  return httpRequest(
    `/api/v1/comments/${targetType}/${targetId}`,
    commentItemSchema,
    {
      method: 'POST',
      body: comment,
    },
  );
}

export async function updateComment(
  commentId: string,
  comment: {
    content: string;
    containsSpoiler: boolean;
    audience: ReviewAudience;
    requestAutomatedAnalysis: boolean;
  },
): Promise<CommentItem> {
  return httpRequest(
    `/api/v1/comments/${commentId}`,
    commentItemSchema,
    {
      method: 'PUT',
      body: comment,
    },
  );
}

export async function deleteComment(commentId: string): Promise<void> {
  return httpRequest(`/api/v1/comments/${commentId}`, z.void(), { method: 'DELETE' });
}

export async function revealComment(commentId: string): Promise<string> {
  const result = await httpRequest(
    `/api/v1/comments/${commentId}/reveal`,
    revealedCommentSchema,
    { method: 'POST' },
  );
  return result.content;
}
