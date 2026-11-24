import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { TitlePosterCard } from '@/features/catalog/components/title-poster-card';
import type { FriendProfileData } from '@/features/social/api/social-api';
import { useFavoriteTitle } from '@/features/social/hooks/use-favorite-title';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** A favorite a friend shares: poster card, or a retry/open placeholder while the details are missing. */
export function FavoritePoster({ favorite }: { favorite: FriendProfileData['favorites'][number] }) {
  const router = useRouter();
  const title = useFavoriteTitle(favorite);
  return <View style={styles.favorite}>
    {title.data ? <TitlePosterCard title={title.data}><Text style={styles.caption}>{favorite.contentType === 'movie' ? 'Filme' : 'Série'}</Text></TitlePosterCard> : <View style={styles.placeholder}>
      {title.isPending ? <ActivityIndicator accessibilityLabel="Carregando favorito" color={colors.brand} /> : <>
        <PrimaryButton label="Tentar novamente" variant="quiet" onPress={() => void title.refetch()} />
        <Pressable accessibilityRole="button" accessibilityLabel="Abrir detalhes do favorito" onPress={() => router.push({ pathname: '/title/[provider]/[contentType]/[externalId]', params: { provider: favorite.provider, contentType: favorite.contentType, externalId: favorite.externalId } })} style={styles.openLink}><Text style={styles.body}>Ver título</Text></Pressable>
      </>}
    </View>}
  </View>;
}

const styles = StyleSheet.create({
  favorite: { width: dimensions.posterWidth, minHeight: dimensions.posterHeight + dimensions.minimumTouchTarget },
  placeholder: { width: dimensions.posterWidth, height: dimensions.posterHeight, borderRadius: radii.small, backgroundColor: colors.surface, justifyContent: 'center' },
  openLink: { flex: 1, minHeight: dimensions.minimumTouchTarget, alignItems: 'center', justifyContent: 'center', gap: spacing.small, paddingVertical: spacing.small, borderBottomWidth: 2, borderBottomColor: colors.background },
  caption: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  body: {
    color: colors.textSecondary,
    fontSize: typography.body,
    lineHeight: spacing.large,
    fontFamily: fontFamily.regular,
  },
});
