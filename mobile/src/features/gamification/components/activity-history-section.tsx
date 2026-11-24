import { Image } from 'expo-image';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import type { CatalogTitle } from '@/features/catalog/api/catalog-api';
import { gamificationStyles as shared } from '@/features/gamification/components/gamification-styles';
import type { useGamificationScreen } from '@/features/gamification/hooks/use-gamification-screen';
import { formatDateTimeMedium } from '@/lib/format';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Screen = ReturnType<typeof useGamificationScreen>;

type Props = {
  history: Screen['activityHistory'];
  groups: Screen['activityGroups'];
  titleById: Map<string, CatalogTitle>;
};

/** "Atividade recente": the latest watched entries grouped by month, with posters when known. */
export function ActivityHistorySection({ history, groups, titleById }: Props) {
  return (
    <View style={shared.section}>
      <Text style={shared.sectionTitle}>Atividade recente</Text>
      {history.isPending ? (
        <ActivityIndicator color={colors.brand} />
      ) : history.isError && !history.data ? (
        <PrimaryButton
          label="Tentar carregar atividades"
          onPress={() => void history.refetch()}
          variant="secondary"
        />
      ) : groups.length === 0 ? (
        <Text style={shared.body}>Ainda não há atividades registradas.</Text>
      ) : (
        groups.map((group) => (
          <View key={group.key} style={styles.group}>
            <Text style={styles.month}>{group.label}</Text>
            {group.items.map((item) => (
              <View key={item.id} style={styles.row}>
                <Text style={styles.icon}>{item.type === 'movie' ? '▶' : '✓'}</Text>
                <View style={styles.copy}>
                  <Text style={styles.label}>
                    {titleById.get(item.id)?.title ??
                      (item.type === 'movie' ? 'Filme assistido' : 'Episódio assistido')}
                  </Text>
                  <Text style={shared.caption}>
                    {item.type === 'movie' ? 'Filme assistido' : 'Episódio assistido'}
                    {' · '}
                    {formatDateTimeMedium(new Date(item.watchedAt))}
                  </Text>
                </View>
                {titleById.get(item.id)?.posterUrl ? (
                  <Image
                    contentFit="cover"
                    source={{ uri: titleById.get(item.id)?.posterUrl ?? undefined }}
                    style={styles.poster}
                  />
                ) : null}
              </View>
            ))}
          </View>
        ))
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  group: {
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  month: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
    textTransform: 'capitalize',
  },
  row: {
    minHeight: 56,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.medium,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  icon: {
    width: 32,
    color: colors.brandSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  poster: {
    width: 42,
    height: 62,
    borderRadius: radii.small,
    backgroundColor: colors.surfaceElevated,
  },
  copy: { flex: 1, gap: spacing.extraSmall },
  label: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
});
