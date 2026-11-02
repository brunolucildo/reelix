import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { PrimaryButton } from '@/components/auth/primary-button';
import { TitlePosterCard } from '@/components/discovery/title-poster-card';
import { BackButton } from '@/components/navigation/back-button';
import { formatRatingStars } from '@/components/reviews/rating-stars';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchCatalogTitleDetails, fetchOwnRatings, type OwnRatingEntry } from '@/services/api-client';
import { colors, dimensions, fontFamily, spacing, typography } from '@/theme/tokens';

export default function MyRatingsScreen() {
  const [page, setPage] = useState(1);
  const ratings = useQuery({ queryKey: ['reviews', 'mine', page], queryFn: ({ signal }) => fetchOwnRatings(page, signal) });
  useErrorToast(ratings.error, ratings.error && 'status' in ratings.error && ratings.error.status === 401
    ? 'Sua sessão expirou. Entre novamente para continuar.' : 'Não foi possível carregar suas avaliações. Tente novamente.');
  return (
    <SafeAreaView style={styles.screen}>
      <ScrollView contentContainerStyle={styles.content}>
        <BackButton />
        <Text accessibilityRole="header" style={styles.title}>Minhas avaliações</Text>
        {ratings.isPending ? <ActivityIndicator color={colors.brand} /> : null}
        {ratings.isError ? <PrimaryButton label="Tentar novamente" onPress={() => void ratings.refetch()} /> : null}
        {ratings.data?.totalCount === 0 ? <Text style={styles.body}>Você ainda não avaliou títulos.</Text> : null}
        <View style={styles.cards}>
          {ratings.data?.items.map((rating) => <RatingCard key={rating.id} rating={rating} />)}
        </View>
        {ratings.data && ratings.data.totalCount > ratings.data.pageSize ? (
          <View style={styles.pagination}>
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Página anterior das avaliações"
              accessibilityState={{ disabled: page === 1 || ratings.isFetching }}
              disabled={page === 1 || ratings.isFetching}
              onPress={() => setPage(page - 1)}
              style={[styles.pageButton, (page === 1 || ratings.isFetching) && styles.disabled]}
            >
              <Text style={styles.pageArrow}>←</Text>
            </Pressable>
            <Text style={styles.body}>{page}</Text>
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Próxima página das avaliações"
              accessibilityState={{
                disabled: page * ratings.data.pageSize >= ratings.data.totalCount || ratings.isFetching,
              }}
              disabled={page * ratings.data.pageSize >= ratings.data.totalCount || ratings.isFetching}
              onPress={() => setPage(page + 1)}
              style={[
                styles.pageButton,
                (page * ratings.data.pageSize >= ratings.data.totalCount || ratings.isFetching) && styles.disabled,
              ]}
            >
              <Text style={styles.pageArrow}>→</Text>
            </Pressable>
          </View>
        ) : null}
      </ScrollView>
    </SafeAreaView>
  );
}

function RatingCard({ rating }: { rating: OwnRatingEntry }) {
  const type = rating.targetType === 'episode' ? 'series' : rating.targetType;
  const externalId = rating.targetType === 'episode' ? rating.seriesExternalId : rating.externalId;
  const title = useQuery({
    queryKey: ['catalog', 'title', rating.provider, type, externalId],
    queryFn: ({ signal }) => fetchCatalogTitleDetails(rating.provider, type, externalId!, signal),
    enabled: Boolean(externalId),
    staleTime: 300_000,
  });
  useErrorToast(title.error, 'Não foi possível carregar os detalhes de um título avaliado.');
  const details = (
    <>
      <Text accessibilityLabel={`Avaliação de ${rating.score} de 5 estrelas`} style={styles.score}>
        {formatRatingStars(rating.score)}
      </Text>
      <Text style={styles.body}>{rating.audience === 'private' ? 'Privada' : 'Pública'}{rating.targetType === 'episode' ? ' · Episódio' : ''}</Text>
      <Text style={styles.body}>{new Date(rating.updatedAt).toLocaleDateString('pt-BR')}</Text>
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
  screen: { flex: 1, backgroundColor: colors.background },
  content: { padding: spacing.medium, gap: spacing.large },
  title: { fontFamily: fontFamily.bold, fontSize: typography.title, color: colors.textPrimary },
  body: { fontFamily: fontFamily.regular, fontSize: typography.caption, color: colors.textSecondary },
  score: { fontFamily: fontFamily.bold, fontSize: typography.body, color: colors.brandSecondary },
  cards: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.medium },
  card: { width: dimensions.posterWidth, gap: spacing.small },
  pagination: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', gap: spacing.medium },
  pageButton: {
    width: 44,
    height: 44,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 8,
    backgroundColor: colors.surfaceElevated,
  },
  pageArrow: { color: colors.textPrimary, fontFamily: fontFamily.bold, fontSize: typography.section },
  disabled: { opacity: 0.45 },
});