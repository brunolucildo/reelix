import { useQuery } from '@tanstack/react-query';
import { Redirect, useLocalSearchParams, useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/auth/primary-button';
import { RatingReviewSection } from '@/components/reviews/rating-review-section';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchCatalogSeason, type CatalogTitle } from '@/services/api-client';
import { colors, dimensions, fontFamily, spacing, typography } from '@/theme/tokens';

export default function CatalogEpisodeScreen() {
  const router = useRouter();
  const { session } = useAuthSession();
  const params = useLocalSearchParams<{
    provider?: string;
    externalSeriesId?: string;
    seasonNumber?: string;
    episodeId?: string;
  }>();
  const provider = isCatalogProvider(params.provider) ? params.provider : null;
  const externalSeriesId = typeof params.externalSeriesId === 'string'
    ? params.externalSeriesId
    : null;
  const seasonNumber = typeof params.seasonNumber === 'string' &&
    /^\d{1,3}$/.test(params.seasonNumber)
    ? Number(params.seasonNumber)
    : null;
  const episodeId = typeof params.episodeId === 'string' ? params.episodeId : null;
  const isValid = provider !== null && externalSeriesId !== null &&
    seasonNumber !== null && episodeId !== null;

  const season = useQuery({
    queryKey: ['catalog-season', provider, externalSeriesId, seasonNumber],
    queryFn: ({ signal }) => {
      if (!provider || !externalSeriesId || seasonNumber === null) {
        throw new Error('O endereço deste episódio é inválido.');
      }

      return fetchCatalogSeason(provider, externalSeriesId, seasonNumber, signal);
    },
    enabled: isValid,
    staleTime: 300_000,
  });
  useErrorToast(season.error, 'Não foi possível carregar os detalhes do episódio.');

  if (!session) {
    return <Redirect href="/" />;
  }

  if (!isValid) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="alert" style={styles.title}>O endereço deste episódio é inválido.</Text>
        <PrimaryButton label="Voltar" onPress={() => router.back()} />
      </View>
    );
  }

  if (season.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.secondaryText}>Carregando episódio...</Text>
      </View>
    );
  }

  if (season.isError || !season.data) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>Não foi possível abrir este episódio.</Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void season.refetch()} />
        <PrimaryButton label="Voltar" variant="secondary" onPress={() => router.back()} />
      </View>
    );
  }

  const episode = season.data.episodes.find((item) => item.id === episodeId);
  if (!episode) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>Episódio não encontrado nesta temporada.</Text>
        <PrimaryButton label="Voltar" onPress={() => router.back()} />
      </View>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.content}>
      <Pressable
        accessibilityRole="button"
        onPress={() => router.back()}
        style={styles.backButton}
      >
        <Text style={styles.backText}>Voltar</Text>
      </Pressable>
      <Text style={styles.secondaryText}>
        {season.data.title ?? `Temporada ${season.data.seasonNumber}`}
        {episode.episodeNumber === null ? '' : ` · Episódio ${episode.episodeNumber}`}
      </Text>
      <Text accessibilityRole="header" style={styles.title}>{episode.title}</Text>
      {episode.overview ? <Text style={styles.secondaryText}>{episode.overview}</Text> : null}
      <RatingReviewSection
        key={`episode:${episode.id}:${session.user.id}`}
        targetId={episode.id}
        targetType="episode"
        seriesId={season.data.seriesId}
      />
    </ScrollView>
  );
}

function isCatalogProvider(value: unknown): value is CatalogTitle['provider'] {
  return value === 'tmdb' || value === 'omdb';
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    gap: spacing.medium,
    padding: spacing.medium,
    paddingTop: spacing.large,
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
  backButton: {
    minHeight: dimensions.minimumTouchTarget,
    alignSelf: 'flex-start',
    justifyContent: 'center',
    paddingHorizontal: spacing.small,
  },
  backText: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.bold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
    lineHeight: 21,
  },
});
