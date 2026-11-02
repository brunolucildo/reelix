import { useQuery } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { UserDrawer } from '@/components/navigation/user-drawer';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchNotificationUnreadCount } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import Logo from '../../../assets/brand/logos/logo-white.svg';
import NotificationsIcon from '../../../assets/brand/navigation-icons/notifications.svg';
import SearchIcon from '../../../assets/brand/navigation-icons/search.svg';

export function AppHeader() {
  const router = useRouter();
  const unreadCount = useQuery({
    queryKey: ['notification-unread-count'],
    queryFn: ({ signal }) => fetchNotificationUnreadCount(signal),
    refetchInterval: 60_000,
  });
  useErrorToast(unreadCount.error, 'Não foi possível carregar o contador de notificações.');

  const count = unreadCount.data?.count ?? 0;

  return (
    <View style={styles.header}>
      <View style={styles.brand}>
        <Logo
          width={dimensions.logoWidth}
          height={dimensions.logoHeight}
          accessibilityLabel="Reelix"
        />
      </View>
      <Pressable
        accessibilityLabel="Pesquisar no catálogo"
        accessibilityRole="button"
        onPress={() => router.push('/search')}
        style={styles.iconButton}
      >
        <SearchIcon width={dimensions.iconSize} height={dimensions.iconSize} />
      </Pressable>
      <Pressable
        accessibilityLabel={count > 0 ? `Notificações, ${count} não lidas` : 'Notificações'}
        accessibilityRole="button"
        onPress={() => router.push('/notifications')}
        style={styles.iconButton}
      >
        <NotificationsIcon width={dimensions.iconSize} height={dimensions.iconSize} />
        {count > 0 ? (
          <View style={styles.unreadBadge}>
            <Text style={styles.unreadCount}>{count > 99 ? '99+' : count}</Text>
          </View>
        ) : null}
      </Pressable>
      <UserDrawer />
    </View>
  );
}

const styles = StyleSheet.create({
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
  },
  brand: {
    flex: 1,
  },
  iconButton: {
    minWidth: dimensions.minimumTouchTarget,
    minHeight: dimensions.minimumTouchTarget,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
  },
  unreadBadge: {
    position: 'absolute',
    top: 0,
    right: 0,
    minWidth: 18,
    height: 18,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    paddingHorizontal: spacing.extraSmall,
    backgroundColor: colors.brand,
  },
  unreadCount: {
    color: colors.background,
    fontSize: typography.caption,
    fontFamily: fontFamily.bold,
  },
});
