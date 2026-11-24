import { RatingReviewSection } from '@/features/ratings/components/rating-review-section';
import { useMovieTrackingStatus } from '@/features/tracking/hooks/use-tracking';

/** Reviews of a movie; rating is allowed only once the viewer marked the movie as watched. */
export function MovieReviews({ titleId }: { titleId: string }) {
  const status = useMovieTrackingStatus(titleId);
  return (
    <RatingReviewSection
      key={`movie-reviews:${titleId}`}
      targetId={titleId}
      targetType="movie"
      allowRating={status.data === true}
    />
  );
}
