import { useLocalSearchParams } from 'expo-router';
import { type ComponentRef, useRef, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet, Text, View } from 'react-native';

import { BackButton } from '@/components/navigation/back-button';
import { IconAction } from '@/components/ui/icon-action';
import { PrimaryButton } from '@/components/ui/primary-button';
import { TitleCollectionActions } from '@/features/lists/components/title-collection-actions';
import EyeIcon from '@/assets/brand/navigation-icons/eye.svg';
import { isCatalogProvider } from '@/features/catalog/api/catalog-api';
import { TitleHeader } from '@/features/catalog/components/title-header';
import { useCatalogTitle } from '@/features/catalog/hooks/use-catalog-title';
import { MovieReviews } from '@/features/ratings/components/movie-reviews';
import { RatingReviewSection } from '@/features/ratings/components/rating-review-section';
import { MovieWatchedAction } from '@/features/tracking/components/movie-watched-action';
import { SeriesSeasonsSection } from '@/features/tracking/components/series-seasons-section';
import { useViewerId } from '@/providers/auth-provider';
import { colors, dimensions, fontFamily, spacing, typography } from '@/theme/tokens';

export default function CatalogTitleScreen() {
  const scroll = useRef<ComponentRef<typeof ScrollView>>(null);
  const [seasonsOffset, setSeasonsOffset] = useState(0);
  const viewerId = useViewerId();
  const params = useLocalSearchParams<{
    provider?: string;
    contentType?: string;
    externalId?: string;
  }>();
  const provider = isCatalogProvider(params.provider) ? params.provider : null;
  const contentType = isCatalogContentType(params.contentType) ? params.contentType : null;
  const externalId = typeof params.externalId === 'string' ? params.externalId : null;
  const details = useCatalogTitle(provider, contentType, externalId);

  if (!provider || !contentType || !externalId) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="alert" style={styles.title}>
          O endereço deste título é inválido.
        </Text>
        <BackButton />
      </View>
    );
  }

  if (details.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.secondaryText}>Carregando detalhes...</Text>
      </View>
    );
  }

  if (details.isError || !details.data) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>
          Não foi possível abrir este título.
        </Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void details.refetch()} />
        <BackButton />
      </View>
    );
  }

  const title = details.data;
  const isMovie = title.contentType === 'movie';

  return (
    <ScrollView ref={scroll} contentContainerStyle={styles.content}>
      <TitleHeader
        title={title}
        actions={
          <>
            <TitleCollectionActions key={title.id} title={title} />
            {isMovie ? (
              <MovieWatchedAction titleId={title.id} title={title.title} />
            ) : (
              <IconAction
                label="Marcar episódios como assistidos"
                onPress={() => scroll.current?.scrollTo({ y: seasonsOffset, animated: true })}
              >
                <EyeIcon width={dimensions.iconSize} height={dimensions.iconSize} />
              </IconAction>
            )}
          </>
        }
      />
      {isMovie ? <MovieReviews titleId={title.id} /> : null}
      {!isMovie ? (
        <SeriesSeasonsSection
          title={title}
          onLayout={(event) => setSeasonsOffset(event.nativeEvent.layout.y)}
        />
      ) : null}
      {!isMovie ? (
        <RatingReviewSection
          key={`${title.contentType}:${title.id}:${viewerId}`}
          targetId={title.id}
          targetType={title.contentType}
        />
      ) : null}
    </ScrollView>
  );
}

function isCatalogContentType(value: unknown): value is 'movie' | 'series' {
  return value === 'movie' || value === 'series';
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    gap: spacing.medium,
    padding: spacing.medium,
    paddingTop: spacing.large,
    backgroundColor: colors.background,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.medium,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.bold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
});
