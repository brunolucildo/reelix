import { ActivityIndicator, Text, View } from 'react-native';

import { Pager } from '@/components/ui/pager';
import { PrimaryButton } from '@/components/ui/primary-button';
import { SmallButton } from '@/components/ui/small-button';
import { listsStyles as styles } from '@/features/lists/components/lists-styles';
import { ReferenceCarousel } from '@/features/lists/components/reference-carousel';
import type { ListMutations } from '@/features/lists/hooks/use-list-mutations';
import type { ListsScreenState } from '@/features/lists/hooks/use-lists-screen';
import { colors } from '@/theme/tokens';

type Props = {
  screen: ListsScreenState;
  mutations: ListMutations;
};

/** Favorites or watchlist tab: counter, optional ordering, titles rail and pager. */
export function CollectionSection({ screen, mutations }: Props) {
  const { section, page, oldestFirst, collection, titleDetails } = screen;
  const isFavorites = section === 'favorites';
  const data = collection.data;

  return (
    <View style={styles.section}>
      <Text style={styles.sectionTitle}>
        {isFavorites ? 'Favoritos' : 'Watchlist'}
        {data ? ` · ${data.totalCount}` : ''}
      </Text>
      {!isFavorites ? (
        <SmallButton
          label={`Ordenar: ${oldestFirst ? 'mais antigas' : 'mais recentes'}`}
          onPress={screen.toggleOrder}
        />
      ) : null}
      {collection.isPending ? <ActivityIndicator color={colors.brand} /> : null}
      {collection.isError ? (
        <PrimaryButton
          label="Tentar novamente"
          onPress={() => void collection.refetch()}
          variant="secondary"
        />
      ) : null}
      {!collection.isPending && data?.items.length === 0 ? (
        <Text style={styles.muted}>Ainda não há títulos nesta seção.</Text>
      ) : null}
      <ReferenceCarousel
        references={data?.items ?? []}
        details={titleDetails}
        scrollKey={`${section}-${page}-${oldestFirst}`}
        accessibilityLabel={isFavorites ? 'Títulos favoritos' : 'Títulos da watchlist'}
        onRemove={(reference) => void (isFavorites
          ? mutations.removeFavorite(reference.titleId)
          : mutations.removeFromWatchlist(reference.titleId))}
      />
      {data ? (
        <Pager
          page={data.page}
          pageSize={data.pageSize}
          totalCount={data.totalCount}
          onChange={screen.setPage}
        />
      ) : null}
    </View>
  );
}
