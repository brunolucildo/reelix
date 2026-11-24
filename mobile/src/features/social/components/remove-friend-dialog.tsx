import { StyleSheet, Text } from 'react-native';

import { ModalDialog } from '@/components/ui/modal-dialog';
import { PrimaryButton } from '@/components/ui/primary-button';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

type Props = {
  friend: { id: string; name: string } | null;
  isRemoving: boolean;
  onConfirm: (relationshipId: string) => void;
  onClose: () => void;
};

/** Confirmation before ending a friendship; keeps the shared history. */
export function RemoveFriendDialog({ friend, isRemoving, onConfirm, onClose }: Props) {
  return (
    <ModalDialog visible={friend !== null} title="Remover amizade?" onClose={onClose} busy={isRemoving}>
      <Text style={styles.text}>Você deixará de compartilhar atividades com {friend?.name}. O histórico de vocês não será apagado.</Text>
      <PrimaryButton label="Remover amizade" loading={isRemoving} onPress={() => { if (friend) onConfirm(friend.id); }} />
      <PrimaryButton label="Cancelar" variant="secondary" disabled={isRemoving} onPress={onClose} />
    </ModalDialog>
  );
}

const styles = StyleSheet.create({
  text: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    lineHeight: typography.bodySmall + spacing.small,
    fontFamily: fontFamily.regular,
  },
});
