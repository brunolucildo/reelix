import { StyleSheet, Text, View } from 'react-native';

import { LoadingState, RetryState } from '@/components/ui/loading-state';
import { Pager } from '@/components/ui/pager';
import { MediaTitleRow } from '@/features/catalog/components/media-title-row';
import { trackingStyles as styles } from '@/features/tracking/components/tracking-styles';
import { useTrackedSeries } from '@/features/tracking/hooks/use-tracking';
import { useTrackedSeriesTitles } from '@/features/tracking/hooks/use-tracking-titles';
import { spacing } from '@/theme/tokens';

const PAGE_SIZE = 10;

type Props = {
  page: number;
  onPageChange: (page: number) => void;
};

/** Series with at least one watched episode, with progress counts and pagination. */
export function TrackedSeriesListSection({ page, onPageChange }: Props) {
  const series = useTrackedSeries(page, PAGE_SIZE);
  const titles = useTrackedSeriesTitles(series.data?.items ?? []);

  return (
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
              {series.data.items.map((item, index) => {
                const title = titles[index]?.data;
                const watched = `${item.watchedEpisodeCount} ${
                  item.watchedEpisodeCount === 1 ? 'episódio assistido' : 'episódios assistidos'
                }`;
                return title ? (
                  <MediaTitleRow key={item.seriesId} title={title}>
                    <View style={localStyles.meta}>
                      <Text style={styles.watchedCount}>{watched}</Text>
                      <Text style={styles.description}>Total disponível: desconhecido</Text>
                    </View>
                  </MediaTitleRow>
                ) : (
                  <View key={item.seriesId} style={styles.card}>
                    {titles[index]?.isPending ? (
                      <LoadingState label="Carregando dados da série..." />
                    ) : (
                      <Text style={styles.cardTitle}>
                        Série acompanhada · {(page - 1) * PAGE_SIZE + index + 1}
                      </Text>
                    )}
                    <Text style={styles.watchedCount}>{watched}</Text>
                  </View>
                );
              })}
            </>
          )}
          {series.isError ? <RetryState onRetry={() => void series.refetch()} /> : null}
          <Pager
            variant="count"
            label="séries acompanhadas"
            unit="séries"
            page={page}
            pageSize={PAGE_SIZE}
            totalCount={series.data.totalCount}
            isFetching={series.isFetching}
            onChange={onPageChange}
          />
        </>
      ) : null}
    </View>
  );
}

const localStyles = StyleSheet.create({
  meta: { gap: spacing.extraSmall },
});
