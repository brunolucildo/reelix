import { Pressable, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { UserAvatar } from '@/components/ui/user-avatar';
import GlobeIcon from '@/assets/brand/navigation-icons/globe.svg';
import LockIcon from '@/assets/brand/navigation-icons/lock.svg';
import type { CommentItem } from '@/features/ratings/api/ratings-api';
import { reviewsStyles as shared } from '@/features/ratings/components/reviews-styles';
import { formatDateMedium } from '@/lib/format';
import { colors, dimensions, fontFamily, spacing, typography } from '@/theme/tokens';

type Props = {
  comment: CommentItem;
  revealedContent?: string;
  isRevealing: boolean;
  isDeleting: boolean;
  onReveal: () => void;
  onEdit: () => void;
  onDelete: () => void;
};

export function CommentCard({
  comment,
  revealedContent,
  isRevealing,
  isDeleting,
  onReveal,
  onEdit,
  onDelete,
}: Props) {
  const visibleContent = revealedContent ?? comment.content;
  const canReveal = comment.moderationStatus === 'published' &&
    (comment.containsSpoiler || comment.possibleSpoiler);

  return (
    <View style={shared.card}>
      <View style={styles.meta}>
        <UserAvatar name={comment.authorName} avatarKey={comment.authorAvatarKey} />
        <Text style={styles.author}>{comment.authorName}</Text>
        <View accessible accessibilityRole="image" accessibilityLabel={comment.audience === 'private' ? 'Comentário privado' : 'Comentário público'}>
          {comment.audience === 'private' ? <LockIcon width={dimensions.iconSize} height={dimensions.iconSize} /> : <GlobeIcon width={dimensions.iconSize} height={dimensions.iconSize} />}
        </View>
        <Text style={styles.date}>{formatDateMedium(new Date(comment.createdAt))}</Text>
      </View>
      {comment.moderationStatus === 'pending' ? (
        <Text style={styles.warning}>Aguardando análise solicitada pelo autor.</Text>
      ) : comment.moderationStatus === 'blocked' ? (
        <Text style={styles.warning}>Este comentário não está publicado.</Text>
      ) : null}
      {visibleContent !== null && visibleContent !== undefined ? (
        <Text style={styles.text}>{visibleContent}</Text>
      ) : canReveal ? (
        <PrimaryButton
          label={isRevealing ? 'Revelando...' : 'Revelar spoiler'}
          variant="secondary"
          loading={isRevealing}
          onPress={onReveal}
        />
      ) : (
        <Text style={shared.secondaryText}>O conteúdo está indisponível.</Text>
      )}
      {comment.isOwn ? (
        <View style={styles.actions}>
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
  meta: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
  },
  author: {
    flex: 1,
    minWidth: 0,
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  date: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  text: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
    lineHeight: 22,
  },
  warning: {
    color: colors.warning,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
  },
  actions: {
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
