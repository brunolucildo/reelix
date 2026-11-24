import { useState } from 'react';
import { Text, View } from 'react-native';

import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { LoadingState, RetryState } from '@/components/ui/loading-state';
import { Pager } from '@/components/ui/pager';
import { PrimaryButton } from '@/components/ui/primary-button';
import type { TrackingHistoryEntry } from '@/features/tracking/api/tracking-api';
import { trackingStyles as styles } from '@/features/tracking/components/tracking-styles';
import { WatchHistoryEntry } from '@/features/tracking/components/watch-history-entry';
import { useTrackingHistory } from '@/features/tracking/hooks/use-tracking';
import type { TrackingActions } from '@/features/tracking/hooks/use-tracking-actions';
import { useHistoryTitles } from '@/features/tracking/hooks/use-tracking-titles';

const PREVIEW_SIZE = 5;
const PAGE_SIZE = 20;

type Props = {
  actions: TrackingActions;
  page: number;
  onPageChange: (page: number) => void;
  onStart: () => void;
  /** A date correction was saved. */
  onSaved: (message: string) => void;
  /** An entry was unmarked; the screen resets its lists to the first page. */
  onRemoved: (message: string, isMovie: boolean) => void;
};

/** Watch history: preview or full list, date correction and unmarking with confirmation. */
export function WatchHistorySection({ actions, page, onPageChange, onStart, onSaved, onRemoved }: Props) {
  const [showAll, setShowAll] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [entryToRemove, setEntryToRemove] = useState<TrackingHistoryEntry | null>(null);
  const pageSize = showAll ? PAGE_SIZE : PREVIEW_SIZE;
  const history = useTrackingHistory(page, pageSize);
  const titles = useHistoryTitles(history.data?.items ?? []);

  const submitWatchedAt = (entry: TrackingHistoryEntry) => (watchedAt: string): string | null => {
    const onSuccess = () => {
      onSaved('Data da visualização atualizada.');
      setEditingId(null);
    };
    if (entry.type === 'movie') {
      onStart();
      actions.markMovie.mutate({ titleId: entry.targetId, watchedAt }, { onSuccess });
      return null;
    }
    if (entry.seriesId) {
      onStart();
      actions.markEpisode.mutate({ seriesId: entry.seriesId, episodeId: entry.targetId, watchedAt }, { onSuccess });
      return null;
    }
    return 'Não foi possível identificar a série deste episódio.';
  };

  const removeEntry = (entry: TrackingHistoryEntry) => {
    const isMovie = entry.type === 'movie';
    onStart();
    const onSuccess = () => onRemoved(
      isMovie ? 'Filme removido do seu histórico.' : 'Episódio removido do seu histórico.',
      isMovie,
    );
    if (isMovie) {
      actions.removeMovie.mutate(entry.targetId, { onSuccess });
    } else if (entry.seriesId) {
      actions.removeEpisode.mutate({ seriesId: entry.seriesId, episodeId: entry.targetId }, { onSuccess });
    }
  };

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.sectionTitle}>Histórico</Text>
      <Text style={styles.description}>Do mais recente ao mais antigo, no seu horário local.</Text>
      {history.isPending ? (
        <LoadingState label="Carregando seu histórico..." />
      ) : history.isError && !history.data ? (
        <RetryState onRetry={() => void history.refetch()} />
      ) : history.data ? (
        <>
          {history.data.totalCount > PREVIEW_SIZE ? (
            <PrimaryButton
              label={showAll ? 'Mostrar apenas os cinco mais recentes' : 'Ver todos os registros'}
              variant="secondary"
              onPress={() => {
                setShowAll((current) => !current);
                onPageChange(1);
              }}
            />
          ) : null}
          {history.data.items.length === 0 ? (
            <Text style={styles.description}>
              {history.data.totalCount === 0
                ? 'Você ainda não registrou visualizações.'
                : 'Não há registros nesta página.'}
            </Text>
          ) : history.data.items.map((entry, index) => (
            <WatchHistoryEntry
              key={entry.id}
              entry={entry}
              title={titles[index]?.data}
              isEditing={editingId === entry.id}
              isSaving={actions.isSaving}
              onEdit={() => {
                setEditingId(entry.id);
                onStart();
              }}
              onCancelEdit={() => setEditingId(null)}
              onSubmitWatchedAt={submitWatchedAt(entry)}
              onRemove={() => setEntryToRemove(entry)}
            />
          ))}
          {history.isError ? <RetryState onRetry={() => void history.refetch()} /> : null}
          <Pager
            variant="count"
            label="histórico"
            unit="registros"
            page={page}
            pageSize={pageSize}
            totalCount={history.data.totalCount}
            isFetching={history.isFetching}
            onChange={onPageChange}
          />
        </>
      ) : null}
      <ConfirmDialog
        visible={entryToRemove !== null}
        title="Desmarcar como assistido?"
        message="O registro será removido do seu histórico e o progresso relacionado será atualizado."
        confirmLabel="Desmarcar"
        onCancel={() => setEntryToRemove(null)}
        onConfirm={() => {
          if (entryToRemove) removeEntry(entryToRemove);
          setEntryToRemove(null);
        }}
      />
    </View>
  );
}
