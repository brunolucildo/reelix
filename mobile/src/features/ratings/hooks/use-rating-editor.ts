import { useMutation, useQuery } from '@tanstack/react-query';
import { useState } from 'react';

import {
  deleteRating,
  fetchRatingSummary,
  saveRating,
  type ReviewAudience,
} from '@/features/ratings/api/ratings-api';
import { type ReviewTarget, useReviewCache } from '@/features/ratings/hooks/use-review-cache';
import { useErrorToast } from '@/providers/toast-provider';

/**
 * The viewer's own rating of a title: loads the summary, keeps an unsaved draft (score and
 * audience) and saves or removes the rating.
 */
export function useRatingEditor(target: ReviewTarget, enabled: boolean) {
  const { targetType, targetId, seriesId } = target;
  const cache = useReviewCache(target);
  const [draft, setDraft] = useState<{ score: number | null; audience: ReviewAudience } | null>(null);

  const rating = useQuery({
    queryKey: cache.ratingKey,
    queryFn: ({ signal }) => fetchRatingSummary(targetType, targetId, seriesId, signal),
    enabled,
  });
  const selectedScore = draft !== null ? draft.score : rating.data?.myRating?.score ?? null;
  const audience = draft?.audience ?? rating.data?.myRating?.audience ?? 'private';

  const save = useMutation({
    mutationFn: () => {
      if (selectedScore === null) {
        throw new Error('Selecione uma nota antes de salvar.');
      }

      return saveRating(targetType, targetId, selectedScore, audience, seriesId);
    },
    onSuccess: async (result) => {
      cache.setRating(result);
      setDraft(null);
      await cache.invalidate();
    },
  });
  const remove = useMutation({
    mutationFn: () => deleteRating(targetType, targetId),
    onSuccess: async () => {
      setDraft({ score: null, audience: 'private' });
      await cache.invalidate();
    },
  });

  useErrorToast(rating.error, 'Não foi possível carregar as avaliações deste conteúdo.');
  useErrorToast(save.error, 'Não foi possível salvar sua avaliação.');
  useErrorToast(remove.error, 'Não foi possível remover sua avaliação.');

  return {
    rating,
    selectedScore,
    audience,
    setScore: (score: number) => setDraft({ score, audience }),
    setAudience: (next: ReviewAudience) => setDraft({ score: selectedScore, audience: next }),
    save,
    remove,
  };
}
