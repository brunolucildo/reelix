import { z } from 'zod';

import { apiUrl } from '@/services/api-url';
import { supabase } from '@/services/supabase';

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

export const catalogTitleSchema = z.object({
  id: z.string().uuid(),
  provider: z.enum(['tmdb', 'omdb']),
  externalId: z.string().min(1),
  contentType: z.enum(['movie', 'series']),
  title: z.string(),
  originalTitle: z.string().nullable(),
  overview: z.string().nullable(),
  genres: z.array(z.string()),
  releaseDate: z.string().nullable(),
  originalLanguage: z.string().nullable(),
  runtimeMinutes: z.number().int().positive().nullable(),
  posterUrl: z.string().url().nullable(),
  backdropUrl: z.string().url().nullable(),
});

const catalogSearchSchema = z.object({
  items: z.array(catalogTitleSchema),
});

const catalogDiscoverySchema = z.object({
  items: z.array(catalogTitleSchema),
  page: z.number().int().positive(),
  totalPages: z.number().int().positive(),
  totalResults: z.number().int().nonnegative(),
  provider: z.enum(['tmdb', 'omdb']),
  contentType: z.enum(['movie', 'series']),
  category: z.enum(['popular', 'upcoming']),
});

const profileSchema = z.object({
  displayName: z.string().nullable(),
  biography: z.string().nullable(),
  avatarUrl: z.string().url().nullable(),
  avatarKey: z.string().nullable().optional(),
  profileIsPublic: z.boolean(),
  favoritesArePublic: z.boolean(),
  watchHistoryIsPublic: z.boolean(),
  isDiscoverable: z.boolean(),
  watchHistorySharedWithFriends: z.boolean(),
  favoritesSharedWithFriends: z.boolean(),
  ratingsSharedWithFriends: z.boolean(),
});

const friendRelationshipStatusSchema = z.enum([
  'pending',
  'accepted',
  'declined',
  'cancelled',
  'removed',
]);
const socialUserSchema = z.object({
  accountId: z.string().uuid(),
  displayName: z.string(),
  relationshipStatus: friendRelationshipStatusSchema.nullable(),
  relationshipId: z.string().uuid().nullable(),
  requestSentByMe: z.boolean(),
  avatarUrl: z.string().url().nullable().optional(),
});
const friendRequestSchema = z.object({
  relationshipId: z.string().uuid(),
  accountId: z.string().uuid(),
  displayName: z.string(),
  createdAt: z.string().datetime({ offset: true }),
  avatarUrl: z.string().url().nullable().optional(),
});
const friendItemSchema = z.object({
  relationshipId: z.string().uuid(),
  accountId: z.string().uuid(),
  displayName: z.string(),
  since: z.string().datetime({ offset: true }),
  avatarUrl: z.string().url().nullable().optional(),
});
const socialListPageSchema = <T extends z.ZodType>(itemSchema: T) => z.object({
  items: z.array(itemSchema),
  nextCursor: z.string().nullable(),
});
const socialActivitySchema = z.object({
  id: z.string().uuid(),
  actorAccountId: z.string().uuid(),
  actorDisplayName: z.string(),
  actorAvatarKey: z.string().nullable().optional(),
  activityType: z.string(),
  targetId: z.string().uuid(),
  targetType: z.enum(['movie', 'series', 'episode']),
  provider: z.enum(['tmdb', 'omdb']),
  externalId: z.string().min(1),
  occurredAt: z.string().datetime({ offset: true }),
});
const socialActivityPageSchema = z.object({
  items: z.array(socialActivitySchema),
  nextCursor: z.string().nullable(),
});
const friendProfileSchema = z.object({
  accountId: z.string().uuid(),
  displayName: z.string(),
  biography: z.string().nullable(),
  avatarUrl: z.string().url().nullable().optional(),
  avatarKey: z.string().nullable().optional(),
  relationshipStatus: friendRelationshipStatusSchema.nullable(),
  relationshipId: z.string().uuid().nullable(),
  requestSentByMe: z.boolean(),
  isSelf: z.boolean(),
  favoritesShared: z.boolean(),
  watchHistoryShared: z.boolean(),
  ratingsShared: z.boolean(),
  favorites: z.array(z.object({
    titleId: z.string().uuid(),
    contentType: z.enum(['movie', 'series']),
    provider: z.enum(['tmdb', 'omdb']),
    externalId: z.string().min(1),
  })),
});
const notificationPreferencesSchema = z.object({
  socialEnabled: z.boolean(),
  newEpisodeEnabled: z.boolean(),
  premiereEnabled: z.boolean(),
});
const notificationInboxItemSchema = z.object({
  id: z.string().uuid(),
  category: z.enum(['social', 'newEpisode', 'premiere']),
  title: z.string(),
  body: z.string(),
  deepLink: z.enum(['/social', '/notifications']),
  createdAt: z.string().datetime({ offset: true }),
  readAt: z.string().datetime({ offset: true }).nullable(),
});
const notificationInboxPageSchema = z.object({
  items: z.array(notificationInboxItemSchema),
  nextCursor: z.string().nullable(),
});
const notificationUnreadCountSchema = z.object({
  count: z.number().int().nonnegative(),
});
const notificationDeviceSchema = z.object({
  id: z.string().uuid(),
  platform: z.enum(['ios', 'android']),
  createdAt: z.string().datetime({ offset: true }),
  lastRegisteredAt: z.string().datetime({ offset: true }),
});
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
  return apiRequest(`/api/v1/ratings/mine?page=${page}&pageSize=20`, ownRatingsPageSchema, { signal });
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
const catalogSeasonSchema = z.object({
  id: z.string().uuid(),
  seriesId: z.string().uuid(),
  provider: z.enum(['tmdb', 'omdb']),
  externalSeriesId: z.string(),
  title: z.string().nullable(),
  seasonNumber: z.number().int().nonnegative(),
  releaseDate: z.string().nullable(),
  episodes: z.array(z.object({
    id: z.string().uuid(),
    externalId: z.string(),
    episodeNumber: z.number().int().nullable(),
    stillUrl: z.string().url().nullable().optional(),
    title: z.string(),
    overview: z.string().nullable(),
    releaseDate: z.string().nullable(),
    runtimeMinutes: z.number().int().positive().nullable(),
  })),
});
const catalogSeasonSummarySchema = z.object({
  seasonNumber: z.number().int().nonnegative(),
  title: z.string().nullable(),
  releaseDate: z.string().nullable(),
});

export type AuthenticatedAccount = z.infer<typeof accountSchema>;
export type OnboardingPreferences = z.infer<typeof onboardingSchema>;
export type CatalogGenre = z.infer<typeof genreSchema>;
export type CatalogTitle = z.infer<typeof catalogTitleSchema>;
export type CatalogDiscoveryPage = z.infer<typeof catalogDiscoverySchema>;
export type CatalogDiscoveryCategory = CatalogDiscoveryPage['category'];
export type ProfileData = z.infer<typeof profileSchema>;
export type SocialUser = z.infer<typeof socialUserSchema>;
export type FriendRequestData = z.infer<typeof friendRequestSchema>;
export type FriendItemData = z.infer<typeof friendItemSchema>;
export type SocialActivityData = z.infer<typeof socialActivitySchema>;
export type FriendProfileData = z.infer<typeof friendProfileSchema>;
export type NotificationPreferences = z.infer<typeof notificationPreferencesSchema>;
export type NotificationInboxItem = z.infer<typeof notificationInboxItemSchema>;
export type NotificationInboxPage = z.infer<typeof notificationInboxPageSchema>;
export type GamificationPreferences = z.infer<typeof gamificationPreferencesSchema>;
export type AchievementProgress = z.infer<typeof achievementProgressSchema>;
export type GamificationStatistics = z.infer<typeof gamificationStatisticsSchema>;
export type GamificationDashboard = z.infer<typeof gamificationDashboardSchema>;
export type PublicBadges = z.infer<typeof publicBadgesSchema>;
export type GamificationRanking = z.infer<typeof gamificationRankingSchema>;
export type SocialPage<T> = {
  items: T[];
  nextCursor: string | null;
};
export type SocialActivityPage = z.infer<typeof socialActivityPageSchema>;
export type ReviewTargetType = z.infer<typeof reviewTargetTypeSchema>;
export type ReviewAudience = z.infer<typeof reviewAudienceSchema>;
export type RatingSummary = z.infer<typeof ratingSummarySchema>;
export type CommentItem = z.infer<typeof commentItemSchema>;
export type PagedComments = z.infer<typeof pagedCommentsSchema>;
export type CatalogSeason = z.infer<typeof catalogSeasonSchema>;
export type CatalogSeasonSummary = z.infer<typeof catalogSeasonSummarySchema>;

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

  const result = schema.safeParse(
    response.status === 204 ? undefined : await response.json(),
  );
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
  signal?: AbortSignal,
): Promise<CatalogTitle[]> {
  const search = new URLSearchParams({ query, type, provider: 'tmdb', language: 'pt-BR' });
  const result = await apiRequest(`/api/v1/catalog/search?${search}`, catalogSearchSchema, { signal });
  return result.items;
}

export async function fetchCatalogDiscovery(
  type: 'movie' | 'series',
  category: CatalogDiscoveryCategory,
  genreIds: number[],
  page: number,
  signal?: AbortSignal,
): Promise<CatalogDiscoveryPage> {
  const search = new URLSearchParams({
    type,
    section: category,
    page: String(page),
    provider: 'tmdb',
    language: 'pt-BR',
  });
  for (const genreId of genreIds) {
    search.append('genreIds', String(genreId));
  }
  return apiRequest(`/api/v1/catalog/discover?${search}`, catalogDiscoverySchema, { signal });
}

export async function fetchCatalogTitleDetails(
  provider: CatalogTitle['provider'],
  type: CatalogTitle['contentType'],
  externalId: string,
  signal?: AbortSignal,
): Promise<CatalogTitle> {
  return apiRequest(
    `/api/v1/catalog/${provider}/${type}/${encodeURIComponent(externalId)}?language=pt-BR`,
    catalogTitleSchema,
    { signal },
  );
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

export async function searchSocialUsers(
  query: string,
  signal?: AbortSignal,
): Promise<SocialUser[]> {
  const search = new URLSearchParams({ query, limit: '20' });
  return apiRequest(`/api/v1/social/users?${search}`, z.array(socialUserSchema), { signal });
}

export async function sendFriendRequest(accountId: string): Promise<{ relationshipId: string }> {
  return apiRequest('/api/v1/social/friend-requests', z.object({
    relationshipId: z.string().uuid(),
    status: friendRelationshipStatusSchema,
  }), {
    method: 'POST',
    body: JSON.stringify({ accountId }),
  });
}

export async function fetchFriends(
  cursor?: string,
  signal?: AbortSignal,
): Promise<SocialPage<FriendItemData>> {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : '';
  return apiRequest(`/api/v1/social/friends${query}`, socialListPageSchema(friendItemSchema), { signal });
}

export async function fetchFriendRequests(
  direction: 'incoming' | 'outgoing',
  cursor?: string,
  signal?: AbortSignal,
): Promise<SocialPage<FriendRequestData>> {
  const query = new URLSearchParams({ direction, ...(cursor ? { cursor } : {}) });
  return apiRequest(
    `/api/v1/social/friend-requests?${query.toString()}`,
    socialListPageSchema(friendRequestSchema),
    { signal },
  );
}

export async function respondToFriendRequest(
  relationshipId: string,
  response: 'accept' | 'decline',
): Promise<void> {
  await apiRequest(
    `/api/v1/social/friend-requests/${relationshipId}/${response}`,
    z.object({
      relationshipId: z.string().uuid(),
      status: friendRelationshipStatusSchema,
    }),
    { method: 'PUT' },
  );
}

export async function cancelFriendRequest(relationshipId: string): Promise<void> {
  await apiRequest(
    `/api/v1/social/friend-requests/${relationshipId}`,
    z.object({
      relationshipId: z.string().uuid(),
      status: friendRelationshipStatusSchema,
    }),
    { method: 'DELETE' },
  );
}

export async function removeFriend(relationshipId: string): Promise<void> {
  await apiRequest(
    `/api/v1/social/friends/${relationshipId}`,
    z.object({
      relationshipId: z.string().uuid(),
      status: friendRelationshipStatusSchema,
    }),
    { method: 'DELETE' },
  );
}

export async function fetchFriendProfile(
  accountId: string,
  signal?: AbortSignal,
): Promise<FriendProfileData> {
  return apiRequest(
    `/api/v1/social/friends/${accountId}/profile`,
    friendProfileSchema,
    { signal },
  );
}

export async function fetchSocialFeed(
  cursor?: string,
  signal?: AbortSignal,
): Promise<SocialActivityPage> {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : '';
  return apiRequest(`/api/v1/social/feed${query}`, socialActivityPageSchema, { signal });
}

export async function fetchFriendActivities(
  accountId: string,
  cursor?: string,
  signal?: AbortSignal,
): Promise<SocialActivityPage> {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : '';
  return apiRequest(`/api/v1/social/friends/${accountId}/activities${query}`, socialActivityPageSchema, { signal });
}

export async function fetchNotificationPreferences(): Promise<NotificationPreferences> {
  return apiRequest('/api/v1/notifications/preferences', notificationPreferencesSchema);
}

export async function saveNotificationPreferences(
  preferences: NotificationPreferences,
): Promise<NotificationPreferences> {
  return apiRequest('/api/v1/notifications/preferences', notificationPreferencesSchema, {
    method: 'PUT',
    body: JSON.stringify(preferences),
  });
}

export async function registerNotificationDevice(registration: {
  installationId: string;
  expoPushToken: string;
  platform: 'ios' | 'android';
  projectId: string;
}): Promise<z.infer<typeof notificationDeviceSchema>> {
  return apiRequest('/api/v1/notifications/devices', notificationDeviceSchema, {
    method: 'POST',
    body: JSON.stringify(registration),
  });
}

export async function deactivateNotificationDevice(deviceId: string): Promise<void> {
  await apiRequest(
    `/api/v1/notifications/devices/${deviceId}`,
    z.void(),
    { method: 'DELETE' },
  );
}

export async function fetchNotificationInbox(
  cursor?: string,
  signal?: AbortSignal,
): Promise<NotificationInboxPage> {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : '';
  return apiRequest(`/api/v1/notifications/inbox${query}`, notificationInboxPageSchema, { signal });
}

export async function fetchNotificationUnreadCount(
  signal?: AbortSignal,
): Promise<{ count: number }> {
  return apiRequest(
    '/api/v1/notifications/inbox/unread-count',
    notificationUnreadCountSchema,
    { signal },
  );
}

export async function markNotificationRead(notificationId: string): Promise<void> {
  await apiRequest(
    `/api/v1/notifications/inbox/${notificationId}/read`,
    z.void(),
    { method: 'PUT' },
  );
}

export async function fetchGamificationDashboard(
  signal?: AbortSignal,
): Promise<GamificationDashboard> {
  return apiRequest('/api/v1/gamification/dashboard', gamificationDashboardSchema, { signal });
}

export async function saveGamificationPreferences(
  preferences: Pick<GamificationPreferences, 'badgesPublic' | 'rankingEnabled'>,
): Promise<GamificationPreferences> {
  return apiRequest('/api/v1/gamification/preferences', gamificationPreferencesSchema, {
    method: 'PUT',
    body: JSON.stringify(preferences),
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
  return apiRequest(`/api/v1/gamification/statistics?${query}`, gamificationStatisticsSchema, { signal });
}

export async function fetchPublicGamificationBadges(
  accountId: string,
  signal?: AbortSignal,
): Promise<PublicBadges> {
  return apiRequest(
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
  return apiRequest(`/api/v1/gamification/friends/ranking?${query}`, gamificationRankingSchema, { signal });
}

export async function fetchRatingSummary(
  targetType: ReviewTargetType,
  targetId: string,
  seriesId?: string,
  signal?: AbortSignal,
): Promise<RatingSummary> {
  const query = seriesId ? `?seriesId=${encodeURIComponent(seriesId)}` : '';
  return apiRequest(
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
  return apiRequest(
    `/api/v1/ratings/${targetType}/${targetId}`,
    ratingSummarySchema,
    {
      method: 'PUT',
      body: JSON.stringify({ score, audience, ...(seriesId ? { seriesId } : {}) }),
    },
  );
}

export async function deleteRating(
  targetType: ReviewTargetType,
  targetId: string,
): Promise<void> {
  return apiRequest(
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
  return apiRequest(
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
  return apiRequest(
    `/api/v1/comments/${targetType}/${targetId}`,
    commentItemSchema,
    {
      method: 'POST',
      body: JSON.stringify(comment),
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
  return apiRequest(
    `/api/v1/comments/${commentId}`,
    commentItemSchema,
    {
      method: 'PUT',
      body: JSON.stringify(comment),
    },
  );
}

export async function deleteComment(commentId: string): Promise<void> {
  return apiRequest(`/api/v1/comments/${commentId}`, z.void(), { method: 'DELETE' });
}

export async function revealComment(commentId: string): Promise<string> {
  const result = await apiRequest(
    `/api/v1/comments/${commentId}/reveal`,
    revealedCommentSchema,
    { method: 'POST' },
  );
  return result.content;
}

export async function fetchCatalogSeason(
  provider: CatalogTitle['provider'],
  externalSeriesId: string,
  seasonNumber: number,
  signal?: AbortSignal,
): Promise<CatalogSeason> {
  return apiRequest(
    `/api/v1/catalog/${provider}/series/${encodeURIComponent(externalSeriesId)}/seasons/${seasonNumber}?language=pt-BR`,
    catalogSeasonSchema,
    { signal },
  );
}

export async function fetchCatalogSeasons(
  provider: CatalogTitle['provider'],
  externalSeriesId: string,
  signal?: AbortSignal,
): Promise<CatalogSeasonSummary[]> {
  return apiRequest(
    `/api/v1/catalog/${provider}/series/${encodeURIComponent(externalSeriesId)}/seasons?language=pt-BR`,
    z.array(catalogSeasonSummarySchema),
    { signal },
  );
}
