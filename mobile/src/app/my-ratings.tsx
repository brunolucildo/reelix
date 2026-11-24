import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { PrimaryButton } from '@/components/ui/primary-button';
import { BackButton } from '@/components/navigation/back-button';
import { OwnRatingCard } from '@/features/ratings/components/own-rating-card';
import { useOwnRatings } from '@/features/ratings/hooks/use-own-ratings';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

export default function MyRatingsScreen() {
  const [page, setPage] = useState(1);
  const ratings = useOwnRatings(page);
  return (
    <SafeAreaView style={styles.screen}>
      <ScrollView contentContainerStyle={styles.content}>
        <BackButton />
        <Text accessibilityRole="header" style={styles.title}>Minhas avaliações</Text>
        {ratings.isPending ? <ActivityIndicator color={colors.brand} /> : null}
        {ratings.isError ? <PrimaryButton label="Tentar novamente" onPress={() => void ratings.refetch()} /> : null}
        {ratings.data?.totalCount === 0 ? <Text style={styles.body}>Você ainda não avaliou títulos.</Text> : null}
        <View style={styles.cards}>
          {ratings.data?.items.map((rating) => <OwnRatingCard key={rating.id} rating={rating} />)}
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

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background },
  content: { padding: spacing.medium, gap: spacing.large },
  title: { fontFamily: fontFamily.bold, fontSize: typography.title, color: colors.textPrimary },
  body: { fontFamily: fontFamily.regular, fontSize: typography.caption, color: colors.textSecondary },
  cards: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.medium },
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