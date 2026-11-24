import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { Chip, SegmentButton } from '@/components/ui/chip';
import { PrimaryButton } from '@/components/ui/primary-button';
import { onboardingStyles as shared } from '@/features/onboarding/components/onboarding-styles';
import { MAX_GENRES } from '@/features/onboarding/schemas';
import type { OnboardingFlowState } from '@/features/onboarding/hooks/use-onboarding-flow';
import { colors, spacing } from '@/theme/tokens';

/** Step "genres": pick up to 20 genres for movies and for series. */
export function GenresStep({ flow }: { flow: OnboardingFlowState }) {
  const { genreType, genres, selectedGenres } = flow;

  return (
    <View style={shared.section}>
      <Text style={shared.eyebrow}>PREFERÊNCIAS</Text>
      <Text accessibilityRole="header" style={shared.title}>Que tipo de história te prende?</Text>
      <View style={styles.segmented}>
        <SegmentButton label="Filmes" selected={genreType === 'movie'} onPress={() => flow.setGenreType('movie')} />
        <SegmentButton label="Séries" selected={genreType === 'series'} onPress={() => flow.setGenreType('series')} />
      </View>
      <Text style={shared.selectedCount}>
        {selectedGenres.length} de {MAX_GENRES} gêneros selecionados
      </Text>
      {genres.isPending ? <ActivityIndicator color={colors.brand} /> : null}
      {genres.isError ? (
        <View style={styles.messageBlock}>
          <Text style={shared.emptyText}>Gêneros indisponíveis no momento.</Text>
          <PrimaryButton label="Tentar novamente" variant="secondary" onPress={() => void genres.refetch()} />
        </View>
      ) : (
        <View style={styles.grid}>
          {genres.data?.map((genre) => (
            <Chip
              key={genre.id}
              label={genre.name}
              selected={selectedGenres.includes(genre.id)}
              onPress={() => flow.toggleGenre(genre)}
            />
          ))}
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  segmented: {
    flexDirection: 'row',
    gap: spacing.small,
    marginTop: 6,
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.small,
  },
  messageBlock: {
    gap: spacing.small,
  },
});
