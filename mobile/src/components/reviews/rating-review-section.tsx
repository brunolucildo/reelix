import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

import { PrimaryButton } from '@/components/auth/primary-button';
import { formatRatingStars } from '@/components/reviews/rating-stars';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast } from '@/providers/toast-provider';
import {
  createComment,
  deleteComment,
  deleteRating,
  fetchComments,
  fetchRatingSummary,
  revealComment,
  saveRating,
  updateComment,
  type CommentItem,
  type ReviewAudience,
  type ReviewTargetType,
} from '@/services/api-client';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type RatingReviewSectionProps = {
  targetId: string;
  targetType: ReviewTargetType;
  seriesId?: string;
  allowRating?: boolean;
};

export function RatingReviewSection({
  targetId,
  targetType,
  seriesId,
  allowRating = true,
}: RatingReviewSectionProps) {
  const queryClient = useQueryClient();
  const { session } = useAuthSession();
  const [ratingDraft, setRatingDraft] = useState<{
    score: number | null;
    audience: ReviewAudience;
  } | null>(null);
  const [content, setContent] = useState('');
  const [containsSpoiler, setContainsSpoiler] = useState(false);
  const [commentAudience, setCommentAudience] = useState<ReviewAudience>('private');
  const [editingComment, setEditingComment] = useState<CommentItem | null>(null);
  const [revealedComments, setRevealedComments] = useState<Record<string, string>>({});
  const viewerId = session?.user.id ?? 'signed-out';
  const ratingKey = ['reviews', viewerId, 'rating', targetType, targetId, seriesId];
  const commentsKey = ['reviews', viewerId, 'comments', targetType, targetId, seriesId];

  const rating = useQuery({
    queryKey: ratingKey,
    queryFn: ({ signal }) => fetchRatingSummary(targetType, targetId, seriesId, signal),
    enabled: allowRating,
  });
  const comments = useInfiniteQuery({
    queryKey: commentsKey,
    queryFn: ({ pageParam, signal }) =>
      fetchComments(targetType, targetId, pageParam, seriesId, signal),
    initialPageParam: 1,
    getNextPageParam: (lastPage) =>
      lastPage.page * lastPage.pageSize < lastPage.totalCount
        ? lastPage.page + 1
        : undefined,
  });

  useErrorToast(rating.error, 'Não foi possível carregar as avaliações deste conteúdo.');
  useErrorToast(comments.error, 'Não foi possível carregar os comentários deste conteúdo.');

  const selectedScore = ratingDraft !== null
    ? ratingDraft.score
    : rating.data?.myRating?.score ?? null;
  const ratingAudience = ratingDraft?.audience ?? rating.data?.myRating?.audience ?? 'private';

  const invalidateReviews = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ratingKey }),
      queryClient.invalidateQueries({ queryKey: commentsKey }),
      queryClient.invalidateQueries({ queryKey: ['reviews', 'mine'] }),
    ]);
  };

  const saveRatingMutation = useMutation({
    mutationFn: () => {
      if (selectedScore === null) {
        throw new Error('Selecione uma nota antes de salvar.');
      }

      return saveRating(targetType, targetId, selectedScore, ratingAudience, seriesId);
    },
    onSuccess: async (result) => {
      queryClient.setQueryData(ratingKey, result);
      setRatingDraft(null);
      await invalidateReviews();
    },
  });
  const deleteRatingMutation = useMutation({
    mutationFn: () => deleteRating(targetType, targetId),
    onSuccess: async () => {
      setRatingDraft({ score: null, audience: 'private' });
      await invalidateReviews();
    },
  });
  const saveCommentMutation = useMutation({
    mutationFn: (commentId?: string) => {
      const request = {
        content,
        containsSpoiler,
        audience: commentAudience,
        requestAutomatedAnalysis: false,
      };

      return commentId
        ? updateComment(commentId, request)
        : createComment(targetType, targetId, { ...request, seriesId });
    },
    onSuccess: async () => {
      if (editingComment) {
        setRevealedComments((current) => {
          const next = { ...current };
          delete next[editingComment.id];
          return next;
        });
      }
      setContent('');
      setContainsSpoiler(false);
      setCommentAudience('private');
      setEditingComment(null);
      await invalidateReviews();
    },
  });
  const deleteCommentMutation = useMutation({
    mutationFn: deleteComment,
    onSuccess: async (_result, commentId) => {
      setRevealedComments((current) => {
        const next = { ...current };
        delete next[commentId];
        return next;
      });
      await invalidateReviews();
    },
  });
  const revealMutation = useMutation({
    mutationFn: revealComment,
    onSuccess: (revealed, commentId) => {
      setRevealedComments((current) => ({ ...current, [commentId]: revealed }));
    },
  });

  useErrorToast(saveRatingMutation.error, 'Não foi possível salvar sua avaliação.');
  useErrorToast(deleteRatingMutation.error, 'Não foi possível remover sua avaliação.');
  useErrorToast(saveCommentMutation.error, 'Não foi possível salvar seu comentário.');
  useErrorToast(deleteCommentMutation.error, 'Não foi possível excluir este comentário.');
  useErrorToast(revealMutation.error, 'Este comentário não pode ser revelado agora.');

  const allComments = comments.data?.pages.flatMap((page) => page.items) ?? [];
  const isSavingComment = saveCommentMutation.isPending;

  const beginEditing = async (comment: CommentItem) => {
    const commentContent = comment.content ?? revealedComments[comment.id];
    if (commentContent === undefined) {
      return;
    }

    setEditingComment(comment);
    setContent(commentContent);
    setContainsSpoiler(comment.containsSpoiler);
    setCommentAudience(comment.audience);
  };

  const askDeleteComment = (comment: CommentItem) => {
    Alert.alert(
      'Excluir comentário?',
      'Esta ação não pode ser desfeita.',
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Excluir',
          style: 'destructive',
          onPress: () => deleteCommentMutation.mutate(comment.id),
        },
      ],
    );
  };

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.heading}>Avaliações e comentários</Text>

      {allowRating ? <View style={styles.card}>
        <Text style={styles.subheading}>Sua avaliação</Text>
        {rating.isPending ? (
          <ActivityIndicator color={colors.brand} />
        ) : rating.isError ? (
          <View style={styles.statusBlock}>
            <Text style={styles.secondaryText}>A avaliação não está disponível agora.</Text>
            <PrimaryButton
              label="Tentar novamente"
              variant="secondary"
              onPress={() => void rating.refetch()}
            />
          </View>
        ) : (
          <>
            {rating.data.averageScore === null ? (
              <Text style={styles.secondaryText}>Ainda sem avaliações públicas.</Text>
            ) : (
              <View style={styles.publicRating}>
                <Text
                  accessibilityLabel={`Média pública ${rating.data.averageScore.toFixed(1)} de 5 estrelas`}
                  style={styles.publicRatingStars}
                >
                  {formatRatingStars(rating.data.averageScore)}
                </Text>
                <Text style={styles.secondaryText}>
                  {rating.data.publicCount} {rating.data.publicCount === 1 ? 'avaliação' : 'avaliações'}
                </Text>
              </View>
            )}
            <View accessibilityRole="radiogroup" style={styles.ratingControls}>
              <View style={styles.scoreOptions}>
                {Array.from({ length: 5 }, (_, index) => index + 1).map((score) => {
                  const selected = selectedScore !== null && selectedScore >= score;
                  const halfSelected = selectedScore === score - 0.5;
                  return (
                    <Pressable
                      key={score}
                      accessibilityRole="radio"
                      accessibilityLabel={`Selecionar ${score} ${score === 1 ? 'estrela' : 'estrelas'}`}
                      accessibilityState={{ selected: selectedScore === score }}
                      onPress={() => setRatingDraft({ score, audience: ratingAudience })}
                      style={styles.starButton}
                    >
                      <Text style={[
                        styles.starText,
                        selected || halfSelected ? styles.selectedStarText : null,
                      ]}>
                        {halfSelected ? '◐' : selected ? '★' : '☆'}
                      </Text>
                    </Pressable>
                  );
                })}
              </View>
              <View style={styles.ratingAdjustments}>
                <Pressable
                  accessibilityRole="radio"
                  accessibilityLabel="Selecionar zero estrelas"
                  accessibilityState={{ selected: selectedScore === 0 }}
                  onPress={() => setRatingDraft({ score: 0, audience: ratingAudience })}
                  style={[
                    styles.ratingAdjustment,
                    selectedScore === 0 ? styles.selectedRatingAdjustment : null,
                  ]}
                >
                  <Text style={styles.adjustmentText}>0 estrelas</Text>
                </Pressable>
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel={selectedScore === 5
                    ? 'Selecione 4 estrelas para escolher meia estrela'
                    : selectedScore !== null && selectedScore % 1 !== 0
                      ? 'Remover meia estrela'
                      : selectedScore === null
                        ? 'Escolha uma nota antes de adicionar meia estrela'
                        : 'Adicionar meia estrela'}
                  accessibilityState={{
                    disabled: selectedScore === null || selectedScore === 5,
                  }}
                  disabled={selectedScore === null || selectedScore === 5}
                  onPress={() => {
                    if (selectedScore === null || selectedScore === 5) {
                      return;
                    }

                    setRatingDraft({
                      score: selectedScore % 1 === 0
                        ? selectedScore + 0.5
                        : selectedScore - 0.5,
                      audience: ratingAudience,
                    });
                  }}
                  style={[
                    styles.ratingAdjustment,
                    selectedScore === null || selectedScore === 5
                      ? styles.disabledRatingAdjustment
                      : null,
                  ]}
                >
                  <Text style={styles.adjustmentText}>
                    {selectedScore === 5
                      ? 'Selecione 4 estrelas para usar meia estrela'
                      : selectedScore !== null && selectedScore % 1 !== 0
                        ? 'Remover meia estrela'
                        : selectedScore === null
                          ? 'Escolha uma nota para adicionar meia estrela'
                          : 'Adicionar meia estrela'}
                  </Text>
                </Pressable>
              </View>
              {selectedScore !== null ? (
                <Text style={styles.secondaryText}>
                  Sua nota: {selectedScore.toLocaleString('pt-BR')} de 5
                </Text>
              ) : null}
            </View>
            <AudienceSelector
              audience={ratingAudience}
              onChange={(audience) => setRatingDraft({ score: selectedScore, audience })}
            />
            <PrimaryButton
              label={rating.data.myRating ? 'Atualizar avaliação' : 'Salvar avaliação'}
              loading={saveRatingMutation.isPending}
              disabled={selectedScore === null || rating.isError}
              onPress={() => saveRatingMutation.mutate()}
            />
            {rating.data.myRating ? (
              <PrimaryButton
                label="Remover minha avaliação"
                variant="quiet"
                loading={deleteRatingMutation.isPending}
                onPress={() => deleteRatingMutation.mutate()}
              />
            ) : null}
          </>
        )}
      </View> : null}

      <View style={styles.card}>
        <Text style={styles.subheading}>{editingComment ? 'Editar comentário' : 'Escrever comentário'}</Text>
        <TextInput
          accessibilityLabel="Texto do comentário"
          accessibilityHint="Máximo de 2.000 caracteres"
          editable={!isSavingComment}
          maxLength={2000}
          multiline
          onChangeText={setContent}
          placeholder="Compartilhe sua opinião"
          placeholderTextColor={colors.textSecondary}
          style={styles.commentInput}
          textAlignVertical="top"
          value={content}
        />
        <Text style={styles.characterCount}>{content.length}/2.000</Text>
        <ToggleButton
          selected={containsSpoiler}
          label={containsSpoiler ? 'Este comentário contém spoiler' : 'Marcar como spoiler'}
          onPress={() => setContainsSpoiler((value) => !value)}
        />
        <AudienceSelector audience={commentAudience} onChange={setCommentAudience} />
        <PrimaryButton
          label={editingComment ? 'Salvar alterações' : 'Publicar comentário'}
          loading={isSavingComment}
          disabled={content.trim().length === 0}
          onPress={() => saveCommentMutation.mutate(editingComment?.id)}
        />
        {editingComment ? (
          <PrimaryButton
            label="Cancelar edição"
            variant="secondary"
            disabled={isSavingComment}
            onPress={() => {
              setEditingComment(null);
              setContent('');
              setContainsSpoiler(false);
              setCommentAudience('private');
            }}
          />
        ) : null}
      </View>

      <View style={styles.commentsHeader}>
        <Text style={styles.subheading}>Comentários</Text>
        {comments.isFetching && !comments.isPending ? (
          <ActivityIndicator color={colors.brand} size="small" />
        ) : null}
      </View>
      {comments.isPending ? (
        <ActivityIndicator color={colors.brand} />
      ) : comments.isError ? (
        <View style={styles.statusBlock}>
          <Text style={styles.secondaryText}>Não foi possível carregar os comentários.</Text>
          <PrimaryButton
            label="Tentar novamente"
            variant="secondary"
            onPress={() => void comments.refetch()}
          />
        </View>
      ) : allComments.length === 0 ? (
        <Text style={styles.secondaryText}>Seja a primeira pessoa a comentar.</Text>
      ) : (
        <>
          {allComments.map((comment) => (
            <CommentCard
              key={comment.id}
              comment={comment}
              revealedContent={revealedComments[comment.id]}
              isRevealing={revealMutation.isPending && revealMutation.variables === comment.id}
              isDeleting={deleteCommentMutation.isPending && deleteCommentMutation.variables === comment.id}
              onReveal={() => revealMutation.mutate(comment.id)}
              onEdit={() => void beginEditing(comment)}
              onDelete={() => askDeleteComment(comment)}
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
    </View>
  );
}

function AudienceSelector({
  audience,
  onChange,
}: {
  audience: ReviewAudience;
  onChange: (audience: ReviewAudience) => void;
}) {
  return (
    <View style={styles.audienceRow}>
      {(['private', 'public'] as const).map((value) => (
        <Pressable
          key={value}
          accessibilityRole="radio"
          accessibilityState={{ selected: audience === value }}
          onPress={() => onChange(value)}
          style={[styles.audienceButton, audience === value ? styles.selectedAudience : null]}
        >
          <Text style={styles.audienceText}>{value === 'private' ? 'Privada' : 'Pública'}</Text>
        </Pressable>
      ))}
    </View>
  );
}

function ToggleButton({
  selected,
  label,
  onPress,
}: {
  selected: boolean;
  label: string;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="checkbox"
      accessibilityState={{ checked: selected }}
      onPress={onPress}
      style={styles.toggle}
    >
      <View style={[styles.checkbox, selected ? styles.checkedBox : null]}>
        {selected ? <Text style={styles.checkmark}>✓</Text> : null}
      </View>
      <Text style={styles.toggleLabel}>{label}</Text>
    </Pressable>
  );
}

function CommentCard({
  comment,
  revealedContent,
  isRevealing,
  isDeleting,
  onReveal,
  onEdit,
  onDelete,
}: {
  comment: CommentItem;
  revealedContent?: string;
  isRevealing: boolean;
  isDeleting: boolean;
  onReveal: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const visibleContent = revealedContent ?? comment.content;
  const canReveal = comment.moderationStatus === 'published' &&
    (comment.containsSpoiler || comment.possibleSpoiler);

  return (
    <View style={styles.commentCard}>
      <View style={styles.commentMeta}>
        <Text style={styles.author}>{comment.authorName}</Text>
        <Text style={styles.commentDate}>
          {new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' })
            .format(new Date(comment.createdAt))}
        </Text>
      </View>
      {comment.moderationStatus === 'pending' ? (
        <Text style={styles.warningText}>Aguardando análise solicitada pelo autor.</Text>
      ) : comment.moderationStatus === 'blocked' ? (
        <Text style={styles.warningText}>Este comentário não está publicado.</Text>
      ) : null}
      {visibleContent !== null && visibleContent !== undefined ? (
        <Text style={styles.commentText}>{visibleContent}</Text>
      ) : canReveal ? (
        <PrimaryButton
          label={isRevealing ? 'Revelando...' : 'Revelar spoiler'}
          variant="secondary"
          loading={isRevealing}
          onPress={onReveal}
        />
      ) : (
        <Text style={styles.secondaryText}>O conteúdo está indisponível.</Text>
      )}
      {comment.isOwn ? (
        <View style={styles.commentActions}>
          {comment.moderationStatus !== 'blocked' ? (
            <Pressable
              accessibilityRole="button"
              disabled={visibleContent === null || visibleContent === undefined}
              onPress={onEdit}
              style={styles.textAction}
            >
              <Text style={styles.actionLabel}>Editar</Text>
            </Pressable>
          ) : null}
          <Pressable
            accessibilityRole="button"
            disabled={isDeleting}
            onPress={onDelete}
            style={styles.textAction}
          >
            <Text style={styles.actionLabel}>{isDeleting ? 'Excluindo...' : 'Excluir'}</Text>
          </Pressable>
        </View>
      ) : null}
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
  subheading: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  card: {
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
    lineHeight: 21,
  },
  scoreOptions: {
    flexDirection: 'row',
    gap: spacing.small,
    alignItems: 'center',
    justifyContent: 'flex-start',
  },
  ratingControls: {
    gap: spacing.small,
  },
  ratingAdjustments: {
    flexDirection: 'row',
    gap: spacing.small,
  },
  ratingAdjustment: {
    flex: 1,
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.small,
    backgroundColor: colors.surfaceElevated,
  },
  selectedRatingAdjustment: {
    borderColor: colors.brandSecondary,
  },
  disabledRatingAdjustment: {
    opacity: 0.5,
  },
  adjustmentText: {
    color: colors.textPrimary,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
    textAlign: 'center',
  },
  starButton: {
    minWidth: 48,
    minHeight: 48,
    alignItems: 'center',
    justifyContent: 'center',
  },
  starText: {
    color: colors.textSecondary,
    fontSize: typography.title,
    fontFamily: fontFamily.bold,
  },
  selectedStarText: {
    color: colors.brandSecondary,
  },
  publicRating: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
  },
  publicRatingStars: {
    color: colors.brandSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  audienceRow: {
    flexDirection: 'row',
    gap: spacing.small,
  },
  audienceButton: {
    minHeight: 44,
    minWidth: 100,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    backgroundColor: colors.surfaceElevated,
  },
  selectedAudience: {
    borderColor: colors.brandSecondary,
  },
  audienceText: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  commentInput: {
    minHeight: 130,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    backgroundColor: colors.background,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  characterCount: {
    alignSelf: 'flex-end',
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  toggle: {
    minHeight: 44,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
  },
  checkbox: {
    width: 22,
    height: 22,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.textSecondary,
    borderRadius: radii.extraSmall,
  },
  checkedBox: {
    borderColor: colors.brand,
    backgroundColor: colors.brand,
  },
  checkmark: {
    color: colors.white,
    fontSize: typography.caption,
    fontFamily: fontFamily.bold,
  },
  toggleLabel: {
    flex: 1,
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  privacyNote: {
    color: colors.warning,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
    lineHeight: 18,
  },
  commentsHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  statusBlock: {
    gap: spacing.small,
  },
  commentCard: {
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  commentMeta: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
  },
  author: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  commentDate: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  commentText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
    lineHeight: 22,
  },
  warningText: {
    color: colors.warning,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
  },
  commentActions: {
    flexDirection: 'row',
    gap: spacing.medium,
  },
  textAction: {
    minHeight: 44,
    justifyContent: 'center',
  },
  actionLabel: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
});
