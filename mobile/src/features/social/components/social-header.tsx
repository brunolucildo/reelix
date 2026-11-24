import { Pressable, StyleSheet, Text, View } from 'react-native';

import FeedIcon from '@/assets/brand/navigation-icons/home.svg';
import RequestsIcon from '@/assets/brand/navigation-icons/profile.svg';
import FriendsIcon from '@/assets/brand/navigation-icons/friends.svg';
import SearchIcon from '@/assets/brand/navigation-icons/search.svg';
import ActivityIcon from '@/assets/brand/navigation-icons/clock.svg';
import { BackButton } from '@/components/navigation/back-button';
import { socialStyles } from '@/features/social/components/social-styles';
import type { SocialTab } from '@/features/social/hooks/use-social-queries';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

const socialTabs = [
  { key: 'feed', label: 'Feed', Icon: FeedIcon },
  { key: 'requests', label: 'Solicitações', Icon: RequestsIcon },
  { key: 'friends', label: 'Amigos', Icon: FriendsIcon },
  { key: 'discover', label: 'Descobrir', Icon: SearchIcon },
  { key: 'activities', label: 'Atividades', Icon: ActivityIcon },
] as const;

type Props = {
  activeTab: SocialTab;
  onTabChange: (tab: SocialTab) => void;
  pendingRequests: number;
  hasMorePendingRequests: boolean;
};

/** Title, quick actions and the five tabs (with the pending-requests badge). */
export function SocialHeader({ activeTab, onTabChange, pendingRequests, hasMorePendingRequests }: Props) {
  return (
    <>
      <BackButton />
      <View style={styles.headerRow}>
        <View style={socialStyles.heading}>
          <Text accessibilityRole="header" style={styles.title}>Amigos</Text>
          <Text style={socialStyles.secondaryText}>O que sua rede está assistindo?</Text>
        </View>
        <View style={socialStyles.inlineActions}>
          <Pressable accessibilityRole="button" accessibilityLabel="Adicionar pessoas" accessibilityHint="Abre a descoberta de pessoas" onPress={() => onTabChange('discover')} style={styles.iconButton}>
            <RequestsIcon width={dimensions.iconSize} height={dimensions.iconSize} />
            <Text style={styles.addSymbol}>+</Text>
          </Pressable>
          <Pressable accessibilityRole="button" accessibilityLabel="Pesquisar pessoas" onPress={() => onTabChange('discover')} style={styles.iconButton}>
            <SearchIcon width={dimensions.iconSize} height={dimensions.iconSize} />
          </Pressable>
        </View>
      </View>

      <View accessibilityRole="tablist" style={styles.navigation}>
        {socialTabs.map(({ key, label, Icon }) => (
          <Pressable
            key={key}
            accessibilityRole="tab"
            accessibilityLabel={key === 'requests' ? `${label}, ${pendingRequests}${hasMorePendingRequests ? ' ou mais' : ''} pendentes` : label}
            accessibilityState={{ selected: activeTab === key }}
            onPress={() => onTabChange(key)}
            style={[styles.navigationTab, activeTab === key && socialStyles.navigationTabActive]}
          >
            <View>
              <Icon width={dimensions.iconSize} height={dimensions.iconSize} />
              {key === 'requests' && pendingRequests > 0 ? (
                <View style={styles.badge}><Text style={styles.badgeText}>{pendingRequests}{hasMorePendingRequests ? '+' : ''}</Text></View>
              ) : null}
            </View>
            <Text style={[styles.tabLabel, activeTab === key && styles.tabLabelActive]}>{label}</Text>
          </Pressable>
        ))}
      </View>
    </>
  );
}

const styles = StyleSheet.create({
  headerRow: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: spacing.small },
  iconButton: { width: dimensions.minimumTouchTarget, height: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center' },
  addSymbol: { position: 'absolute', right: spacing.extraSmall, bottom: spacing.extraSmall, color: colors.brandSecondary, backgroundColor: colors.background, fontSize: typography.body, fontFamily: fontFamily.bold },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.extraBold,
  },
  navigation: { flexDirection: 'row', borderBottomWidth: 1, borderBottomColor: colors.border },
  navigationTab: { flex: 1, minHeight: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center', gap: spacing.small, paddingVertical: spacing.small, borderBottomWidth: 2, borderBottomColor: colors.background },
  tabLabel: { color: colors.textSecondary, fontSize: typography.eyebrow, fontFamily: fontFamily.medium, textAlign: 'center' },
  tabLabelActive: { color: colors.brandSecondary, fontFamily: fontFamily.bold },
  badge: { position: 'absolute', top: -spacing.small, right: -spacing.medium, backgroundColor: colors.brand, borderRadius: radii.pill, paddingHorizontal: spacing.extraSmall },
  badgeText: { color: colors.background, fontSize: typography.eyebrow, fontFamily: fontFamily.bold },
});
