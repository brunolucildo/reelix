import type { ReactNode } from 'react';
import { KeyboardAvoidingView, Modal, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ToastViewport } from '@/providers/toast-provider';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import CloseIcon from '../../assets/brand/navigation-icons/close.svg';

type Props = {
  visible: boolean;
  title: string;
  onClose: () => void;
  busy?: boolean;
  children: ReactNode;
};

export function ModalDialog({ visible, title, onClose, busy = false, children }: Props) {
  const close = () => {
    if (!busy) onClose();
  };
  return (
    <Modal transparent animationType="fade" visible={visible} onRequestClose={close}>
      <SafeAreaView style={styles.overlay}>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Fechar janela"
          disabled={busy}
          onPress={close}
          style={StyleSheet.absoluteFill}
        />
        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
          style={styles.container}
        >
          <View accessibilityViewIsModal style={styles.dialog}>
            <View style={styles.header}>
              <Text accessibilityRole="header" style={styles.title}>{title}</Text>
              <Pressable
                accessibilityRole="button"
                accessibilityLabel="Fechar janela"
                disabled={busy}
                onPress={close}
                style={styles.close}
              >
                <CloseIcon width={dimensions.iconSize} height={dimensions.iconSize} />
              </Pressable>
            </View>
            <ScrollView keyboardShouldPersistTaps="handled" contentContainerStyle={styles.content}>
              {children}
            </ScrollView>
          </View>
        </KeyboardAvoidingView>
        <ToastViewport />
      </SafeAreaView>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: { flex: 1, justifyContent: 'center', padding: spacing.medium, backgroundColor: colors.background },
  container: { width: '100%', maxWidth: dimensions.drawerWidth * 2, maxHeight: '100%', alignSelf: 'center' },
  dialog: { maxHeight: '100%', borderRadius: radii.medium, padding: spacing.medium, backgroundColor: colors.surface },
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.small, marginBottom: spacing.medium },
  title: { flex: 1, color: colors.textPrimary, fontFamily: fontFamily.bold, fontSize: typography.section },
  close: { minWidth: dimensions.minimumTouchTarget, minHeight: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center' },
  content: { gap: spacing.medium },
});
