import type { ReviewTargetType } from '@/features/ratings/api/ratings-api';

export const ratingsQueryKeys = {
  all: ['reviews'] as const,
  ownRatingsAll: ['reviews', 'mine'] as const,
  ownRatings: (page: number) => ['reviews', 'mine', page] as const,
  rating: (viewerId: string, targetType: ReviewTargetType, targetId: string, seriesId?: string) =>
    ['reviews', viewerId, 'rating', targetType, targetId, seriesId] as const,
  comments: (viewerId: string, targetType: ReviewTargetType, targetId: string, seriesId?: string) =>
    ['reviews', viewerId, 'comments', targetType, targetId, seriesId] as const,
};
