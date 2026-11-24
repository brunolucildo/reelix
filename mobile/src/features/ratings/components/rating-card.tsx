import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { AudienceSelector } from '@/features/ratings/components/audience-selector';
import { reviewsStyles as shared } from '@/features/ratings/components/reviews-styles';
import type { ReviewTarget } from '@/features/ratings/hooks/use-review-cache';
import { useRatingEditor } from '@/features/ratings/hooks/use-rating-editor';
import { formatRatingStars } from '@/features/ratings/rating-stars';
import { formatNumber } from '@/lib/format';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** "Sua avaliação": public average, star picker with half stars, audience and save/remove. */
export function RatingCard({ target }: { target: ReviewTarget }) {
  const { rating, selectedScore, audience, setScore, setAudience, save, remove } =
    useRatingEditor(target, true);
  const cannotAddHalf = selectedScore === null || selectedScore === 5;
  const hasHalf = selectedScore !== null && selectedScore % 1 !== 0;

  return (
    <View style={shared.card}>
      <Text style={shared.subheading}>Sua avaliação</Text>
      {rating.isPending ? (
        <ActivityIndicator color={colors.brand} />
      ) : rating.isError ? (
        <View style={shared.statusBlock}>
          <Text style={shared.secondaryText}>A avaliação não está disponível agora.</Text>
          <PrimaryButton
            label="Tentar novamente"
            variant="secondary"
            onPress={() => void rating.refetch()}
          />
        </View>
      ) : (
        <>
          {rating.data.averageScore === null ? (
            <Text style={shared.secondaryText}>Ainda sem avaliações públicas.</Text>
          ) : (
            <View style={styles.publicRating}>
              <Text
                accessibilityLabel={`Média pública ${rating.data.averageScore.toFixed(1)} de 5 estrelas`}
                style={styles.publicRatingStars}
              >
                {formatRatingStars(rating.data.averageScore)}
              </Text>
              <Text style={shared.secondaryText}>
                {rating.data.publicCount} {rating.data.publicCount === 1 ? 'avaliação' : 'avaliações'}
              </Text>
            </View>
          )}
          <View accessibilityRole="radiogroup" style={styles.controls}>
            <View style={styles.scoreOptions}>
              {Array.from({ length: 5 }, (_, index) => index + 1).map((score) => {
                const selected = selectedScore !== null && selectedScore >= score;
                const halfSelected = selectedScore === score - 0.5;
                return (
                  <Pressable
                    key={score}
                    accessibilityRole="radio"
                    accessibilityLabel={`Selecionar ${score} ${score === 1 ? 'estrela' : 'estrelas'}`}
                    accessibilityState={{ selected: selectedScore === score }}
                    onPress={() => setScore(score)}
                    style={styles.starButton}
                  >
                    <Text style={[
                      styles.starText,
                      selected || halfSelected ? styles.selectedStarText : null,
                    ]}>
                      {halfSelected ? '◐' : selected ? '★' : '☆'}
                    </Text>
                  </Pressable>
                );
              })}
            </View>
            <View style={styles.adjustments}>
              <Pressable
                accessibilityRole="radio"
                accessibilityLabel="Selecionar zero estrelas"
                accessibilityState={{ selected: selectedScore === 0 }}
                onPress={() => setScore(0)}
                style={[styles.adjustment, selectedScore === 0 ? styles.selectedAdjustment : null]}
              >
                <Text style={styles.adjustmentText}>0 estrelas</Text>
              </Pressable>
              <Pressable
                accessibilityRole="button"
                accessibilityLabel={selectedScore === 5
                  ? 'Selecione 4 estrelas para escolher meia estrela'
                  : hasHalf
                    ? 'Remover meia estrela'
                    : selectedScore === null
                      ? 'Escolha uma nota antes de adicionar meia estrela'
                      : 'Adicionar meia estrela'}
                accessibilityState={{ disabled: cannotAddHalf }}
                disabled={cannotAddHalf}
                onPress={() => {
                  if (selectedScore === null || selectedScore === 5) {
                    return;
                  }

                  setScore(selectedScore % 1 === 0 ? selectedScore + 0.5 : selectedScore - 0.5);
                }}
                style={[styles.adjustment, cannotAddHalf ? styles.disabledAdjustment : null]}
              >
                <Text style={styles.adjustmentText}>
                  {selectedScore === 5
                    ? 'Selecione 4 estrelas para usar meia estrela'
                    : hasHalf
                      ? 'Remover meia estrela'
                      : selectedScore === null
                        ? 'Escolha uma nota para adicionar meia estrela'
                        : 'Adicionar meia estrela'}
                </Text>
              </Pressable>
            </View>
            {selectedScore !== null ? (
              <Text style={shared.secondaryText}>Sua nota: {formatNumber(selectedScore)} de 5</Text>
            ) : null}
          </View>
          <AudienceSelector
            disabled={save.isPending || remove.isPending}
            audience={audience}
            onChange={setAudience}
          />
          <PrimaryButton
            label={rating.data.myRating ? 'Atualizar avaliação' : 'Salvar avaliação'}
            loading={save.isPending}
            disabled={selectedScore === null || rating.isError}
            onPress={() => save.mutate()}
          />
          {rating.data.myRating ? (
            <PrimaryButton
              label="Remover minha avaliação"
              variant="quiet"
              loading={remove.isPending}
              onPress={() => remove.mutate()}
            />
          ) : null}
        </>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  scoreOptions: {
    flexDirection: 'row',
    gap: spacing.small,
    alignItems: 'center',
    justifyContent: 'flex-start',
  },
  controls: {
    gap: spacing.small,
  },
  adjustments: {
    flexDirection: 'row',
    gap: spacing.small,
  },
  adjustment: {
    flex: 1,
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.small,
    backgroundColor: colors.surfaceElevated,
  },
  selectedAdjustment: {
    borderColor: colors.brandSecondary,
  },
  disabledAdjustment: {
    opacity: 0.5,
  },
  adjustmentText: {
    color: colors.textPrimary,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
    textAlign: 'center',
  },
  starButton: {
    minWidth: 48,
    minHeight: 48,
    alignItems: 'center',
    justifyContent: 'center',
  },
  starText: {
    color: colors.textSecondary,
    fontSize: typography.title,
    fontFamily: fontFamily.bold,
  },
  selectedStarText: {
    color: colors.brandSecondary,
  },
  publicRating: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
  },
  publicRatingStars: {
    color: colors.brandSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
});
