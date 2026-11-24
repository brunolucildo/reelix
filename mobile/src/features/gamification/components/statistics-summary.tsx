import { StyleSheet, Text, View } from 'react-native';

import type { GamificationStatistics } from '@/features/gamification/api/gamification-api';
import { gamificationStyles as shared } from '@/features/gamification/components/gamification-styles';
import { formatNumber } from '@/lib/format';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Counters, estimated watch time and runtime coverage of a statistics period. */
export function StatisticsSummary({ statistics }: { statistics: GamificationStatistics }) {
  return (
    <View style={styles.statistics}>
      <View style={styles.grid}>
        <StatTile label="Episódios" value={statistics.episodesWatched} icon="✓" />
        <StatTile label="Séries" value={statistics.seriesExplored} icon="▣" />
        <StatTile label="Filmes" value={statistics.moviesWatched} icon="▶" />
        <StatTile
          label="Registros"
          value={statistics.moviesWatched + statistics.episodesWatched}
          icon="◷"
        />
      </View>
      <Text style={styles.line}>{statistics.genresExplored} gêneros de filmes conhecidos</Text>
      <Text style={shared.body}>
        {statistics.estimatedWatchHours === null
          ? 'Tempo estimado indisponível: ainda não há durações conhecidas.'
          : `Tempo estimado: ${formatNumber(statistics.estimatedWatchHours, { maximumFractionDigits: 2 })} h`}
      </Text>
      <Text style={shared.body}>
        Cobertura de duração: {statistics.itemsWithRuntime}/{statistics.itemsWithRuntime + statistics.itemsWithoutRuntime} itens
        {' '}({Math.round(statistics.runtimeCoverage * 100)}%).
      </Text>
      <Text style={shared.caption}>
        Tempo confirmado não é medido. Fontes: {statistics.runtimeSources.length > 0
          ? statistics.runtimeSources.join(', ')
          : 'nenhuma duração disponível'}
        {statistics.runtimeMetadataVersion === null
          ? ''
          : ` · snapshot v${statistics.runtimeMetadataVersion}`}
      </Text>
      {statistics.moviesMissingGenreMetadata > 0 ? (
        <Text style={shared.caption}>
          Alguns filmes ainda não têm gêneros conhecidos; eles não contam para essa estatística.
        </Text>
      ) : null}
    </View>
  );
}

function StatTile({ label, value, icon }: { label: string; value: number; icon: string }) {
  return (
    <View style={styles.tile}>
      <Text style={styles.icon}>{icon}</Text>
      <Text style={styles.value}>{value}</Text>
      <Text style={styles.label}>{label}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  statistics: {
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.small,
  },
  tile: {
    flexGrow: 1,
    flexBasis: '45%',
    minHeight: 92,
    gap: spacing.extraSmall,
    justifyContent: 'center',
    padding: spacing.small,
    borderRadius: radii.small,
    backgroundColor: colors.surfaceElevated,
  },
  icon: { color: colors.brandSecondary, fontSize: typography.body, fontFamily: fontFamily.bold },
  value: { color: colors.textPrimary, fontSize: typography.section, fontFamily: fontFamily.bold },
  label: { color: colors.textSecondary, fontSize: typography.caption, fontFamily: fontFamily.medium },
  line: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
});
