import { z } from 'zod';

import { httpRequest } from '@/lib/http-client';

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

export type SocialUser = z.infer<typeof socialUserSchema>;

export type FriendRequestData = z.infer<typeof friendRequestSchema>;

export type FriendItemData = z.infer<typeof friendItemSchema>;

export type SocialActivityData = z.infer<typeof socialActivitySchema>;

export type FriendProfileData = z.infer<typeof friendProfileSchema>;

export type SocialPage<T> = {
  items: T[];
  nextCursor: string | null;
};

export type SocialActivityPage = z.infer<typeof socialActivityPageSchema>;

export async function searchSocialUsers(
  query: string,
  signal?: AbortSignal,
): Promise<SocialUser[]> {
  const search = new URLSearchParams({ query, limit: '20' });
  return httpRequest(`/api/v1/social/users?${search}`, z.array(socialUserSchema), { signal });
}

export async function sendFriendRequest(accountId: string): Promise<{ relationshipId: string }> {
  return httpRequest('/api/v1/social/friend-requests', z.object({
    relationshipId: z.string().uuid(),
    status: friendRelationshipStatusSchema,
  }), {
    method: 'POST',
    body: { accountId },
  });
}

export async function fetchFriends(
  cursor?: string,
  signal?: AbortSignal,
): Promise<SocialPage<FriendItemData>> {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : '';
  return httpRequest(`/api/v1/social/friends${query}`, socialListPageSchema(friendItemSchema), { signal });
}

export async function fetchFriendRequests(
  direction: 'incoming' | 'outgoing',
  cursor?: string,
  signal?: AbortSignal,
): Promise<SocialPage<FriendRequestData>> {
  const query = new URLSearchParams({ direction, ...(cursor ? { cursor } : {}) });
  return httpRequest(
    `/api/v1/social/friend-requests?${query.toString()}`,
    socialListPageSchema(friendRequestSchema),
    { signal },
  );
}

export async function respondToFriendRequest(
  relationshipId: string,
  response: 'accept' | 'decline',
): Promise<void> {
  await httpRequest(
    `/api/v1/social/friend-requests/${relationshipId}/${response}`,
    z.object({
      relationshipId: z.string().uuid(),
      status: friendRelationshipStatusSchema,
    }),
    { method: 'PUT' },
  );
}

export async function cancelFriendRequest(relationshipId: string): Promise<void> {
  await httpRequest(
    `/api/v1/social/friend-requests/${relationshipId}`,
    z.object({
      relationshipId: z.string().uuid(),
      status: friendRelationshipStatusSchema,
    }),
    { method: 'DELETE' },
  );
}

export async function removeFriend(relationshipId: string): Promise<void> {
  await httpRequest(
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
  return httpRequest(
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
  return httpRequest(`/api/v1/social/feed${query}`, socialActivityPageSchema, { signal });
}

export async function fetchFriendActivities(
  accountId: string,
  cursor?: string,
  signal?: AbortSignal,
): Promise<SocialActivityPage> {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : '';
  return httpRequest(`/api/v1/social/friends/${accountId}/activities${query}`, socialActivityPageSchema, { signal });
}
