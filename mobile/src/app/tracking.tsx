import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Redirect, useRouter } from 'expo-router';
import { ActivityIndicator, StyleSheet, Text, TextInput, View } from 'react-native';

import { AuthScreen } from '@/components/auth/auth-screen';
import { PrimaryButton } from '@/components/auth/primary-button';
import {
  useMarkMovieWatched,
  useMarkEpisodeWatched,
  usePendingEpisodes,
  useRemoveWatchedEpisode,
  useRemoveWatchedMovie,
  useTrackedSeries,
  useTrackingHistory,
} from '@/hooks/use-tracking';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast } from '@/providers/toast-provider';
import { searchCatalog } from '@/services/api-client';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

const PAGE_SIZE = 10;
const watchDateFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'medium',
  timeStyle: 'short',
});

export default function TrackingScreen() {
  const { session, isLoading } = useAuthSession();

  if (isLoading) {
    return <LoadingState label="Carregando sua sessão..." />;
  }

  if (!session) {
    return <Redirect href="/" />;
  }

  return <AuthenticatedTracking />;
}

function AuthenticatedTracking() {
  const router = useRouter();
  const [historyPage, setHistoryPage] = useState(1);
  const [seriesPage, setSeriesPage] = useState(1);
  const [movieSearch, setMovieSearch] = useState('');
  const [submittedSearch, setSubmittedSearch] = useState('');
  const [searchFocused, setSearchFocused] = useState(false);
  const [searchValidation, setSearchValidation] = useState<string | null>(null);
  const [saveConfirmation, setSaveConfirmation] = useState<string | null>(null);
  const [editingEntryId, setEditingEntryId] = useState<string | null>(null);
  const [watchedAtDraft, setWatchedAtDraft] = useState('');
  const [watchedAtValidation, setWatchedAtValidation] = useState<string | null>(null);
  const [seriesSearch, setSeriesSearch] = useState('');
  const [submittedSeriesSearch, setSubmittedSeriesSearch] = useState('');
  const [selectedSeries, setSelectedSeries] = useState<{ id: string; title: string } | null>(null);
  const [seasonInput, setSeasonInput] = useState('');
  const [seasonNumber, setSeasonNumber] = useState(-1);
  const [episodeValidation, setEpisodeValidation] = useState<string | null>(null);
  const [episodeFocusedInput, setEpisodeFocusedInput] = useState<'search' | 'season' | null>(null);
  const history = useTrackingHistory(historyPage, PAGE_SIZE);
  const series = useTrackedSeries(seriesPage, PAGE_SIZE);
  const markMovie = useMarkMovieWatched();
  const removeMovie = useRemoveWatchedMovie();
  const markEpisode = useMarkEpisodeWatched();
  const removeEpisode = useRemoveWatchedEpisode();
  const pendingEpisodes = usePendingEpisodes(selectedSeries?.id ?? '', seasonNumber);
  const seriesResults = useQuery({
    queryKey: ['catalog', 'tracking-series-search', submittedSeriesSearch],
    queryFn: () => searchCatalog(submittedSeriesSearch, 'series'),
    enabled: submittedSeriesSearch.length > 0,
  });
  useErrorToast(
    seriesResults.error,
    seriesResults.error instanceof Error &&
      'status' in seriesResults.error &&
      seriesResults.error.status === 401
      ? 'Sua sessão expirou. Entre novamente para continuar.'
      : 'Não foi possível buscar séries. Tente novamente.',
  );
  const movieResults = useQuery({
    queryKey: ['catalog', 'tracking-movie-search', submittedSearch],
    queryFn: () => searchCatalog(submittedSearch, 'movie'),
    enabled: submittedSearch.length > 0,
  });
  useErrorToast(
    movieResults.error,
    movieResults.error instanceof Error &&
      'status' in movieResults.error &&
      movieResults.error.status === 401
      ? 'Sua sessão expirou. Entre novamente para continuar.'
      : 'Não foi possível buscar filmes. Tente novamente.',
  );
  const isSaving = markMovie.isPending || removeMovie.isPending ||
    markEpisode.isPending || removeEpisode.isPending;

  const beginEditingWatchedAt = (entryId: string, watchedAt: string) => {
    setEditingEntryId(entryId);
    setWatchedAtDraft(toLocalDateTimeInput(watchedAt));
    setWatchedAtValidation(null);
    setSaveConfirmation(null);
  };

  const cancelEditingWatchedAt = () => {
    setEditingEntryId(null);
    setWatchedAtDraft('');
    setWatchedAtValidation(null);
  };

  const saveWatchedAt = (entry: {
    id: string;
    type: 'movie' | 'episode';
    targetId: string;
    seriesId: string | null;
  }) => {
    const watchedAt = parseLocalDateTimeInput(watchedAtDraft);
    if (!watchedAt) {
      setWatchedAtValidation('Informe uma data e hora válidas.');
      return;
    }

    setWatchedAtValidation(null);
    setSaveConfirmation(null);
    const onSuccess = () => {
      setSaveConfirmation('Data da visualização atualizada.');
      cancelEditingWatchedAt();
    };

    if (entry.type === 'movie') {
      markMovie.mutate({ titleId: entry.targetId, watchedAt }, { onSuccess });
    } else if (entry.seriesId) {
      markEpisode.mutate({
        seriesId: entry.seriesId,
        episodeId: entry.targetId,
        watchedAt,
      }, { onSuccess });
    } else {
      setWatchedAtValidation('Não foi possível identificar a série deste episódio.');
    }
  };

  const submitSeriesSearch = () => {
    const query = seriesSearch.trim();
    if (query.length === 0 || query.length > 200) {
      setEpisodeValidation('Digite o nome de uma série, com até 200 caracteres.');
      return;
    }
    setEpisodeValidation(null);
    setSelectedSeries(null);
    setSeasonNumber(-1);
    setSaveConfirmation(null);
    if (query === submittedSeriesSearch) {
      void seriesResults.refetch();
    } else {
      setSubmittedSeriesSearch(query);
    }
  };

  const loadPendingEpisodes = () => {
    const value = seasonInput.trim();
    const parsedSeason = Number(value);
    if (!/^\d+$/.test(value) || !Number.isSafeInteger(parsedSeason) || parsedSeason > 2147483647) {
      setEpisodeValidation('Informe um número de temporada válido. Use 0 para especiais.');
      return;
    }
    setEpisodeValidation(null);
    setSaveConfirmation(null);
    if (parsedSeason === seasonNumber) {
      void pendingEpisodes.refetch();
    } else {
      setSeasonNumber(parsedSeason);
    }
  };

  const submitMovieSearch = () => {
    const query = movieSearch.trim();
    if (query.length === 0 || query.length > 200) {
      setSearchValidation('Digite o nome de um filme, com até 200 caracteres.');
      return;
    }

    setSearchValidation(null);
    setSaveConfirmation(null);
    if (query === submittedSearch) {
      void movieResults.refetch();
    } else {
      setSubmittedSearch(query);
    }
  };

  return (
    <AuthScreen
      title="Seu acompanhamento"
      subtitle="Veja seu histórico de visualização e os episódios que você já assistiu."
    >
      <PrimaryButton
        label="Voltar"
        variant="secondary"
        onPress={() => {
          if (router.canGoBack()) {
            router.back();
          } else {
            router.replace('/home');
          }
        }}
      />
      <View style={styles.section}>
        <Text accessibilityRole="header" style={styles.sectionTitle}>Registrar um filme</Text>
        <Text style={styles.description}>
          Busque no catálogo e marque como assistido. A data será registrada automaticamente.
          Para desmarcar, use seu histórico.
        </Text>
        <TextInput
          accessibilityLabel="Nome do filme para buscar no catálogo"
          value={movieSearch}
          onChangeText={(value) => {
            setMovieSearch(value);
            setSearchValidation(null);
          }}
          onFocus={() => setSearchFocused(true)}
          onBlur={() => setSearchFocused(false)}
          onSubmitEditing={submitMovieSearch}
          placeholder="Nome do filme"
          placeholderTextColor={colors.textSecondary}
          returnKeyType="search"
          style={[styles.searchInput, searchFocused ? styles.searchInputFocused : null]}
        />
        {searchValidation ? (
          <Text accessibilityRole="alert" style={styles.validation}>{searchValidation}</Text>
        ) : null}
        <PrimaryButton
          label="Buscar filme"
          onPress={submitMovieSearch}
          loading={movieResults.isFetching}
        />
        {submittedSearch.length > 0 ? (
          movieResults.isPending ? (
            <LoadingState label="Buscando filmes..." />
          ) : movieResults.isError && !movieResults.data ? (
            <RetryState onRetry={() => void movieResults.refetch()} />
          ) : movieResults.data ? (
            <>
              {movieResults.data.length === 0 ? (
                <Text style={styles.description}>Nenhum filme encontrado para esta busca.</Text>
              ) : (
                movieResults.data.map((movie) => (
                  <View key={movie.id} style={styles.card}>
                    <Text style={styles.cardTitle}>{movie.title}</Text>
                    <PrimaryButton
                      label={`Marcar ${movie.title} como assistido`}
                      disabled={isSaving}
                      onPress={() => {
                        setSaveConfirmation(null);
                        markMovie.mutate({ titleId: movie.id }, {
                          onSuccess: () => {
                            setSaveConfirmation(`${movie.title} registrado como assistido.`);
                            setHistoryPage(1);
                          },
                        });
                      }}
                    />
                  </View>
                ))
              )}
              {movieResults.isError ? (
                <RetryState onRetry={() => void movieResults.refetch()} />
              ) : null}
            </>
          ) : null
        ) : null}
      </View>
      <View style={styles.section}>
        <Text accessibilityRole="header" style={styles.sectionTitle}>Registrar episódios</Text>
        <Text style={styles.description}>
          Busque uma série e escolha a temporada. Use 0 para episódios especiais.
          Para desmarcar um episódio, use seu histórico.
        </Text>
        <TextInput
          accessibilityLabel="Nome da série para buscar no catálogo"
          value={seriesSearch}
          onChangeText={(value) => {
            setSeriesSearch(value);
            setEpisodeValidation(null);
          }}
          onFocus={() => setEpisodeFocusedInput('search')}
          onBlur={() => setEpisodeFocusedInput(null)}
          onSubmitEditing={submitSeriesSearch}
          placeholder="Nome da série"
          placeholderTextColor={colors.textSecondary}
          returnKeyType="search"
          style={[styles.searchInput, episodeFocusedInput === 'search' ? styles.searchInputFocused : null]}
        />
        <PrimaryButton
          label="Buscar série"
          onPress={submitSeriesSearch}
          loading={seriesResults.isFetching}
          disabled={isSaving}
        />
        {submittedSeriesSearch.length > 0 ? (
          seriesResults.isPending ? (
            <LoadingState label="Buscando séries..." />
          ) : seriesResults.isError && !seriesResults.data ? (
            <RetryState onRetry={() => void seriesResults.refetch()} />
          ) : seriesResults.data ? (
            <>
              {seriesResults.data.length === 0 ? (
                <Text style={styles.description}>Nenhuma série encontrada para esta busca.</Text>
              ) : seriesResults.data.map((item) => (
                <View key={item.id} style={styles.card}>
                  <Text style={styles.cardTitle}>{item.title}</Text>
                  {selectedSeries?.id === item.id ? (
                    <Text accessibilityLiveRegion="polite" style={styles.watchedCount}>Série selecionada</Text>
                  ) : (
                    <PrimaryButton
                      label={`Selecionar ${item.title}`}
                      variant="secondary"
                      disabled={isSaving}
                      onPress={() => {
                        setSelectedSeries({ id: item.id, title: item.title });
                        setSeasonInput('');
                        setSeasonNumber(-1);
                        setEpisodeValidation(null);
                        setSaveConfirmation(null);
                      }}
                    />
                  )}
                </View>
              ))}
              {seriesResults.isError ? <RetryState onRetry={() => void seriesResults.refetch()} /> : null}
            </>
          ) : null
        ) : null}
        {selectedSeries ? (
          <>
            <Text style={styles.cardTitle}>{selectedSeries.title}</Text>
            <TextInput
              accessibilityLabel={`Número da temporada de ${selectedSeries.title}; zero para especiais`}
              value={seasonInput}
              onChangeText={(value) => {
                setSeasonInput(value);
                setEpisodeValidation(null);
              }}
              onFocus={() => setEpisodeFocusedInput('season')}
              onBlur={() => setEpisodeFocusedInput(null)}
              onSubmitEditing={loadPendingEpisodes}
              placeholder="Número da temporada"
              placeholderTextColor={colors.textSecondary}
              keyboardType="number-pad"
              style={[styles.searchInput, episodeFocusedInput === 'season' ? styles.searchInputFocused : null]}
            />
            <PrimaryButton
              label="Consultar episódios pendentes"
              onPress={loadPendingEpisodes}
              loading={pendingEpisodes.isFetching}
              disabled={isSaving}
            />
            {seasonNumber >= 0 ? (
              <>
                <Text style={styles.description}>Temporada consultada: {seasonNumber}</Text>
                {pendingEpisodes.isPending ? (
                  <LoadingState label="Carregando episódios pendentes..." />
                ) : pendingEpisodes.isError && !pendingEpisodes.data ? (
                  <RetryState onRetry={() => void pendingEpisodes.refetch()} />
                ) : pendingEpisodes.data ? (
                  <>
                    {pendingEpisodes.data.length === 0 ? (
                      <Text style={styles.description}>
                        Não há episódios disponíveis pendentes nesta temporada.
                        Isso não confirma a conclusão da série.
                      </Text>
                    ) : pendingEpisodes.data.map((episode) => (
                      <View key={episode.episodeId} style={styles.card}>
                        <Text style={styles.cardTitle}>{episode.title}</Text>
                        <Text style={styles.description}>
                          {episode.episodeNumber === null
                            ? 'Episódio sem numeração'
                            : `Episódio ${episode.episodeNumber}`}
                        </Text>
                        <PrimaryButton
                          label={`Marcar ${episode.title} como assistido`}
                          disabled={isSaving}
                          onPress={() => {
                            setSaveConfirmation(null);
                            markEpisode.mutate({
                              seriesId: selectedSeries.id,
                              episodeId: episode.episodeId,
                            }, {
                              onSuccess: () => {
                                setSaveConfirmation(`${episode.title} registrado como assistido.`);
                                setHistoryPage(1);
                                setSeriesPage(1);
                              },
                            });
                          }}
                        />
                      </View>
                    ))}
                    {pendingEpisodes.isError ? (
                      <RetryState onRetry={() => void pendingEpisodes.refetch()} />
                    ) : null}
                  </>
                ) : null}
              </>
            ) : null}
          </>
        ) : null}
        {episodeValidation ? (
          <Text accessibilityRole="alert" style={styles.validation}>{episodeValidation}</Text>
        ) : null}
      </View>
      <View style={styles.section}>
        {saveConfirmation ? (
          <Text accessibilityLiveRegion="polite" style={styles.watchedCount}>
            {saveConfirmation}
          </Text>
        ) : null}
        {isSaving ? <LoadingState label="Salvando seu acompanhamento..." /> : null}
      </View>
      <View style={styles.section}>
        <Text accessibilityRole="header" style={styles.sectionTitle}>Histórico</Text>
        <Text style={styles.description}>Do mais recente ao mais antigo, no seu horário local.</Text>
        {history.isPending ? (
          <LoadingState label="Carregando seu histórico..." />
        ) : history.isError && !history.data ? (
          <RetryState onRetry={() => void history.refetch()} />
        ) : history.data ? (
          <>
            {history.data.items.length === 0 ? (
              <Text style={styles.description}>
                {history.data.totalCount === 0
                  ? 'Você ainda não registrou visualizações.'
                  : 'Não há registros nesta página.'}
              </Text>
            ) : (
              <>
                <Text style={styles.description}>
                  Os nomes dos títulos ainda não estão disponíveis neste histórico.
                </Text>
                {history.data.items.map((entry) => (
                  <View key={entry.id} style={styles.card}>
                    <Text style={styles.cardTitle}>
                      {entry.type === 'movie' ? 'Filme assistido' : 'Episódio assistido'}
                    </Text>
                    <Text style={styles.description}>
                      {watchDateFormatter.format(new Date(entry.watchedAt))}
                    </Text>
                    {editingEntryId === entry.id ? (
                      <>
                        <Text style={styles.description}>Nova data e hora (horário local)</Text>
                        <TextInput
                          accessibilityLabel="Nova data e hora da visualização"
                          value={watchedAtDraft}
                          onChangeText={(value) => {
                            setWatchedAtDraft(value);
                            setWatchedAtValidation(null);
                          }}
                          placeholder="DD/MM/AAAA HH:mm"
                          placeholderTextColor={colors.textSecondary}
                          keyboardType="numbers-and-punctuation"
                          style={styles.searchInput}
                        />
                        {watchedAtValidation ? (
                          <Text accessibilityRole="alert" style={styles.validation}>
                            {watchedAtValidation}
                          </Text>
                        ) : null}
                        <PrimaryButton
                          label="Salvar nova data"
                          disabled={isSaving}
                          onPress={() => saveWatchedAt(entry)}
                        />
                        <PrimaryButton
                          label="Cancelar alteração"
                          variant="secondary"
                          disabled={isSaving}
                          onPress={cancelEditingWatchedAt}
                        />
                      </>
                    ) : (
                      <PrimaryButton
                        label="Corrigir data e hora"
                        variant="secondary"
                        disabled={isSaving}
                        onPress={() => beginEditingWatchedAt(entry.id, entry.watchedAt)}
                      />
                    )}
                    {entry.type === 'movie' ? (
                      <PrimaryButton
                        label={`Desmarcar filme assistido em ${watchDateFormatter.format(new Date(entry.watchedAt))}`}
                        variant="secondary"
                        disabled={isSaving}
                        onPress={() => {
                          setSaveConfirmation(null);
                          removeMovie.mutate(entry.targetId, {
                            onSuccess: () => {
                              setSaveConfirmation('Filme removido do seu histórico.');
                              setHistoryPage(1);
                            },
                          });
                        }}
                      />
                    ) : entry.seriesId ? (
                      <PrimaryButton
                        label={`Desmarcar episódio assistido em ${watchDateFormatter.format(new Date(entry.watchedAt))}`}
                        variant="secondary"
                        disabled={isSaving}
                        onPress={() => {
                          if (!entry.seriesId) {
                            return;
                          }
                          setSaveConfirmation(null);
                          removeEpisode.mutate({
                            seriesId: entry.seriesId,
                            episodeId: entry.targetId,
                          }, {
                            onSuccess: () => {
                              setSaveConfirmation('Episódio removido do seu histórico.');
                              setHistoryPage(1);
                              setSeriesPage(1);
                            },
                          });
                        }}
                      />
                    ) : null}
                  </View>
                ))}
              </>
            )}
            {history.isError ? <RetryState onRetry={() => void history.refetch()} /> : null}
            <Pagination
              label="histórico"
              page={historyPage}
              totalCount={history.data.totalCount}
              isFetching={history.isFetching}
              onChange={setHistoryPage}
            />
          </>
        ) : null}
      </View>
      <View style={styles.section}>
        <Text accessibilityRole="header" style={styles.sectionTitle}>Séries acompanhadas</Text>
        <Text style={styles.description}>
          O total de episódios disponíveis ainda é desconhecido. Não é possível calcular
          um percentual ou confirmar a conclusão da série.
        </Text>
        {series.isPending ? (
          <LoadingState label="Carregando suas séries..." />
        ) : series.isError && !series.data ? (
          <RetryState onRetry={() => void series.refetch()} />
        ) : series.data ? (
          <>
            {series.data.items.length === 0 ? (
              <Text style={styles.description}>
                {series.data.totalCount === 0
                  ? 'Suas séries aparecerão aqui quando você registrar um episódio assistido.'
                  : 'Não há séries nesta página.'}
              </Text>
            ) : (
              <>
                <Text style={styles.description}>
                  Os nomes das séries ainda não estão disponíveis neste acompanhamento.
                </Text>
                {series.data.items.map((item, index) => (
                  <View key={item.seriesId} style={styles.card}>
                    <Text style={styles.cardTitle}>
                      Série acompanhada · {(seriesPage - 1) * PAGE_SIZE + index + 1}
                    </Text>
                    <Text style={styles.watchedCount}>
                      {item.watchedEpisodeCount}{' '}
                      {item.watchedEpisodeCount === 1 ? 'episódio assistido' : 'episódios assistidos'}
                    </Text>
                    <Text style={styles.description}>Total disponível: desconhecido</Text>
                  </View>
                ))}
              </>
            )}
            {series.isError ? <RetryState onRetry={() => void series.refetch()} /> : null}
            <Pagination
              label="séries acompanhadas"
              page={seriesPage}
              totalCount={series.data.totalCount}
              isFetching={series.isFetching}
              onChange={setSeriesPage}
            />
          </>
        ) : null}
      </View>
    </AuthScreen>
  );
}

function LoadingState({ label }: { label: string }) {
  return (
    <View accessibilityLiveRegion="polite" style={styles.loading}>
      <ActivityIndicator accessibilityLabel={label} color={colors.brand} />
      <Text style={styles.description}>{label}</Text>
    </View>
  );
}

function RetryState({ onRetry }: { onRetry: () => void }) {
  return <PrimaryButton label="Tentar novamente" onPress={onRetry} variant="secondary" />;
}

function toLocalDateTimeInput(value: string) {
  const date = new Date(value);
  const pad = (part: number) => String(part).padStart(2, '0');
  const year = String(date.getFullYear()).padStart(4, '0');
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${year} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function parseLocalDateTimeInput(value: string): string | null {
  const match = /^(\d{2})\/(\d{2})\/(\d{4}) (\d{2}):(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }

  const [day, month, year, hour, minute] = match.slice(1).map(Number);
  if (year < 1 || month < 1 || month > 12 || day < 1 || day > 31 || hour > 23 || minute > 59) {
    return null;
  }

  const date = new Date(0);
  date.setFullYear(year, month - 1, day);
  date.setHours(hour, minute, 0, 0);
  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month - 1 ||
    date.getDate() !== day ||
    date.getHours() !== hour ||
    date.getMinutes() !== minute
  ) {
    return null;
  }
  return date.toISOString();
}

function Pagination({
  label,
  page,
  totalCount,
  isFetching,
  onChange,
}: {
  label: string;
  page: number;
  totalCount: number;
  isFetching: boolean;
  onChange: (page: number) => void;
}) {
  if (totalCount === 0 && page === 1) {
    return null;
  }

  return (
    <View style={styles.pagination}>
      <Text accessibilityLiveRegion="polite" style={styles.description}>
        Página {page} · {totalCount} {label === 'histórico' ? 'registros' : 'séries'}
      </Text>
      <PrimaryButton
        label={`Página anterior de ${label}`}
        variant="secondary"
        disabled={page <= 1 || isFetching}
        onPress={() => onChange(page - 1)}
      />
      <PrimaryButton
        label={`Próxima página de ${label}`}
        variant="secondary"
        disabled={page * PAGE_SIZE >= totalCount || isFetching}
        onPress={() => onChange(page + 1)}
      />
      {isFetching ? <LoadingState label="Atualizando..." /> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    gap: spacing.medium,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontFamily: fontFamily.bold,
    fontSize: typography.section,
  },
  description: {
    color: colors.textSecondary,
    fontFamily: fontFamily.regular,
    fontSize: typography.body,
  },
  searchInput: {
    minHeight: spacing.extraLarge + spacing.medium,
    padding: spacing.medium,
    borderWidth: spacing.extraSmall / spacing.extraSmall,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surfaceElevated,
    color: colors.textPrimary,
    fontFamily: fontFamily.regular,
    fontSize: typography.body,
  },
  searchInputFocused: {
    borderColor: colors.brandSecondary,
  },
  validation: {
    color: colors.error,
    fontFamily: fontFamily.regular,
    fontSize: typography.body,
  },
  card: {
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surfaceElevated,
  },
  cardTitle: {
    color: colors.textPrimary,
    fontFamily: fontFamily.semiBold,
    fontSize: typography.body,
  },
  watchedCount: {
    color: colors.success,
    fontFamily: fontFamily.medium,
    fontSize: typography.body,
  },
  loading: {
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.small,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  pagination: {
    gap: spacing.small,
  },
});
