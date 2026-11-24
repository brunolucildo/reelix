import { StyleSheet, Text } from 'react-native';

import { ModalDialog } from '@/components/ui/modal-dialog';
import { PrimaryButton } from '@/components/ui/primary-button';
import { colors, fontFamily, typography } from '@/theme/tokens';

type Props = {
  visible: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  onConfirm: () => void;
  onCancel: () => void;
  /** While true the dialog cannot be dismissed and the confirm button shows a spinner. */
  busy?: boolean;
};

/**
 * Destructive-action confirmation. Replaces `Alert.alert`, which is a no-op on React Native Web.
 * Cancel closes; confirm runs `onConfirm` (the caller closes the dialog once the action settles).
 */
export function ConfirmDialog({
  visible,
  title,
  message,
  confirmLabel,
  onConfirm,
  onCancel,
  busy = false,
}: Props) {
  return (
    <ModalDialog visible={visible} title={title} onClose={onCancel} busy={busy}>
      <Text style={styles.message}>{message}</Text>
      <PrimaryButton label={confirmLabel} loading={busy} onPress={onConfirm} />
      <PrimaryButton label="Cancelar" variant="secondary" disabled={busy} onPress={onCancel} />
    </ModalDialog>
  );
}

const styles = StyleSheet.create({
  message: {
    color: colors.textSecondary,
    fontFamily: fontFamily.regular,
    fontSize: typography.body,
  },
});
