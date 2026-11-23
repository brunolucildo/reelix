import { z } from 'zod';

import { httpRequest } from '@/lib/http-client';
export * from '@/features/account/api/account-api';
export * from '@/features/onboarding/api/onboarding-api';
export * from '@/features/catalog/api/catalog-api';
export * from '@/features/profile/api/profile-api';
export * from '@/features/social/api/social-api';
export * from '@/features/notifications/api/notifications-api';
export * from '@/features/gamification/api/gamification-api';

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

export type ReviewTargetType = z.infer<typeof reviewTargetTypeSchema>;

export type ReviewAudience = z.infer<typeof reviewAudienceSchema>;

export type RatingSummary = z.infer<typeof ratingSummarySchema>;

export type CommentItem = z.infer<typeof commentItemSchema>;

export type PagedComments = z.infer<typeof pagedCommentsSchema>;

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
