import { useQuery } from '@tanstack/react-query';
import { Image } from 'expo-image';
import { type Href, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Animated, Modal, Pressable, ScrollView, StyleSheet, Text, useWindowDimensions, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { PrimaryButton } from '@/components/auth/primary-button';
import { AchievementGrid } from '@/components/gamification/achievement-grid';
import { useAuthSession } from '@/providers/auth-provider';
import { ToastViewport, useErrorToast, useToast } from '@/providers/toast-provider';
import { fetchGamificationDashboard, fetchProfile } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import ProfileIcon from '../../../assets/brand/navigation-icons/profile.svg';
import FavoritesIcon from '../../../assets/brand/navigation-icons/favorites.svg';
import ListsIcon from '../../../assets/brand/navigation-icons/list-add.svg';
import ClockIcon from '../../../assets/brand/navigation-icons/clock.svg';
import FriendsIcon from '../../../assets/brand/navigation-icons/friends.svg';
import AchievementsIcon from '../../../assets/brand/navigation-icons/achievements.svg';
import SettingsIcon from '../../../assets/brand/navigation-icons/settings.svg';
import CloseIcon from '../../../assets/brand/navigation-icons/close.svg';
import RatingIcon from '../../../assets/brand/navigation-icons/rating.svg';

const destinations: { label: string; href: Href; Icon: typeof ProfileIcon }[] = [
  { label: 'Meu perfil', href: '/profile', Icon: ProfileIcon },
  { label: 'Favoritos', href: { pathname: '/lists', params: { section: 'favorites' } }, Icon: FavoritesIcon },
  { label: 'Minhas listas', href: { pathname: '/lists', params: { section: 'lists' } }, Icon: ListsIcon },
  { label: 'Quero assistir', href: { pathname: '/lists', params: { section: 'watchlist' } }, Icon: ClockIcon },
  { label: 'Acompanhamento', href: '/tracking', Icon: ClockIcon },
  { label: 'Minhas avaliações', href: '/my-ratings', Icon: RatingIcon },
  { label: 'Conquistas', href: '/gamification', Icon: AchievementsIcon },
  { label: 'Amigos e atividades', href: '/social', Icon: FriendsIcon },
  { label: 'Configurações', href: '/settings', Icon: SettingsIcon },
];

export function UserDrawer() {
  const router = useRouter();
  const { session, signOut } = useAuthSession();
  const { showError } = useToast();
  const insets = useSafeAreaInsets();
  const { width } = useWindowDimensions();
  const drawerWidth = Math.min(dimensions.drawerWidth, width - dimensions.minimumTouchTarget);
  const [visible, setVisible] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [signingOut, setSigningOut] = useState(false);
  const [offset] = useState(() => new Animated.Value(-dimensions.drawerWidth));
  const profile = useQuery({ queryKey: ['profile'], queryFn: fetchProfile, enabled: Boolean(session) });
  const gamification = useQuery({
    queryKey: ['gamification', 'dashboard'],
    queryFn: ({ signal }) => fetchGamificationDashboard(signal),
    enabled: Boolean(session) && visible,
  });
  useErrorToast(gamification.error, 'Não foi possível carregar suas conquistas.');
  useErrorToast(profile.error, 'Não foi possível carregar seu perfil. Tente novamente.');
  const name = profile.data?.displayName || 'Minha conta';
  const initials = name.trim().slice(0, 1).toLocaleUpperCase('pt-BR');

  const close = (after?: () => void) => {
    if (signingOut) return;
    Animated.timing(offset, { toValue: -drawerWidth, duration: 200, useNativeDriver: false }).start(({ finished }) => {
      if (finished) {
        setVisible(false);
        setConfirming(false);
        after?.();
      }
    });
  };
  const logout = async () => {
    setSigningOut(true);
    try {
      await signOut();
      setVisible(false);
    } catch (error) {
      setVisible(false);
      showError(error instanceof Error ? error.message : 'Não foi possível sair da conta.');
    } finally {
      setSigningOut(false);
    }
  };
  const avatar = (large: boolean) => profile.data?.avatarUrl ? (
    <Image source={{ uri: profile.data.avatarUrl }} style={large ? styles.avatarLarge : styles.avatar} contentFit="cover" />
  ) : (
    <View style={large ? styles.avatarLarge : styles.avatar}><Text style={styles.initial}>{initials}</Text></View>
  );

  return (
    <>
      <Pressable accessibilityRole="button" accessibilityLabel="Abrir menu da conta" accessibilityState={{ expanded: visible }} onPress={() => {
        offset.setValue(-drawerWidth);
        setVisible(true);
      }} style={styles.trigger}>{avatar(false)}</Pressable>
      <Modal transparent visible={visible} onRequestClose={() => close()} onShow={() => {
        Animated.timing(offset, { toValue: 0, duration: 220, useNativeDriver: false }).start();
      }}>
        <View style={styles.overlay}>
          <Pressable accessibilityRole="button" accessibilityLabel="Fechar menu da conta" onPress={() => close()} style={styles.backdrop} />
          <Animated.View accessibilityViewIsModal style={[styles.drawer, { width: drawerWidth, paddingTop: insets.top + spacing.medium, paddingBottom: insets.bottom + spacing.medium, transform: [{ translateX: offset }] }]}>
            <View style={styles.header}>
              <View style={styles.identity}>
                {avatar(true)}
                {gamification.isPending ? (
                  <ActivityIndicator accessibilityLabel="Carregando conquistas" color={colors.brand} />
                ) : null}
                {gamification.data ? (
                  <AchievementGrid compact achievements={gamification.data.achievements.filter((item) => item.isEarned)} />
                ) : null}
              </View>
              <Pressable accessibilityRole="button" accessibilityLabel="Fechar menu" onPress={() => close()} style={styles.iconButton}>
                <CloseIcon width={dimensions.iconSize} height={dimensions.iconSize} />
              </Pressable>
            </View>
            <Text accessibilityRole="header" style={styles.name}>{name}</Text>
            {session?.user.email ? <Text style={styles.email}>{session.user.email}</Text> : null}
            {profile.isError ? <PrimaryButton label="Atualizar perfil" variant="secondary" onPress={() => void profile.refetch()} /> : null}
            {gamification.isError ? <PrimaryButton label="Atualizar conquistas" variant="secondary" onPress={() => void gamification.refetch()} /> : null}
            <ScrollView contentContainerStyle={styles.options}>
              {destinations.map(({ label, href, Icon }) => (
                <Pressable key={label} accessibilityRole="button" onPress={() => close(() => router.push(href))} style={({ pressed }) => [styles.option, pressed && styles.pressed]}>
                  <Icon width={dimensions.iconSize} height={dimensions.iconSize} />
                  <Text style={styles.optionLabel}>{label}</Text>
                </Pressable>
              ))}
            </ScrollView>
            {confirming ? (
              <View style={styles.confirmation}>
                <Text accessibilityRole="header" style={styles.optionLabel}>Sair da conta?</Text>
                <PrimaryButton label="Confirmar saída" loading={signingOut} onPress={() => void logout()} />
                <PrimaryButton label="Cancelar" disabled={signingOut} variant="secondary" onPress={() => setConfirming(false)} />
              </View>
            ) : (
              <Pressable accessibilityRole="button" onPress={() => setConfirming(true)} style={styles.option}>
                <CloseIcon width={dimensions.iconSize} height={dimensions.iconSize} />
                <Text style={styles.signOutLabel}>Sair da conta</Text>
              </Pressable>
            )}
          </Animated.View>
          <ToastViewport />
        </View>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  trigger: { minWidth: dimensions.minimumTouchTarget, minHeight: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center' },
  avatar: { width: dimensions.minimumTouchTarget, height: dimensions.minimumTouchTarget, borderRadius: radii.pill, backgroundColor: colors.surfaceElevated, alignItems: 'center', justifyContent: 'center' },
  avatarLarge: { width: dimensions.avatarSize, height: dimensions.avatarSize, borderRadius: radii.pill, backgroundColor: colors.surfaceElevated, alignItems: 'center', justifyContent: 'center' },
  initial: { fontFamily: fontFamily.bold, fontSize: typography.section, color: colors.textPrimary },
  overlay: { flex: 1 },
  backdrop: { ...StyleSheet.absoluteFill, backgroundColor: colors.background, opacity: 0.72 },
  drawer: { position: 'absolute', left: 0, top: 0, bottom: 0, paddingHorizontal: spacing.medium, gap: spacing.small, backgroundColor: colors.surface },
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  identity: { flex: 1, flexDirection: 'row', alignItems: 'center', gap: spacing.small },
  iconButton: { width: dimensions.minimumTouchTarget, height: dimensions.minimumTouchTarget, justifyContent: 'center', alignItems: 'center' },
  name: { fontFamily: fontFamily.bold, fontSize: typography.section, color: colors.textPrimary },
  email: { fontFamily: fontFamily.regular, fontSize: typography.caption, color: colors.textSecondary },
  options: { paddingVertical: spacing.medium, gap: spacing.extraSmall },
  option: { minHeight: dimensions.minimumTouchTarget, flexDirection: 'row', alignItems: 'center', gap: spacing.medium, paddingVertical: spacing.small },
  optionLabel: { flexShrink: 1, fontFamily: fontFamily.medium, fontSize: typography.body, color: colors.textPrimary },
  signOutLabel: { fontFamily: fontFamily.medium, fontSize: typography.body, color: colors.brandSecondary },
  pressed: { backgroundColor: colors.surfaceElevated, borderRadius: radii.small },
  confirmation: { gap: spacing.small },
});