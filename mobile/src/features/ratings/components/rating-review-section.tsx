import { useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { PrimaryButton } from '@/components/ui/primary-button';
import type { CommentItem, ReviewTargetType } from '@/features/ratings/api/ratings-api';
import { CommentCard } from '@/features/ratings/components/comment-card';
import { CommentForm } from '@/features/ratings/components/comment-form';
import { RatingCard } from '@/features/ratings/components/rating-card';
import { reviewsStyles as shared } from '@/features/ratings/components/reviews-styles';
import { useReviewComments } from '@/features/ratings/hooks/use-review-comments';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

type Props = {
  targetId: string;
  targetType: ReviewTargetType;
  seriesId?: string;
  allowRating?: boolean;
};

/** Rating card, comment form and comment list of a movie, series or episode. */
export function RatingReviewSection({ targetId, targetType, seriesId, allowRating = true }: Props) {
  const target = { targetType, targetId, seriesId };
  const { comments, items, revealed, save, remove, reveal } = useReviewComments(target);
  const [editing, setEditing] = useState<{ comment: CommentItem; content: string } | null>(null);
  const [commentToDelete, setCommentToDelete] = useState<CommentItem | null>(null);

  const beginEditing = (comment: CommentItem) => {
    const content = comment.content ?? revealed[comment.id];
    if (content === undefined) {
      return;
    }

    setEditing({ comment, content });
  };

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.heading}>Avaliações e comentários</Text>

      {allowRating ? <RatingCard target={target} /> : null}

      <CommentForm
        key={editing?.comment.id ?? 'new'}
        editing={editing ? {
          id: editing.comment.id,
          values: {
            content: editing.content,
            containsSpoiler: editing.comment.containsSpoiler,
            audience: editing.comment.audience,
          },
        } : null}
        isSaving={save.isPending}
        onSave={(commentId, values, onSuccess) => save.mutate({ commentId, values }, { onSuccess })}
        onFinish={() => setEditing(null)}
      />

      <View style={styles.commentsHeader}>
        <Text style={shared.subheading}>Comentários</Text>
        {comments.isFetching && !comments.isPending ? (
          <ActivityIndicator color={colors.brand} size="small" />
        ) : null}
      </View>
      {comments.isPending ? (
        <ActivityIndicator color={colors.brand} />
      ) : comments.isError ? (
        <View style={shared.statusBlock}>
          <Text style={shared.secondaryText}>Não foi possível carregar os comentários.</Text>
          <PrimaryButton
            label="Tentar novamente"
            variant="secondary"
            onPress={() => void comments.refetch()}
          />
        </View>
      ) : items.length === 0 ? (
        <Text style={shared.secondaryText}>Seja a primeira pessoa a comentar.</Text>
      ) : (
        <>
          {items.map((comment) => (
            <CommentCard
              key={comment.id}
              comment={comment}
              revealedContent={revealed[comment.id]}
              isRevealing={reveal.isPending && reveal.variables === comment.id}
              isDeleting={remove.isPending && remove.variables === comment.id}
              onReveal={() => reveal.mutate(comment.id)}
              onEdit={() => beginEditing(comment)}
              onDelete={() => setCommentToDelete(comment)}
            />
          ))}
          {comments.hasNextPage ? (
            <PrimaryButton
              label="Carregar mais comentários"
              variant="secondary"
              loading={comments.isFetchingNextPage}
              onPress={() => void comments.fetchNextPage()}
            />
          ) : null}
        </>
      )}
      <ConfirmDialog
        visible={commentToDelete !== null}
        title="Excluir comentário?"
        message="Esta ação não pode ser desfeita."
        confirmLabel="Excluir"
        onCancel={() => setCommentToDelete(null)}
        onConfirm={() => {
          if (commentToDelete) remove.mutate(commentToDelete.id);
          setCommentToDelete(null);
        }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    gap: spacing.medium,
  },
  heading: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  commentsHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
});
