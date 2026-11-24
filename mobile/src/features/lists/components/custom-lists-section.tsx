import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View, useWindowDimensions } from 'react-native';

import { Pager } from '@/components/ui/pager';
import { PrimaryButton } from '@/components/ui/primary-button';
import { SmallButton } from '@/components/ui/small-button';
import { ListActionButton } from '@/features/lists/components/list-action-button';
import { listsStyles as shared } from '@/features/lists/components/lists-styles';
import { ReferenceCarousel } from '@/features/lists/components/reference-carousel';
import type { ListDialogController } from '@/features/lists/hooks/use-list-dialog';
import type { ListMutations } from '@/features/lists/hooks/use-list-mutations';
import type { ListsScreenState } from '@/features/lists/hooks/use-lists-screen';
import AddListIcon from '@/assets/brand/navigation-icons/list-add.svg';
import ArrowDownIcon from '@/assets/brand/navigation-icons/arrow-down.svg';
import ArrowUpIcon from '@/assets/brand/navigation-icons/arrow-up.svg';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  screen: ListsScreenState;
  mutations: ListMutations;
  dialog: ListDialogController;
};

/** "Minhas listas" tab: private lists rail; the selected list expands with its titles. */
export function CustomListsSection({ screen, mutations, dialog }: Props) {
  const { width } = useWindowDimensions();
  const cardWidth = Math.min(dimensions.drawerWidth, width - spacing.medium * 2);
  const { customLists, selectedList, activeListId, listPage, page, titleDetails } = screen;
  const lists = customLists.data?.items ?? [];
  const visibleLists = selectedList.data && !lists.some((list) => list.id === selectedList.data.list.id)
    ? [selectedList.data.list, ...lists]
    : lists;
  const selectedData = selectedList.data;

  const listContents = (
    <View style={styles.contents}>
      {selectedList.isPending ? <ActivityIndicator color={colors.brand} /> : null}
      {selectedList.isError ? (
        <PrimaryButton label="Tentar novamente" onPress={() => void selectedList.refetch()} variant="secondary" />
      ) : null}
      {selectedData?.titles.items.length === 0 ? (
        <Text style={shared.muted}>Esta lista ainda não tem títulos. Use a busca para adicionar.</Text>
      ) : null}
      <ReferenceCarousel
        references={selectedData?.titles.items ?? []}
        details={titleDetails}
        scrollKey={`${activeListId}-${page}`}
        accessibilityLabel="Conteúdos da lista privada"
        nested
        onRemove={(reference) => {
          if (activeListId) void mutations.removeFromList(activeListId, reference.titleId);
        }}
        renderExtra={(reference) => (
          <View style={styles.orderActions}>
            <ListActionButton
              Icon={ArrowUpIcon}
              disabled={reference.position === 0}
              label="Mover para cima"
              onPress={() => {
                if (activeListId) void mutations.moveInList(activeListId, reference.titleId, -1);
              }}
            />
            <ListActionButton
              Icon={ArrowDownIcon}
              disabled={reference.position === (selectedData?.list.totalCount ?? 0) - 1}
              label="Mover para baixo"
              onPress={() => {
                if (activeListId) void mutations.moveInList(activeListId, reference.titleId, 1);
              }}
            />
          </View>
        )}
      />
      {selectedData ? (
        <Pager
          page={selectedData.titles.page}
          pageSize={selectedData.titles.pageSize}
          totalCount={selectedData.titles.totalCount}
          onChange={screen.setPage}
        />
      ) : null}
    </View>
  );

  return (
    <View style={shared.section}>
      <View style={styles.header}>
        <Text style={shared.sectionTitle}>Listas privadas</Text>
        <Pressable
          accessibilityLabel="Adicionar nova lista"
          accessibilityRole="button"
          onPress={() => dialog.openForm(null)}
          style={styles.iconButton}
        >
          <AddListIcon width={dimensions.iconSize} height={dimensions.iconSize} />
        </Pressable>
      </View>
      {customLists.isPending ? <ActivityIndicator color={colors.brand} /> : null}
      {customLists.isError ? (
        <PrimaryButton label="Tentar novamente" onPress={() => void customLists.refetch()} variant="secondary" />
      ) : null}
      {visibleLists.length === 0 && customLists.data && listPage === 1 ? (
        <Text style={shared.muted}>Você ainda não criou listas.</Text>
      ) : null}
      <ScrollView
        key={listPage}
        accessibilityLabel="Listas privadas"
        horizontal
        nestedScrollEnabled
        directionalLockEnabled
        keyboardShouldPersistTaps="handled"
        contentContainerStyle={shared.horizontalCards}
      >
        {visibleLists.map((summary) => {
          const list = summary.id === activeListId && selectedData ? selectedData.list : summary;
          const isActive = list.id === activeListId;
          return (
            <View key={list.id} style={[styles.card, { width: cardWidth }]}>
              <View style={styles.header}>
                <Pressable
                  accessibilityLabel={`Abrir lista ${list.name}`}
                  accessibilityRole="button"
                  accessibilityState={{ selected: isActive, expanded: isActive }}
                  onPress={() => screen.selectList(list.id)}
                  style={[styles.select, isActive ? styles.selected : null]}
                >
                  <View style={shared.titleCopy}>
                    <Text style={styles.itemTitle}>{list.name}</Text>
                    <Text style={shared.muted}>{list.totalCount} filmes e séries · privada</Text>
                    {list.description ? <Text style={shared.muted}>{list.description}</Text> : null}
                  </View>
                </Pressable>
                <SmallButton
                  label="..."
                  accessibilityLabel={`Opções da lista ${list.name}`}
                  onPress={() => dialog.openMenu(list)}
                  iconOnly
                />
              </View>
              {isActive ? listContents : null}
            </View>
          );
        })}
      </ScrollView>
      {customLists.data ? (
        <Pager
          page={listPage}
          pageSize={customLists.data.pageSize}
          totalCount={customLists.data.totalCount}
          onChange={screen.changeListPage}
        />
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
  },
  contents: { gap: spacing.small },
  card: {
    gap: spacing.small,
    borderRadius: radii.medium,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.small,
    backgroundColor: colors.surface,
  },
  iconButton: {
    minWidth: dimensions.minimumTouchTarget,
    minHeight: dimensions.minimumTouchTarget,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.small,
    backgroundColor: colors.surfaceElevated,
  },
  select: {
    flex: 1,
    minHeight: 44,
    justifyContent: 'center',
    borderRadius: radii.small,
    padding: spacing.small,
  },
  selected: {
    borderWidth: 1,
    borderColor: colors.brandSecondary,
    backgroundColor: colors.brandSurface,
  },
  itemTitle: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  orderActions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.extraSmall,
  },
});
