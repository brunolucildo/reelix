import { useState } from 'react';
import { Text, View } from 'react-native';

import { AuthScreen } from '@/components/auth/auth-screen';
import { BackButton } from '@/components/navigation/back-button';
import { LoadingState } from '@/components/ui/loading-state';
import { EpisodeRegistrationSection } from '@/features/tracking/components/episode-registration-section';
import { MovieRegistrationSection } from '@/features/tracking/components/movie-registration-section';
import { TrackedSeriesListSection } from '@/features/tracking/components/tracked-series-list-section';
import { trackingStyles as styles } from '@/features/tracking/components/tracking-styles';
import { WatchHistorySection } from '@/features/tracking/components/watch-history-section';
import { useTrackingActions } from '@/features/tracking/hooks/use-tracking-actions';

export default function TrackingScreen() {
  const actions = useTrackingActions();
  const [historyPage, setHistoryPage] = useState(1);
  const [seriesPage, setSeriesPage] = useState(1);
  const [confirmation, setConfirmation] = useState<string | null>(null);

  const clearConfirmation = () => setConfirmation(null);
  const movieSaved = (message: string) => {
    setConfirmation(message);
    setHistoryPage(1);
  };
  const episodeSaved = (message: string) => {
    movieSaved(message);
    setSeriesPage(1);
  };

  return (
    <AuthScreen
      title="Seu acompanhamento"
      subtitle="Veja seu histórico de visualização e os episódios que você já assistiu."
    >
      <BackButton />
      <MovieRegistrationSection actions={actions} onStart={clearConfirmation} onSaved={movieSaved} />
      <EpisodeRegistrationSection actions={actions} onStart={clearConfirmation} onSaved={episodeSaved} />
      <View style={styles.section}>
        {confirmation ? (
          <Text accessibilityLiveRegion="polite" style={styles.watchedCount}>{confirmation}</Text>
        ) : null}
        {actions.isSaving ? <LoadingState label="Salvando seu acompanhamento..." /> : null}
      </View>
      <WatchHistorySection
        actions={actions}
        page={historyPage}
        onPageChange={setHistoryPage}
        onStart={clearConfirmation}
        onSaved={setConfirmation}
        onRemoved={(message, isMovie) => {
          setConfirmation(message);
          setHistoryPage(1);
          if (!isMovie) setSeriesPage(1);
        }}
      />
      <TrackedSeriesListSection page={seriesPage} onPageChange={setSeriesPage} />
    </AuthScreen>
  );
}
