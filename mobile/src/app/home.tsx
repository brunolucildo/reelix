import { useQuery } from '@tanstack/react-query';
import { Redirect, useRouter } from 'expo-router';
import { useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';

import { PrimaryButton } from '@/components/auth/primary-button';
import { DiscoveryFeedSection } from '@/components/discovery/discovery-feed-section';
import { FavoriteTitleSection } from '@/components/discovery/favorite-title-section';
import { TrackedSeriesSection } from '@/components/discovery/tracked-series-section';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast, useToast } from '@/providers/toast-provider';
import { fetchOnboarding } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type ContentType = 'movie' | 'series';

export default function HomeScreen() {
  const router = useRouter();
  const { session, signOut } = useAuthSession();
  const { showError } = useToast();
  const [contentType, setContentType] = useState<ContentType>('movie');
  const onboarding = useQuery({
    queryKey: ['onboarding'],
    queryFn: fetchOnboarding,
  });

  useErrorToast(
    onboarding.error,
    'Não foi possível carregar suas preferências. Verifique a conexão e tente novamente.',
  );

  if (!session) {
    return <Redirect href="/" />;
  }

  if (onboarding.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.secondaryText}>Preparando sua Home...</Text>
      </View>
    );
  }

  if (onboarding.isError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>
          Não foi possível abrir a Home.
        </Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void onboarding.refetch()} />
      </View>
    );
  }

  if (!onboarding.data.isComplete) {
    return <Redirect href="/onboarding" />;
  }

  const genreIds = contentType === 'movie'
    ? onboarding.data.movieGenreIds
    : onboarding.data.seriesGenreIds;
  const label = contentType === 'movie' ? 'Filmes' : 'Séries';

  const handleSignOut = async () => {
    try {
      await signOut();
    } catch {
      showError('Não foi possível confirmar o encerramento da sessão. Feche o app e tente novamente.');
    }
  };

  return (
    <ScrollView contentContainerStyle={styles.content}>
      <View style={styles.header}>
        <View style={styles.brand}>
          <Text style={styles.brandName}>reelix</Text>
          <Text style={styles.subtitle}>Descubra sua próxima história.</Text>
        </View>
        <Pressable
          accessibilityLabel="Pesquisar no catálogo"
          accessibilityRole="button"
          onPress={() => router.push('./search')}
          style={styles.searchButton}
        >
          <Text style={styles.searchButtonText}>Pesquisar</Text>
        </Pressable>
      </View>

      <View accessibilityRole="tablist" style={styles.contentTabs}>
        {(['movie', 'series'] as const).map((type) => {
          const selected = type === contentType;
          return (
            <Pressable
              key={type}
              accessibilityRole="tab"
              accessibilityState={{ selected }}
              onPress={() => setContentType(type)}
              style={[styles.tab, selected && styles.selectedTab]}
            >
              <Text style={[styles.tabText, selected && styles.selectedTabText]}>
                {type === 'movie' ? 'Filmes' : 'Séries'}
              </Text>
            </Pressable>
          );
        })}
      </View>

      <View style={styles.quickLinks}>
        <Pressable
          accessibilityRole="button"
          onPress={() => router.push('/profile')}
          style={styles.quickLink}
        >
          <Text style={styles.quickLinkText}>Meu perfil</Text>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          onPress={() => router.push('/lists')}
          style={styles.quickLink}
        >
          <Text style={styles.quickLinkText}>Favoritos e listas</Text>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          onPress={() => router.push('/tracking')}
          style={styles.quickLink}
        >
          <Text style={styles.quickLinkText}>Acompanhamento</Text>
        </Pressable>
      </View>

      <View style={styles.feeds}>
        <DiscoveryFeedSection title={`${label} em destaque`} type={contentType} category="popular" />
        <FavoriteTitleSection
          type={contentType}
          title={contentType === 'movie' ? 'Seus filmes favoritos' : 'Suas séries favoritas'}
        />
        {contentType === 'series' ? (
          <TrackedSeriesSection onOpenTracking={() => router.push('/tracking')} />
        ) : null}
        {genreIds.length > 0 ? (
          <DiscoveryFeedSection
            title="Sugestões para você"
            type={contentType}
            category="popular"
            genreIds={genreIds}
          />
        ) : (
          <View style={styles.preferencePrompt}>
            <Text style={styles.promptTitle}>Personalize suas sugestões</Text>
            <Text style={styles.secondaryText}>
              Escolha gêneros para receber recomendações mais próximas do seu gosto.
            </Text>
            <PrimaryButton
              label="Editar preferências"
              onPress={() => router.push({ pathname: '/onboarding', params: { edit: '1' } })}
              variant="secondary"
            />
          </View>
        )}
        <DiscoveryFeedSection
          title={contentType === 'movie' ? 'Próximos lançamentos' : 'Estreias de séries'}
          type={contentType}
          category="upcoming"
        />
      </View>

      <Pressable
        accessibilityRole="button"
        onPress={() => void handleSignOut()}
        style={styles.signOut}
      >
        <Text style={styles.signOutText}>Sair da conta</Text>
      </Pressable>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: {
    gap: spacing.large,
    padding: spacing.medium,
    paddingBottom: spacing.extraLarge,
    backgroundColor: colors.background,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.medium,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.medium,
    paddingTop: spacing.large,
  },
  brand: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  brandName: {
    color: colors.brand,
    fontSize: typography.title,
    fontFamily: fontFamily.extraBold,
  },
  subtitle: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
    textAlign: 'center',
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  searchButton: {
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.medium,
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
  },
  searchButtonText: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  contentTabs: {
    flexDirection: 'row',
    gap: spacing.small,
  },
  tab: {
    minHeight: dimensions.minimumTouchTarget,
    minWidth: dimensions.posterWidth,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    backgroundColor: colors.surface,
  },
  selectedTab: {
    backgroundColor: colors.brand,
  },
  tabText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  selectedTabText: {
    color: colors.background,
  },
  quickLinks: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.small,
  },
  quickLink: {
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.pill,
  },
  quickLinkText: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.medium,
  },
  feeds: {
    gap: spacing.large,
  },
  preferencePrompt: {
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  promptTitle: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  signOut: {
    minHeight: dimensions.minimumTouchTarget,
    alignItems: 'center',
    justifyContent: 'center',
  },
  signOutText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.medium,
  },
});
