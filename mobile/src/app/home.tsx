import { Redirect, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AppHeader } from '@/components/navigation/app-header';
import { PrimaryButton } from '@/components/ui/primary-button';
import { DiscoveryFeedSection } from '@/features/home/components/discovery-feed-section';
import { FavoriteTitleSection } from '@/features/home/components/favorite-title-section';
import { HomeContentTabs, type HomeContentType } from '@/features/home/components/home-content-tabs';
import { StartWatchingSection } from '@/features/home/components/start-watching-section';
import { TrackedSeriesSection } from '@/features/home/components/tracked-series-section';
import { UpcomingCalendarSection } from '@/features/home/components/upcoming-calendar-section';
import { useOnboardingPreferences } from '@/features/onboarding/hooks/use-onboarding-preferences';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export default function HomeScreen() {
  const router = useRouter();
  const [contentType, setContentType] = useState<HomeContentType>('series');
  const onboarding = useOnboardingPreferences(
    'Não foi possível carregar suas preferências. Tente novamente.',
    true,
  );

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
        <HomeContentTabs selected={contentType} onSelect={setContentType} />

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
