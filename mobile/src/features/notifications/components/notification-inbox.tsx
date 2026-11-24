import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import type { NotificationInboxItem } from '@/features/notifications/api/notifications-api';
import { formatDateTime } from '@/lib/format';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  items: NotificationInboxItem[];
  hasNextPage: boolean;
  isFetchingNextPage: boolean;
  onLoadMore: () => void;
  /** Called when an unread notification is opened. */
  onRead: (notificationId: string) => void;
};

/** Inbox list: opening an item marks it as read and goes to its related screen. */
export function NotificationInbox({ items, hasNextPage, isFetchingNextPage, onLoadMore, onRead }: Props) {
  const router = useRouter();

  return (
    <View style={styles.section}>
      <Text style={styles.sectionTitle}>Caixa de entrada</Text>
      {items.length === 0 ? (
        <Text style={styles.emptyText}>Você ainda não tem notificações.</Text>
      ) : items.map((item) => (
        <Pressable
          key={item.id}
          accessibilityRole="button"
          accessibilityLabel={`${item.title}. ${item.body}. ${item.readAt ? 'Lida' : 'Não lida'}.`}
          accessibilityHint="Abre a área relacionada à notificação."
          onPress={() => {
            if (!item.readAt) {
              onRead(item.id);
            }
            router.push(item.deepLink);
          }}
          style={[styles.notification, !item.readAt && styles.unreadNotification]}
        >
          <View style={styles.notificationCopy}>
            <Text style={styles.notificationTitle}>{item.title}</Text>
            <Text style={styles.description}>{item.body}</Text>
            <Text style={styles.timestamp}>{formatDateTime(item.createdAt)}</Text>
          </View>
          {!item.readAt ? <View accessibilityLabel="Não lida" style={styles.unreadDot} /> : null}
        </Pressable>
      ))}
      {hasNextPage ? (
        <PrimaryButton
          label="Carregar mais"
          loading={isFetchingNextPage}
          onPress={onLoadMore}
          variant="secondary"
        />
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    gap: spacing.medium,
    padding: spacing.medium,
    borderRadius: radii.large,
    backgroundColor: colors.surface,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  description: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  notification: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.medium,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surfaceElevated,
  },
  unreadNotification: {
    borderColor: colors.brand,
    borderWidth: 1,
  },
  notificationCopy: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  notificationTitle: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  timestamp: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  unreadDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: colors.brand,
  },
  emptyText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
});
