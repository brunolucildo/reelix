import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import { useTrackedSeries } from '@/hooks/use-tracking';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  onOpenTracking: () => void;
};

export function TrackedSeriesSection({ onOpenTracking }: Props) {
  const series = useTrackedSeries(1, 1);

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.heading}>Séries em acompanhamento</Text>
      {series.isPending ? (
        <View accessibilityLiveRegion="polite" style={styles.status}>
          <ActivityIndicator color={colors.brand} />
          <Text style={styles.statusText}>Carregando seu acompanhamento...</Text>
        </View>
      ) : series.isError && !series.data ? (
        <View style={styles.status}>
          <Text style={styles.statusText}>Não foi possível carregar esta seção.</Text>
          <Pressable
            accessibilityRole="button"
            onPress={() => void series.refetch()}
            style={styles.action}
          >
            <Text style={styles.actionText}>Tentar novamente</Text>
          </Pressable>
        </View>
      ) : series.data ? (
        <View style={styles.card}>
          <Text style={styles.statusText}>
            {series.data.totalCount === 0
              ? 'Registre episódios assistidos para acompanhar suas séries aqui.'
              : `Você acompanha ${series.data.totalCount} ${
                series.data.totalCount === 1 ? 'série' : 'séries'
              }.`}
          </Text>
          <Pressable
            accessibilityRole="button"
            onPress={onOpenTracking}
            style={styles.action}
          >
            <Text style={styles.actionText}>
              {series.data.totalCount === 0 ? 'Começar acompanhamento' : 'Ver acompanhamento'}
            </Text>
          </Pressable>
          {series.isError ? (
            <Pressable
              accessibilityRole="button"
              onPress={() => void series.refetch()}
              style={styles.action}
            >
              <Text style={styles.actionText}>Atualizar resumo</Text>
            </Pressable>
          ) : null}
        </View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    gap: spacing.medium,
  },
  heading: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  status: {
    minHeight: dimensions.posterHeight,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  card: {
    alignItems: 'flex-start',
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  statusText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  action: {
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.small,
  },
  actionText: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
});
