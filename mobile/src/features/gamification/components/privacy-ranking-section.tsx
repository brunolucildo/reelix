import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { ToggleRow } from '@/components/ui/toggle-row';
import type { GamificationDashboard } from '@/features/gamification/api/gamification-api';
import { gamificationStyles as shared } from '@/features/gamification/components/gamification-styles';
import type { useGamificationScreen } from '@/features/gamification/hooks/use-gamification-screen';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

type Screen = ReturnType<typeof useGamificationScreen>;

type Props = {
  preferences: GamificationDashboard['preferences'];
  mutation: Screen['preferencesMutation'];
  ranking: Screen['ranking'];
};

/** Badge visibility and ranking opt-in toggles, plus the monthly friends ranking when enabled. */
export function PrivacyRankingSection({ preferences, mutation, ranking }: Props) {
  return (
    <View style={shared.section}>
      <Text style={shared.sectionTitle}>Privacidade e ranking</Text>
      <ToggleRow
        label="Exibir badges no perfil"
        description="Compartilha somente conquistas obtidas, nunca seu histórico."
        value={preferences.badgesPublic}
        disabled={mutation.isPending}
        onValueChange={(badgesPublic) => mutation.mutate({
          badgesPublic,
          rankingEnabled: preferences.rankingEnabled,
        })}
      />
      <ToggleRow
        label="Participar do ranking entre amigos"
        description="Ranking mensal de filmes distintos assistidos. A participação é opcional."
        value={preferences.rankingEnabled}
        disabled={mutation.isPending}
        onValueChange={(rankingEnabled) => mutation.mutate({
          badgesPublic: preferences.badgesPublic,
          rankingEnabled,
        })}
      />
      {preferences.rankingEnabled ? (
        <View style={styles.ranking}>
          <Text style={styles.rankingTitle}>Filmes assistidos no mês</Text>
          {ranking.isPending ? (
            <ActivityIndicator color={colors.brand} />
          ) : ranking.isError ? (
            <PrimaryButton
              label="Tentar novamente"
              onPress={() => void ranking.refetch()}
              variant="secondary"
            />
          ) : (
            <>
              {(ranking.data.pages[0]?.items ?? []).length === 0 ? (
                <Text style={shared.body}>Ainda não há filmes no ranking deste mês.</Text>
              ) : null}
              {ranking.data.pages.flatMap((page) => page.items).map((entry) => (
                <View key={entry.accountId} style={styles.rankingRow}>
                  <Text style={styles.rankingName}>
                    {entry.position}. {entry.displayName}
                  </Text>
                  <Text style={styles.rankingCount}>{entry.moviesWatched}</Text>
                </View>
              ))}
              {ranking.hasNextPage ? (
                <PrimaryButton
                  label="Carregar mais"
                  loading={ranking.isFetchingNextPage}
                  onPress={() => void ranking.fetchNextPage()}
                  variant="secondary"
                />
              ) : null}
            </>
          )}
        </View>
      ) : (
        <Text style={shared.body}>
          Ative a participação para aparecer no ranking mensal apenas entre amigos que também optaram por participar.
        </Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  ranking: {
    gap: spacing.small,
    paddingTop: spacing.medium,
  },
  rankingTitle: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  rankingRow: {
    minHeight: 44,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  rankingName: {
    flex: 1,
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.medium,
  },
  rankingCount: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
});
