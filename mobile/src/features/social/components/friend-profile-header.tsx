import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import FriendsIcon from '@/assets/brand/navigation-icons/friends.svg';
import MoreIcon from '@/assets/brand/navigation-icons/more.svg';
import { BackButton } from '@/components/navigation/back-button';
import { PrimaryButton } from '@/components/ui/primary-button';
import { UserAvatar } from '@/components/ui/user-avatar';
import type { PublicBadges } from '@/features/gamification/api/gamification-api';
import type { FriendProfileData } from '@/features/social/api/social-api';
import type { FriendProfileTab } from '@/features/social/hooks/use-friend-profile';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type RelationshipAction = 'send' | 'cancel' | 'accept' | 'decline' | 'remove';

type Props = {
  profile: FriendProfileData;
  badges: PublicBadges | undefined;
  /** A friendship action or a profile refresh is running. */
  busy: boolean;
  /** A friendship action is running. */
  isActing: boolean;
  onAction: (action: RelationshipAction) => void;
  onManage: () => void;
  onBack: () => void;
  onSelectTab: (tab: FriendProfileTab) => void;
};

/** Toolbar, identity, shared counters and the friendship call to action of a friend's profile. */
export function FriendProfileHeader({ profile, badges, busy, isActing, onAction, onManage, onBack, onSelectTab }: Props) {
  const router = useRouter();
  const status = profile.relationshipStatus;

  return (
    <>
      <View style={styles.toolbar}>
        <BackButton onPress={onBack} />
        <Text style={styles.eyebrow}>PERFIL</Text>
        {status === 'accepted' ? <Pressable accessibilityRole="button" accessibilityLabel="Gerenciar amizade" onPress={onManage} style={styles.iconButton}><MoreIcon width={dimensions.iconSize} height={dimensions.iconSize} /></Pressable> : null}
      </View>
      <View style={styles.profileHeader}>
        <View style={styles.avatarRing}>
          <UserAvatar name={profile.displayName} avatarKey={profile.avatarKey} uri={profile.avatarUrl} size={dimensions.minimumTouchTarget * 2} />
        </View>
        <View style={styles.identity}>
          <Text accessibilityRole="header" style={styles.title}>{profile.displayName}</Text>
          <Text style={styles.eyebrow}>{profile.isSelf ? 'Seu perfil' : status === 'accepted' ? 'Na sua rede' : 'Perfil Reelix'}</Text>
        </View>
      </View>
      {profile.biography ? <Text style={styles.body}>{profile.biography}</Text> : null}
      <View style={styles.statistics}>
        {profile.favoritesShared ? <Pressable accessibilityRole="button" accessibilityLabel="Ver favoritos compartilhados" onPress={() => onSelectTab('favorites')} style={styles.stat}><Text style={styles.statValue}>{profile.favorites.length}{profile.favorites.length === 100 ? '+' : ''}</Text><Text style={styles.caption}>Favoritos compartilhados</Text></Pressable> : null}
        {badges?.isPublic ? <Pressable accessibilityRole="button" accessibilityLabel="Ver conquistas públicas" onPress={() => onSelectTab('badges')} style={styles.stat}><Text style={styles.statValue}>{badges.badges.length}</Text><Text style={styles.caption}>Conquistas públicas</Text></Pressable> : null}
      </View>
      {profile.isSelf ? <PrimaryButton label="Editar meu perfil" variant="secondary" onPress={() => router.push('/profile')} /> : status === 'accepted' ? <Pressable accessibilityRole="button" accessibilityLabel="Amigos, gerenciar amizade" onPress={onManage} style={styles.friendButton}><FriendsIcon width={dimensions.iconSize} height={dimensions.iconSize} /><Text style={styles.friendLabel}>Amigos</Text></Pressable> : status === 'pending' ? profile.requestSentByMe ? <>
        <PrimaryButton label="Solicitação enviada" disabled variant="secondary" onPress={() => {}} />
        <PrimaryButton label="Cancelar solicitação" disabled={busy} loading={isActing} variant="quiet" onPress={() => onAction('cancel')} />
      </> : <View style={styles.relationshipActions}><View style={styles.grow}><PrimaryButton label="Aceitar" disabled={busy} loading={isActing} onPress={() => onAction('accept')} /></View><View style={styles.grow}><PrimaryButton label="Recusar" disabled={busy} variant="secondary" onPress={() => onAction('decline')} /></View></View> : <PrimaryButton label="Adicionar amigo" disabled={busy} loading={isActing} onPress={() => onAction('send')} />}
    </>
  );
}

const styles = StyleSheet.create({
  toolbar: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.small },
  iconButton: { width: dimensions.minimumTouchTarget, minHeight: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center' },
  profileHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.large },
  avatarRing: { borderWidth: 2, borderColor: colors.brand, borderRadius: radii.pill, padding: spacing.extraSmall },
  identity: { flex: 1, minWidth: 0, gap: spacing.small },
  eyebrow: { color: colors.textSecondary, fontSize: typography.caption, fontFamily: fontFamily.medium },
  statistics: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.large },
  stat: { minHeight: dimensions.minimumTouchTarget, flexBasis: dimensions.posterWidth, flexGrow: 1, gap: spacing.extraSmall },
  statValue: { color: colors.textPrimary, fontFamily: fontFamily.bold, fontSize: typography.section },
  caption: { color: colors.textSecondary, fontFamily: fontFamily.regular, fontSize: typography.caption },
  friendButton: { minHeight: dimensions.minimumTouchTarget, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: spacing.small, backgroundColor: colors.surface, borderRadius: radii.small, borderWidth: 1, borderColor: colors.border },
  friendLabel: { color: colors.textPrimary, fontFamily: fontFamily.semiBold, fontSize: typography.bodySmall },
  relationshipActions: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.small },
  grow: { flex: 1, minWidth: dimensions.posterWidth },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.extraBold,
  },
  body: {
    color: colors.textSecondary,
    fontSize: typography.body,
    lineHeight: spacing.large,
    fontFamily: fontFamily.regular,
  },
});
