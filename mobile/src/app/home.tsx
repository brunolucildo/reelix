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
import { StartWatchingSection } from '@/components/discovery/start-watching-section';
import { TrackedSeriesSection } from '@/components/discovery/tracked-series-section';
import { UpcomingCalendarSection } from '@/components/discovery/upcoming-calendar-section';
import { AppHeader } from '@/components/navigation/app-header';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchOnboarding } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type ContentType = 'movie' | 'series';

export default function HomeScreen() {
  const router = useRouter();
  const { session } = useAuthSession();
  const [contentType, setContentType] = useState<ContentType>('series');
  const onboarding = useQuery({
    queryKey: ['onboarding'],
    queryFn: fetchOnboarding,
  });

  const onboardingErrorMessage = onboarding.error instanceof TypeError
    ? 'Não foi possível conectar à API. Verifique a rede e tente novamente.'
    : onboarding.error instanceof Error &&
        'status' in onboarding.error &&
        typeof onboarding.error.status === 'number'
      ? `A API recusou a solicitação (HTTP ${onboarding.error.status}).`
      : 'Não foi possível carregar suas preferências. Tente novamente.';

  useErrorToast(
    onboarding.error,
    onboardingErrorMessage,
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

  return (
    <SafeAreaView style={styles.screen}>
    <ScrollView contentContainerStyle={styles.content}>
      <AppHeader />

      <View accessibilityRole="tablist" style={styles.contentTabs}>
        {(['series', 'movie'] as const).map((type) => {
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

      <View style={styles.feeds}>
        {contentType === 'movie' ? <FavoriteTitleSection type="movie" title="Quero assistir" collection="watchlist" /> : null}
        {contentType === 'series' ? (
          <>
            <TrackedSeriesSection onOpenTracking={() => router.push('/tracking')} />
            <StartWatchingSection />
            <UpcomingCalendarSection />
          </>
        ) : null}
        <DiscoveryFeedSection title={`${label} em destaque`} type={contentType} category="popular" />
        <FavoriteTitleSection
          type={contentType}
          title={contentType === 'movie' ? 'Seus filmes favoritos' : 'Suas séries favoritas'}
        />
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

    </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background },
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
});
