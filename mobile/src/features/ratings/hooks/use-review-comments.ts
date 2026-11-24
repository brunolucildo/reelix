import { useInfiniteQuery, useMutation } from '@tanstack/react-query';
import { useState } from 'react';

import {
  createComment,
  deleteComment,
  fetchComments,
  revealComment,
  updateComment,
  type ReviewAudience,
} from '@/features/ratings/api/ratings-api';
import { type ReviewTarget, useReviewCache } from '@/features/ratings/hooks/use-review-cache';
import { getNextPageByCount } from '@/lib/pagination';
import { useErrorToast } from '@/providers/toast-provider';

export type CommentValues = {
  content: string;
  containsSpoiler: boolean;
  audience: ReviewAudience;
};

/** Comments of a title: paged list, create/update/delete and on-demand spoiler reveal. */
export function useReviewComments(target: ReviewTarget) {
  const { targetType, targetId, seriesId } = target;
  const cache = useReviewCache(target);
  const [revealed, setRevealed] = useState<Record<string, string>>({});
  const forget = (commentId: string) => setRevealed((current) => {
    const next = { ...current };
    delete next[commentId];
    return next;
  });

  const comments = useInfiniteQuery({
    queryKey: cache.commentsKey,
    queryFn: ({ pageParam, signal }) =>
      fetchComments(targetType, targetId, pageParam, seriesId, signal),
    initialPageParam: 1,
    getNextPageParam: getNextPageByCount,
  });
  const save = useMutation({
    mutationFn: ({ commentId, values }: { commentId?: string; values: CommentValues }) => {
      const request = { ...values, requestAutomatedAnalysis: false };

      return commentId
        ? updateComment(commentId, request)
        : createComment(targetType, targetId, { ...request, seriesId });
    },
    onSuccess: async (_result, { commentId }) => {
      if (commentId) forget(commentId);
      await cache.invalidate();
    },
  });
  const remove = useMutation({
    mutationFn: deleteComment,
    onSuccess: async (_result, commentId) => {
      forget(commentId);
      await cache.invalidate();
    },
  });
  const reveal = useMutation({
    mutationFn: revealComment,
    onSuccess: (content, commentId) => {
      setRevealed((current) => ({ ...current, [commentId]: content }));
    },
  });

  useErrorToast(comments.error, 'Não foi possível carregar os comentários deste conteúdo.');
  useErrorToast(save.error, 'Não foi possível salvar seu comentário.');
  useErrorToast(remove.error, 'Não foi possível excluir este comentário.');
  useErrorToast(reveal.error, 'Este comentário não pode ser revelado agora.');

  return {
    comments,
    items: comments.data?.pages.flatMap((page) => page.items) ?? [],
    revealed,
    save,
    remove,
    reveal,
  };
}
