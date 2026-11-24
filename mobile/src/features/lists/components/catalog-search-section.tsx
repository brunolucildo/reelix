import { useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { MediaTitleRow } from '@/features/catalog/components/media-title-row';
import { ListActionButton } from '@/features/lists/components/list-action-button';
import { ListInput } from '@/features/lists/components/list-input';
import { listsStyles as styles } from '@/features/lists/components/lists-styles';
import type { ListMutations } from '@/features/lists/hooks/use-list-mutations';
import { useListsCatalogSearch } from '@/features/lists/hooks/use-lists-search';
import AddListIcon from '@/assets/brand/navigation-icons/list-add.svg';
import ClockIcon from '@/assets/brand/navigation-icons/clock.svg';
import FavoritesIcon from '@/assets/brand/navigation-icons/favorites.svg';
import { colors, radii, spacing } from '@/theme/tokens';

type Props = {
  mutations: ListMutations;
  /** Selected private list; "Adicionar à lista" shows only while a list tab is open. */
  activeListId: string | null;
  showAddToList: boolean;
};

/** Catalog search with favorite, watchlist and add-to-list actions on each result. */
export function CatalogSearchSection({ mutations, activeListId, showAddToList }: Props) {
  const [text, setText] = useState('');
  const { search, statuses, statusByTitleId } = useListsCatalogSearch(text);
  const query = text.trim();

  return (
    <View style={localStyles.container}>
      <Text style={styles.sectionTitle}>Buscar no catálogo</Text>
      <ListInput
        accessibilityLabel="Buscar filmes e séries no catálogo"
        onChangeText={setText}
        placeholder="Nome de um filme ou série"
        value={text}
      />
      {query.length >= 2 && search.isPending ? <ActivityIndicator color={colors.brand} /> : null}
      {search.isError ? (
        <PrimaryButton label="Tentar novamente" onPress={() => void search.refetch()} variant="secondary" />
      ) : null}
      {statuses.isError ? (
        <PrimaryButton
          label="Tentar verificar títulos salvos"
          onPress={() => void statuses.refetch()}
          variant="secondary"
        />
      ) : null}
      {search.data?.length === 0 ? <Text style={styles.muted}>Nenhum título encontrado.</Text> : null}
      <ScrollView
        key={query}
        accessibilityLabel="Resultados do catálogo"
        horizontal
        directionalLockEnabled
        keyboardShouldPersistTaps="handled"
        contentContainerStyle={styles.horizontalCards}
      >
        {search.data?.map((title) => {
          const status = statusByTitleId.get(title.id);
          return (
            <MediaTitleRow key={title.id} title={title} layout="poster">
              <View style={localStyles.actions}>
                <ListActionButton
                  Icon={FavoritesIcon}
                  selected={status?.isFavorite}
                  label={status?.isFavorite ? 'Remover favorito' : 'Adicionar favorito'}
                  onPress={() => void (status?.isFavorite
                    ? mutations.removeFavorite(title.id)
                    : mutations.addFavorite(title))}
                />
                <ListActionButton
                  Icon={ClockIcon}
                  selected={status?.isInWatchlist}
                  label={status?.isInWatchlist ? 'Remover da watchlist' : 'Adicionar à watchlist'}
                  onPress={() => void (status?.isInWatchlist
                    ? mutations.removeFromWatchlist(title.id)
                    : mutations.addToWatchlist(title))}
                />
                {showAddToList && activeListId ? (
                  <ListActionButton
                    Icon={AddListIcon}
                    label="Adicionar à lista"
                    onPress={() => void mutations.addToList(activeListId, title)}
                  />
                ) : null}
              </View>
            </MediaTitleRow>
          );
        })}
      </ScrollView>
    </View>
  );
}

const localStyles = StyleSheet.create({
  container: {
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
  },
  actions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.extraSmall,
  },
});
