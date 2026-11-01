import { useQuery } from '@tanstack/react-query';
import { Image } from 'expo-image';
import { Redirect, useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

import { PrimaryButton } from '@/components/auth/primary-button';
import { RatingReviewSection } from '@/components/reviews/rating-review-section';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchCatalogSeason, fetchCatalogTitleDetails } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export default function CatalogTitleScreen() {
  const router = useRouter();
  const { session } = useAuthSession();
  const params = useLocalSearchParams<{
    provider?: string;
    contentType?: string;
    externalId?: string;
  }>();
  const provider = isCatalogProvider(params.provider) ? params.provider : null;
  const contentType = isCatalogContentType(params.contentType) ? params.contentType : null;
  const externalId = typeof params.externalId === 'string' ? params.externalId : null;
  const [seasonInput, setSeasonInput] = useState('');
  const [seasonNumber, setSeasonNumber] = useState<number | null>(null);
  const validParams = provider && contentType && externalId
    ? { provider, contentType, externalId }
    : null;

  const details = useQuery({
    queryKey: ['catalog', 'title', provider, contentType, externalId],
    queryFn: ({ signal }) => {
      if (!validParams) {
        throw new Error('O endereço deste título é inválido.');
      }

      return fetchCatalogTitleDetails(
        validParams.provider,
        validParams.contentType,
        validParams.externalId,
        signal,
      );
    },
    enabled: validParams !== null,
    staleTime: 300_000,
  });
  useErrorToast(details.error, 'Não foi possível carregar os detalhes deste título. Tente novamente.');
  const season = useQuery({
    queryKey: ['catalog-season', provider, externalId, seasonNumber],
    queryFn: ({ signal }) => {
      if (!provider || !externalId || seasonNumber === null) {
        throw new Error('Selecione uma temporada válida.');
      }

      return fetchCatalogSeason(provider, externalId, seasonNumber, signal);
    },
    enabled: provider !== null && contentType === 'series' && externalId !== null && seasonNumber !== null,
    staleTime: 300_000,
  });
  useErrorToast(season.error, 'Não foi possível carregar os episódios desta temporada.');

  if (!session) {
    return <Redirect href="/" />;
  }

  if (!validParams) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="alert" style={styles.title}>
          O endereço deste título é inválido.
        </Text>
        <PrimaryButton label="Voltar" onPress={() => router.back()} />
      </View>
    );
  }

  if (details.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.secondaryText}>Carregando detalhes...</Text>
      </View>
    );
  }

  if (details.isError || !details.data) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>
          Não foi possível abrir este título.
        </Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void details.refetch()} />
        <PrimaryButton label="Voltar" onPress={() => router.back()} variant="secondary" />
      </View>
    );
  }

  const title = details.data;
  const releaseDate = title.releaseDate
    ? new Intl.DateTimeFormat('pt-BR', { dateStyle: 'long' })
      .format(new Date(`${title.releaseDate}T00:00:00`))
    : null;

  return (
    <ScrollView contentContainerStyle={styles.content}>
      <Pressable
        accessibilityRole="button"
        onPress={() => router.back()}
        style={styles.backButton}
      >
        <Text style={styles.backText}>Voltar</Text>
      </Pressable>
      {title.backdropUrl ? (
        <Image
          contentFit="cover"
          source={{ uri: title.backdropUrl }}
          style={styles.backdrop}
          transition={150}
        />
      ) : null}
      <Text accessibilityRole="header" style={styles.title}>{title.title}</Text>
      {title.originalTitle && title.originalTitle !== title.title ? (
        <Text style={styles.secondaryText}>{title.originalTitle}</Text>
      ) : null}
      <View style={styles.metadata}>
        <Text style={styles.metadataText}>
          {title.contentType === 'movie' ? 'Filme' : 'Série'}
        </Text>
        {releaseDate ? <Text style={styles.metadataText}>{releaseDate}</Text> : null}
        {title.runtimeMinutes ? (
          <Text style={styles.metadataText}>{title.runtimeMinutes} min</Text>
        ) : null}
      </View>
      {title.genres.length > 0 ? (
        <Text style={styles.secondaryText}>{title.genres.join(' · ')}</Text>
      ) : null}
      {title.overview ? (
        <View style={styles.overview}>
          <Text style={styles.sectionTitle}>Sinopse</Text>
          <Text style={styles.body}>{title.overview}</Text>
        </View>
      ) : null}
      {title.contentType === 'series' ? (
        <View style={styles.seasons}>
          <Text accessibilityRole="header" style={styles.sectionTitle}>Temporadas e episódios</Text>
          <Text style={styles.secondaryText}>
            Informe o número da temporada para consultar episódios disponíveis.
          </Text>
          <TextInput
            accessibilityLabel="Número da temporada"
            keyboardType="number-pad"
            onChangeText={setSeasonInput}
            placeholder="Ex.: 1"
            placeholderTextColor={colors.textSecondary}
            style={styles.seasonInput}
            value={seasonInput}
          />
          <PrimaryButton
            label="Carregar episódios"
            disabled={!/^\d{1,3}$/.test(seasonInput.trim())}
            loading={season.isFetching}
            variant="secondary"
            onPress={() => setSeasonNumber(Number(seasonInput.trim()))}
          />
          {season.isError ? (
            <PrimaryButton
              label="Tentar novamente"
              variant="secondary"
              onPress={() => void season.refetch()}
            />
          ) : null}
          {season.data ? (
            <View style={styles.episodeList}>
              {season.data.title ? <Text style={styles.secondaryText}>{season.data.title}</Text> : null}
              {season.data.episodes.length === 0 ? (
                <Text style={styles.secondaryText}>Nenhum episódio disponível nesta temporada.</Text>
              ) : season.data.episodes.map((episode) => (
                <View key={episode.id} style={styles.episodeCard}>
                  <View style={styles.episodeCopy}>
                    <Text style={styles.episodeTitle}>
                      {episode.episodeNumber === null
                        ? episode.title
                        : `E${episode.episodeNumber}: ${episode.title}`}
                    </Text>
                    {episode.overview ? (
                      <Text numberOfLines={3} style={styles.secondaryText}>{episode.overview}</Text>
                    ) : null}
                  </View>
                  <PrimaryButton
                    label="Avaliar episódio"
                    variant="secondary"
                    onPress={() => router.push({
                      pathname: '/episode/[provider]/[externalSeriesId]/[seasonNumber]/[episodeId]',
                      params: {
                        provider: title.provider,
                        externalSeriesId: title.externalId,
                        seasonNumber: String(season.data.seasonNumber),
                        episodeId: episode.id,
                      },
                    })}
                  />
                </View>
              ))}
            </View>
          ) : null}
        </View>
      ) : null}
      <RatingReviewSection
        key={`${title.contentType}:${title.id}:${session.user.id}`}
        targetId={title.id}
        targetType={title.contentType}
      />
    </ScrollView>
  );
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
  backdrop: {
    width: '100%',
    aspectRatio: 16 / 9,
    borderRadius: radii.large,
    backgroundColor: colors.surface,
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
  },
  metadata: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.small,
  },
  metadataText: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.medium,
  },
  overview: {
    gap: spacing.small,
  },
  seasons: {
    gap: spacing.small,
  },
  seasonInput: {
    minHeight: dimensions.minimumTouchTarget,
    paddingHorizontal: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  episodeList: {
    gap: spacing.small,
    marginTop: spacing.small,
  },
  episodeCard: {
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  episodeCopy: {
    gap: spacing.extraSmall,
  },
  episodeTitle: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  body: {
    color: colors.textSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
    lineHeight: typography.body * 1.6,
  },
});

function isCatalogProvider(value: unknown): value is 'tmdb' | 'omdb' {
  return value === 'tmdb' || value === 'omdb';
}

function isCatalogContentType(value: unknown): value is 'movie' | 'series' {
  return value === 'movie' || value === 'series';
}
