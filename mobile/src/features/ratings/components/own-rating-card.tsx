import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { TitlePosterCard } from '@/features/catalog/components/title-poster-card';
import type { OwnRatingEntry } from '@/features/ratings/api/ratings-api';
import { useRatedTitle } from '@/features/ratings/hooks/use-own-ratings';
import { formatRatingStars } from '@/features/ratings/rating-stars';
import { formatDate } from '@/lib/format';
import { colors, dimensions, fontFamily, spacing, typography } from '@/theme/tokens';

/** A rating the viewer gave, shown on its title poster. */
export function OwnRatingCard({ rating }: { rating: OwnRatingEntry }) {
  const title = useRatedTitle(rating);
  const details = (
    <>
      <Text accessibilityLabel={`Avaliação de ${rating.score} de 5 estrelas`} style={styles.score}>
        {formatRatingStars(rating.score)}
      </Text>
      <Text style={styles.body}>{rating.audience === 'private' ? 'Privada' : 'Pública'}{rating.targetType === 'episode' ? ' · Episódio' : ''}</Text>
      <Text style={styles.body}>{formatDate(rating.updatedAt)}</Text>
    </>
  );
  return (
    <View style={styles.card}>
      {title.data ? <TitlePosterCard title={title.data}>{details}</TitlePosterCard> : (
        <>
          {title.isFetching ? <ActivityIndicator color={colors.brand} /> : <Text style={styles.body}>Título ainda não disponível.</Text>}
          {details}
        </>
      )}
      {title.isError ? <PrimaryButton label="Atualizar título" variant="secondary" onPress={() => void title.refetch()} /> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  body: { fontFamily: fontFamily.regular, fontSize: typography.caption, color: colors.textSecondary },
  score: { fontFamily: fontFamily.bold, fontSize: typography.body, color: colors.brandSecondary },
  card: { width: dimensions.posterWidth, gap: spacing.small },
});
