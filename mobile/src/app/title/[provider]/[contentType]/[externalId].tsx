import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Image } from 'expo-image';
import { Redirect, useLocalSearchParams, useRouter } from 'expo-router';
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
import { BackButton } from '@/components/navigation/back-button';
import { RatingReviewSection } from '@/components/reviews/rating-review-section';
import {
  useMarkMovieWatched,
  useMarkEpisodeWatched,
  useMovieTrackingStatus,
  usePendingEpisodes,
  useRemoveWatchedMovie,
  useRemoveWatchedEpisode,
  trackingQueryKeys,
  useSeasonProgress,
} from '@/hooks/use-tracking';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchCatalogSeason, fetchCatalogSeasons, fetchCatalogTitleDetails } from '@/services/api-client';
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
  const [seasonSelections, setSeasonSelections] = useState<Record<string, number>>({});
  const [trackingFeedback, setTrackingFeedback] = useState<string | null>(null);
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
  const seasons = useQuery({
    queryKey: ['catalog-seasons', provider, externalId],
    queryFn: ({ signal }) => {
      if (!provider || !externalId) {
        throw new Error('O endereço desta série é inválido.');
      }

      return fetchCatalogSeasons(provider, externalId, signal);
    },
    enabled: provider !== null && contentType === 'series' && externalId !== null && details.isSuccess,
    staleTime: 300_000,
  });
  useErrorToast(seasons.error, 'Não foi possível carregar as temporadas disponíveis.');
  const availableSeasons = seasons.data ?? [];
  const seriesKey = `${provider}:${externalId}`;
  const savedSeasonNumber = seasonSelections[seriesKey];
  const defaultSeasonNumber = availableSeasons.find((seasonInfo) => seasonInfo.seasonNumber > 0)?.seasonNumber ??
    availableSeasons[0]?.seasonNumber ?? null;
  const seasonNumber = availableSeasons.some((seasonInfo) => seasonInfo.seasonNumber === savedSeasonNumber)
    ? savedSeasonNumber
    : defaultSeasonNumber;
  const setSeasonNumber = (number: number) => {
    setSeasonSelections((current) => ({ ...current, [seriesKey]: number }));
  };
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
  const pendingEpisodes = usePendingEpisodes(
    season.data?.seriesId ?? '',
    seasonNumber ?? -1,
    provider ?? 'tmdb',
  );
  const progress = useSeasonProgress(
    season.data?.seriesId ?? '',
    seasonNumber ?? -1,
    provider ?? 'tmdb',
  );
  const markEpisode = useMarkEpisodeWatched();
  const removeEpisode = useRemoveWatchedEpisode();

  if (!session) {
    return <Redirect href="/" />;
  }

  if (!validParams) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="alert" style={styles.title}>
          O endereço deste título é inválido.
        </Text>
        <BackButton />
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
        <BackButton />
      </View>
    );
  }

  const title = details.data;
  const todayUtc = new Date().toISOString().slice(0, 10);
  const releaseDate = title.releaseDate
    ? new Intl.DateTimeFormat('pt-BR', { dateStyle: 'long' })
      .format(new Date(`${title.releaseDate}T00:00:00`))
    : null;

  return (
    <ScrollView contentContainerStyle={styles.content}>
      <BackButton />
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
      {title.contentType === 'movie' ? (
        <MovieTrackingControls titleId={title.id} title={title.title} />
      ) : null}
      {title.contentType === 'series' ? (
        <View style={styles.seasons}>
          <Text accessibilityRole="header" style={styles.sectionTitle}>Temporadas e episódios</Text>
          {seasons.isPending ? <ActivityIndicator color={colors.brand} /> : null}
          {seasons.isError ? (
            <PrimaryButton
              label="Tentar carregar temporadas"
              variant="secondary"
              onPress={() => void seasons.refetch()}
            />
          ) : null}
          {!seasons.isPending && !seasons.isError && availableSeasons.length === 0 ? (
            <Text style={styles.secondaryText}>Nenhuma temporada disponível no catálogo.</Text>
          ) : null}
          {availableSeasons.length > 0 ? (
            <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.seasonTabs}>
              {availableSeasons.map((seasonInfo) => {
                const selected = seasonInfo.seasonNumber === seasonNumber;
                return (
                  <Pressable
                    key={seasonInfo.seasonNumber}
                    accessibilityRole="tab"
                    accessibilityState={{ selected }}
                    onPress={() => setSeasonNumber(seasonInfo.seasonNumber)}
                    style={[styles.seasonTab, selected && styles.selectedSeasonTab]}
                  >
                    <Text style={[styles.seasonTabText, selected && styles.selectedSeasonTabText]}>
                      {seasonInfo.seasonNumber === 0 ? 'Especiais' : `Temporada ${seasonInfo.seasonNumber}`}
                    </Text>
                  </Pressable>
                );
              })}
            </ScrollView>
          ) : null}
          {season.isPending && seasonNumber !== null ? (
            <View style={styles.loadingSeason}>
              <ActivityIndicator color={colors.brand} />
              <Text style={styles.secondaryText}>Carregando episódios...</Text>
            </View>
          ) : null}
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
              {progress.data ? (
                <Text style={styles.secondaryText}>
                  {progress.data.watchedEpisodeCount} de {progress.data.availableEpisodeCount} episódios assistidos
                </Text>
              ) : null}
              {trackingFeedback ? (
                <Text accessibilityLiveRegion="polite" style={styles.trackingFeedback}>
                  {trackingFeedback}
                </Text>
              ) : null}
              {season.data.episodes.length === 0 ? (
                <Text style={styles.secondaryText}>Nenhum episódio disponível nesta temporada.</Text>
              ) : season.data.episodes.map((episode) => {
                const episodeIsPending = pendingEpisodes.data?.some(
                  (pending) => pending.episodeId === episode.id,
                );
                const episodeIsWatched =
                  !pendingEpisodes.isPending &&
                  !pendingEpisodes.isError &&
                  episodeIsPending === false;
                const episodeIsUpcoming =
                  episode.releaseDate !== null && episode.releaseDate > todayUtc;
                const trackingIsBusy = markEpisode.isPending || removeEpisode.isPending;
                return (
                  <View key={episode.id} style={styles.episodeCard}>
                    <View style={styles.episodeCopy}>
                      <Text style={styles.episodeTitle}>
                        {episode.episodeNumber === null
                          ? episode.title
                          : `E${episode.episodeNumber}: ${episode.title}`}
                      </Text>
                      {episode.releaseDate ? (
                        <Text style={styles.secondaryText}>
                          Lançamento: {formatCatalogDate(episode.releaseDate)}
                        </Text>
                      ) : null}
                      {episode.overview ? (
                        <Text numberOfLines={3} style={styles.secondaryText}>{episode.overview}</Text>
                      ) : null}
                    </View>
                    {pendingEpisodes.isPending ? (
                      <View style={styles.loadingSeason}>
                        <ActivityIndicator color={colors.brand} />
                        <Text style={styles.secondaryText}>Verificando acompanhamento...</Text>
                      </View>
                    ) : pendingEpisodes.isError ? (
                      <PrimaryButton
                        label="Tentar carregar estado"
                        variant="secondary"
                        onPress={() => void pendingEpisodes.refetch()}
                      />
                    ) : episodeIsUpcoming ? (
                      <Text style={styles.secondaryText}>Ainda não lançado</Text>
                    ) : (
                      <Pressable
                        accessibilityRole="checkbox"
                        accessibilityLabel={`${episodeIsPending ? 'Marcar como assistido' : 'Desmarcar como assistido'}: ${episode.title}`}
                        accessibilityState={{ checked: !episodeIsPending, disabled: trackingIsBusy }}
                        disabled={trackingIsBusy}
                        onPress={() => {
                          setTrackingFeedback(null);
                          if (episodeIsPending) {
                            markEpisode.mutate(
                              { seriesId: season.data.seriesId, episodeId: episode.id },
                              { onSuccess: () => setTrackingFeedback('Episódio marcado como assistido.') },
                            );
                          } else {
                            removeEpisode.mutate(
                              { seriesId: season.data.seriesId, episodeId: episode.id },
                              { onSuccess: () => setTrackingFeedback('Episódio removido do acompanhamento.') },
                            );
                          }
                        }}
                        style={styles.watchedToggle}
                      >
                        <Text style={[styles.watchedIcon, episodeIsPending && styles.unwatchedIcon]}>
                          {episodeIsPending ? '○' : '✓'}
                        </Text>
                      </Pressable>
                    )}
                    <PrimaryButton
                      label={pendingEpisodes.isPending
                        ? 'Verificando acompanhamento...'
                        : pendingEpisodes.isError
                          ? 'Não foi possível verificar o episódio'
                          : episodeIsPending
                            ? 'Marque como assistido para avaliar'
                            : 'Avaliar episódio'}
                      variant="secondary"
                      disabled={!episodeIsWatched || trackingIsBusy}
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
                );
              })}
            </View>
          ) : null}
        </View>
      ) : null}
      {title.contentType === 'series' ? (
        <RatingReviewSection
          key={`${title.contentType}:${title.id}:${session.user.id}`}
          targetId={title.id}
          targetType={title.contentType}
        />
      ) : null}
    </ScrollView>
  );
}

function MovieTrackingControls({ titleId, title }: { titleId: string; title: string }) {
  const queryClient = useQueryClient();
  const status = useMovieTrackingStatus(titleId);
  const markWatched = useMarkMovieWatched();
  const removeWatched = useRemoveWatchedMovie();
  const isWatched = status.data === true;
  const isBusy = markWatched.isPending || removeWatched.isPending;

  return (
    <View style={styles.movieTracking}>
      {status.isPending ? (
        <>
          <ActivityIndicator color={colors.brand} />
          <Text style={styles.secondaryText}>Verificando seu acompanhamento...</Text>
        </>
      ) : status.isError ? (
        <PrimaryButton
          label="Tentar carregar estado do filme"
          variant="secondary"
          onPress={() => void status.refetch()}
        />
      ) : (
        <Pressable
          accessibilityRole="checkbox"
          accessibilityLabel={isWatched ? `Desmarcar ${title} como assistido` : `Marcar ${title} como assistido`}
          accessibilityState={{ checked: isWatched, disabled: isBusy }}
          disabled={isBusy}
          onPress={() => {
            if (isWatched) {
              removeWatched.mutate(titleId, {
                onSuccess: () => queryClient.setQueryData(trackingQueryKeys.movieStatus(titleId), false),
              });
            } else {
              markWatched.mutate({ titleId }, {
                onSuccess: () => queryClient.setQueryData(trackingQueryKeys.movieStatus(titleId), true),
              });
            }
          }}
          style={[styles.movieWatchedToggle, isWatched && styles.movieWatchedSelected]}
        >
          <Text style={[styles.movieWatchedIcon, isWatched && styles.movieWatchedIconSelected]}>
            {isWatched ? '✓' : '○'}
          </Text>
          <Text style={styles.movieWatchedLabel}>
            {isWatched ? 'Assistido' : 'Marcar como assistido'}
          </Text>
        </Pressable>
      )}
      <RatingReviewSection
        key={`movie-reviews:${titleId}`}
        targetId={titleId}
        targetType="movie"
        allowRating={isWatched}
      />
    </View>
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
  seasonTabs: { gap: spacing.small, paddingEnd: spacing.medium },
  seasonTab: {
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.medium,
    borderRadius: radii.pill,
    backgroundColor: colors.surface,
  },
  selectedSeasonTab: { backgroundColor: colors.brand },
  seasonTabText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  selectedSeasonTabText: { color: colors.background },
  loadingSeason: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
    paddingVertical: spacing.medium,
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
  movieTracking: {
    gap: spacing.small,
  },
  movieWatchedToggle: {
    minHeight: dimensions.minimumTouchTarget,
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: spacing.small,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.pill,
    paddingHorizontal: spacing.medium,
    backgroundColor: colors.surface,
  },
  movieWatchedSelected: {
    borderColor: colors.success,
    backgroundColor: colors.surfaceElevated,
  },
  movieWatchedIcon: {
    color: colors.textSecondary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  movieWatchedIconSelected: {
    color: colors.success,
  },
  movieWatchedLabel: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  watchedToggle: {
    minHeight: dimensions.minimumTouchTarget,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
  },
  watchedIcon: {
    width: dimensions.minimumTouchTarget,
    height: dimensions.minimumTouchTarget,
    textAlign: 'center',
    textAlignVertical: 'center',
    color: colors.success,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  trackingFeedback: {
    color: colors.success,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  unwatchedIcon: { color: colors.textSecondary },
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

function formatCatalogDate(date: string) {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' })
    .format(new Date(`${date}T12:00:00`));
}
