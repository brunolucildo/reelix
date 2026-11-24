import { useState } from 'react';
import { Text, View } from 'react-native';

import { LoadingState, RetryState } from '@/components/ui/loading-state';
import { PrimaryButton } from '@/components/ui/primary-button';
import { SearchField } from '@/features/tracking/components/search-field';
import { trackingStyles as styles } from '@/features/tracking/components/tracking-styles';
import type { TrackingActions } from '@/features/tracking/hooks/use-tracking-actions';
import { usePendingEpisodes } from '@/features/tracking/hooks/use-tracking';
import { useTrackingSeriesSearch } from '@/features/tracking/hooks/use-tracking-search';

type Props = {
  actions: TrackingActions;
  onStart: () => void;
  onSaved: (message: string) => void;
};

/** Search a series, pick a season and register its pending episodes as watched. */
export function EpisodeRegistrationSection({ actions, onStart, onSaved }: Props) {
  const [search, setSearch] = useState('');
  const [submittedSearch, setSubmittedSearch] = useState('');
  const [selectedSeries, setSelectedSeries] = useState<{ id: string; title: string } | null>(null);
  const [seasonInput, setSeasonInput] = useState('');
  const [seasonNumber, setSeasonNumber] = useState(-1);
  const [validation, setValidation] = useState<string | null>(null);
  const seriesResults = useTrackingSeriesSearch(submittedSearch);
  const pendingEpisodes = usePendingEpisodes(selectedSeries?.id ?? '', seasonNumber);

  const submitSeriesSearch = () => {
    const query = search.trim();
    if (query.length === 0 || query.length > 200) {
      setValidation('Digite o nome de uma série, com até 200 caracteres.');
      return;
    }
    setValidation(null);
    setSelectedSeries(null);
    setSeasonNumber(-1);
    onStart();
    if (query === submittedSearch) {
      void seriesResults.refetch();
    } else {
      setSubmittedSearch(query);
    }
  };

  const loadPendingEpisodes = () => {
    const value = seasonInput.trim();
    const parsedSeason = Number(value);
    if (!/^\d+$/.test(value) || !Number.isSafeInteger(parsedSeason) || parsedSeason > 2147483647) {
      setValidation('Informe um número de temporada válido. Use 0 para especiais.');
      return;
    }
    setValidation(null);
    onStart();
    if (parsedSeason === seasonNumber) {
      void pendingEpisodes.refetch();
    } else {
      setSeasonNumber(parsedSeason);
    }
  };

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.sectionTitle}>Registrar episódios</Text>
      <Text style={styles.description}>
        Busque uma série e escolha a temporada. Use 0 para episódios especiais.
        Para desmarcar um episódio, use seu histórico.
      </Text>
      <SearchField
        accessibilityLabel="Nome da série para buscar no catálogo"
        value={search}
        onChangeText={(value) => {
          setSearch(value);
          setValidation(null);
        }}
        onSubmitEditing={submitSeriesSearch}
        placeholder="Nome da série"
        returnKeyType="search"
      />
      <PrimaryButton
        label="Buscar série"
        onPress={submitSeriesSearch}
        loading={seriesResults.isFetching}
        disabled={actions.isSaving}
      />
      {submittedSearch.length > 0 ? (
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
                    disabled={actions.isSaving}
                    onPress={() => {
                      setSelectedSeries({ id: item.id, title: item.title });
                      setSeasonInput('');
                      setSeasonNumber(-1);
                      setValidation(null);
                      onStart();
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
          <SearchField
            accessibilityLabel={`Número da temporada de ${selectedSeries.title}; zero para especiais`}
            value={seasonInput}
            onChangeText={(value) => {
              setSeasonInput(value);
              setValidation(null);
            }}
            onSubmitEditing={loadPendingEpisodes}
            placeholder="Número da temporada"
            keyboardType="number-pad"
          />
          <PrimaryButton
            label="Consultar episódios pendentes"
            onPress={loadPendingEpisodes}
            loading={pendingEpisodes.isFetching}
            disabled={actions.isSaving}
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
                        disabled={actions.isSaving}
                        onPress={() => {
                          onStart();
                          actions.markEpisode.mutate({
                            seriesId: selectedSeries.id,
                            episodeId: episode.episodeId,
                          }, {
                            onSuccess: () => onSaved(`${episode.title} registrado como assistido.`),
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
      {validation ? (
        <Text accessibilityRole="alert" style={styles.validation}>{validation}</Text>
      ) : null}
    </View>
  );
}
