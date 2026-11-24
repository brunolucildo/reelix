import { useState } from 'react';
import { Text, View } from 'react-native';

import { LoadingState, RetryState } from '@/components/ui/loading-state';
import { PrimaryButton } from '@/components/ui/primary-button';
import { SearchField } from '@/features/tracking/components/search-field';
import { trackingStyles as styles } from '@/features/tracking/components/tracking-styles';
import type { TrackingActions } from '@/features/tracking/hooks/use-tracking-actions';
import { useTrackingMovieSearch } from '@/features/tracking/hooks/use-tracking-search';

type Props = {
  actions: TrackingActions;
  /** Called before a search or save starts, so the screen can clear its confirmation. */
  onStart: () => void;
  onSaved: (message: string) => void;
};

/** Search a movie in the catalog and register it as watched now. */
export function MovieRegistrationSection({ actions, onStart, onSaved }: Props) {
  const [text, setText] = useState('');
  const [submitted, setSubmitted] = useState('');
  const [validation, setValidation] = useState<string | null>(null);
  const results = useTrackingMovieSearch(submitted);

  const submit = () => {
    const query = text.trim();
    if (query.length === 0 || query.length > 200) {
      setValidation('Digite o nome de um filme, com até 200 caracteres.');
      return;
    }

    setValidation(null);
    onStart();
    if (query === submitted) {
      void results.refetch();
    } else {
      setSubmitted(query);
    }
  };

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.sectionTitle}>Registrar um filme</Text>
      <Text style={styles.description}>
        Busque no catálogo e marque como assistido. A data será registrada automaticamente.
        Para desmarcar, use seu histórico.
      </Text>
      <SearchField
        accessibilityLabel="Nome do filme para buscar no catálogo"
        value={text}
        onChangeText={(value) => {
          setText(value);
          setValidation(null);
        }}
        onSubmitEditing={submit}
        placeholder="Nome do filme"
        returnKeyType="search"
      />
      {validation ? (
        <Text accessibilityRole="alert" style={styles.validation}>{validation}</Text>
      ) : null}
      <PrimaryButton label="Buscar filme" onPress={submit} loading={results.isFetching} />
      {submitted.length > 0 ? (
        results.isPending ? (
          <LoadingState label="Buscando filmes..." />
        ) : results.isError && !results.data ? (
          <RetryState onRetry={() => void results.refetch()} />
        ) : results.data ? (
          <>
            {results.data.length === 0 ? (
              <Text style={styles.description}>Nenhum filme encontrado para esta busca.</Text>
            ) : (
              results.data.map((movie) => (
                <View key={movie.id} style={styles.card}>
                  <Text style={styles.cardTitle}>{movie.title}</Text>
                  <PrimaryButton
                    label={`Marcar ${movie.title} como assistido`}
                    disabled={actions.isSaving}
                    onPress={() => {
                      onStart();
                      actions.markMovie.mutate({ titleId: movie.id }, {
                        onSuccess: () => onSaved(`${movie.title} registrado como assistido.`),
                      });
                    }}
                  />
                </View>
              ))
            )}
            {results.isError ? <RetryState onRetry={() => void results.refetch()} /> : null}
          </>
        ) : null
      ) : null}
    </View>
  );
}
