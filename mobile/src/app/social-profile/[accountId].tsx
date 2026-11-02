import { useLocalSearchParams, useRouter } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/auth/primary-button';
import { BackButton } from '@/components/navigation/back-button';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchFriendProfile, fetchPublicGamificationBadges } from '@/services/api-client';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export default function SocialProfileScreen() {
  const router = useRouter();
  const { accountId } = useLocalSearchParams<{ accountId: string }>();
  const profile = useQuery({
    queryKey: ['social', 'friend-profile', accountId],
    queryFn: ({ signal }) => fetchFriendProfile(accountId, signal),
    enabled: Boolean(accountId),
  });
  const badges = useQuery({
    queryKey: ['gamification', 'public-badges', accountId],
    queryFn: ({ signal }) => fetchPublicGamificationBadges(accountId, signal),
    enabled: Boolean(accountId),
  });
  useErrorToast(profile.error, 'Não foi possível carregar o perfil do amigo.');
  useErrorToast(badges.error, 'Não foi possível carregar os badges compartilhados.');

  if (profile.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
      </View>
    );
  }

  if (profile.isError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>Perfil indisponível</Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void profile.refetch()} />
        <BackButton />
      </View>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.content}>
      <View style={styles.container}>
        <BackButton />
        <Text accessibilityRole="header" style={styles.title}>{profile.data.displayName}</Text>
        {profile.data.biography ? <Text style={styles.body}>{profile.data.biography}</Text> : null}
        <Text style={styles.sectionTitle}>Conquistas</Text>
        {badges.isPending ? (
          <ActivityIndicator color={colors.brand} />
        ) : badges.isError ? (
          <Pressable accessibilityRole="button" onPress={() => void badges.refetch()}>
            <Text style={styles.body}>Tentar carregar os badges novamente.</Text>
          </Pressable>
        ) : !badges.data.isPublic ? (
          <Text style={styles.body}>Este amigo não compartilha badges.</Text>
        ) : badges.data.badges.length === 0 ? (
          <Text style={styles.body}>Ainda não há conquistas públicas.</Text>
        ) : (
          badges.data.badges.map((badge) => (
            <View key={badge.code} style={styles.badge}>
              <Text style={styles.badgeTitle}>{badge.title}</Text>
              <Text style={styles.body}>{badge.description}</Text>
              <Text style={styles.badgeDate}>
                Conquistada em {new Date(badge.firstEarnedAt).toLocaleDateString('pt-BR')}
              </Text>
            </View>
          ))
        )}
        <Text style={styles.sectionTitle}>Favoritos compartilhados</Text>
        {profile.data.favorites.length === 0 ? (
          <Text style={styles.body}>Este amigo não compartilhou títulos favoritos.</Text>
        ) : (
          profile.data.favorites.map((favorite) => (
            <Pressable
              key={favorite.titleId}
              accessibilityRole="button"
              onPress={() => router.push({
                pathname: '/title/[provider]/[contentType]/[externalId]',
                params: {
                  provider: favorite.provider,
                  contentType: favorite.contentType,
                  externalId: favorite.externalId,
                },
              })}
            >
              <Text style={styles.body}>
                Abrir {favorite.contentType === 'movie' ? 'filme' : 'série'} favorito
              </Text>
            </Pressable>
          ))
        )}
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  container: {
    width: '100%',
    maxWidth: 560,
    alignSelf: 'center',
    gap: spacing.medium,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.medium,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  backLabel: {
    minHeight: 44,
    paddingTop: spacing.small,
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.extraBold,
  },
  sectionTitle: {
    marginTop: spacing.large,
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  body: {
    color: colors.textSecondary,
    fontSize: typography.body,
    lineHeight: 24,
    fontFamily: fontFamily.regular,
  },
  badge: {
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    backgroundColor: colors.surface,
  },
  badgeTitle: {
    color: colors.brandSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  badgeDate: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
});
