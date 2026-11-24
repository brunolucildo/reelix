import { useQueryClient } from '@tanstack/react-query';

import type { RatingSummary, ReviewTargetType } from '@/features/ratings/api/ratings-api';
import { ratingsQueryKeys } from '@/features/ratings/query-keys';
import { useViewerId } from '@/providers/auth-provider';

export type ReviewTarget = {
  targetType: ReviewTargetType;
  targetId: string;
  seriesId?: string;
};

/** Cache keys and refresh helpers of one reviewed title, scoped to the signed-in viewer. */
export function useReviewCache({ targetType, targetId, seriesId }: ReviewTarget) {
  const queryClient = useQueryClient();
  const viewerId = useViewerId() ?? 'signed-out';
  const ratingKey = ratingsQueryKeys.rating(viewerId, targetType, targetId, seriesId);
  const commentsKey = ratingsQueryKeys.comments(viewerId, targetType, targetId, seriesId);

  return {
    ratingKey,
    commentsKey,
    setRating: (summary: RatingSummary) => queryClient.setQueryData(ratingKey, summary),
    invalidate: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ratingKey }),
        queryClient.invalidateQueries({ queryKey: commentsKey }),
        queryClient.invalidateQueries({ queryKey: ratingsQueryKeys.ownRatingsAll }),
      ]);
    },
  };
}
