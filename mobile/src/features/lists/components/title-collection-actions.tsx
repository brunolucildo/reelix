import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';

import ClockIcon from '@/assets/brand/navigation-icons/clock.svg';
import FavoritesIcon from '@/assets/brand/navigation-icons/favorites.svg';
import ListIcon from '@/assets/brand/navigation-icons/list-add.svg';
import CheckIcon from '@/assets/brand/navigation-icons/check.svg';
import { IconAction } from '@/components/ui/icon-action';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { PrimaryButton } from '@/components/ui/primary-button';
import type { CatalogTitle } from '@/features/catalog/api/catalog-api';
import { useTitleCollectionActions } from '@/features/lists/hooks/use-title-collection-actions';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export function TitleCollectionActions({ title }: { title: CatalogTitle }) {
  const [listPickerVisible, setListPickerVisible] = useState(false);
  const [listName, setListName] = useState('');
  const [nameFocused, setNameFocused] = useState(false);
  const {
    status, relation, lists, update, addToList, createList,
    feedback, setFeedback, addedLists, busy, listBusy,
  } = useTitleCollectionActions(title, listPickerVisible);

  return <>
    <View style={styles.actions}>
      {status.isError ? <IconAction label="Tentar carregar títulos salvos" onPress={() => void status.refetch()}><ClockIcon width={dimensions.iconSize} height={dimensions.iconSize} /></IconAction> : <>
        <IconAction label={relation?.isInWatchlist ? 'Remover de Quero assistir' : 'Adicionar a Quero assistir'} selected={relation?.isInWatchlist ?? false} disabled={busy} loading={status.isPending || (update.isPending && update.variables === 'watchlist')} onPress={() => update.mutate('watchlist')}><ClockIcon width={dimensions.iconSize} height={dimensions.iconSize} /></IconAction>
        <IconAction label={relation?.isFavorite ? 'Remover dos favoritos' : 'Adicionar aos favoritos'} selected={relation?.isFavorite ?? false} disabled={busy} loading={status.isPending || (update.isPending && update.variables === 'favorite')} onPress={() => update.mutate('favorite')}><FavoritesIcon width={dimensions.iconSize} height={dimensions.iconSize} /></IconAction>
      </>}
      <IconAction label="Adicionar à minha lista" onPress={() => { setFeedback(null); setListPickerVisible(true); }}><ListIcon width={dimensions.iconSize} height={dimensions.iconSize} /></IconAction>
    </View>
    <ModalDialog visible={listPickerVisible} title="Adicionar à minha lista" busy={listBusy} onClose={() => setListPickerVisible(false)}>
      {lists.isPending ? <ActivityIndicator accessibilityLabel="Carregando listas" color={colors.brand} /> : lists.isError ? <PrimaryButton label="Tentar novamente" variant="secondary" onPress={() => void lists.refetch()} /> : (lists.data?.pages.flatMap((page) => page.items) ?? []).map((list) => <Pressable key={list.id} accessibilityRole="button" accessibilityLabel={`Adicionar ${title.title} à lista ${list.name}`} disabled={listBusy || addedLists.includes(list.id)} onPress={() => { setFeedback(null); addToList.mutate(list.id); }} style={styles.listRow}>
        <ListIcon width={dimensions.iconSize} height={dimensions.iconSize} />
        <Text style={styles.listName}>{list.name}</Text>
        {addedLists.includes(list.id) ? <CheckIcon width={dimensions.iconSize} height={dimensions.iconSize} /> : null}
      </Pressable>)}
      {lists.data?.pages[0]?.totalCount === 0 ? <Text style={styles.body}>Você ainda não criou listas.</Text> : null}
      {lists.hasNextPage ? <PrimaryButton label="Mais listas" variant="secondary" loading={lists.isFetchingNextPage} onPress={() => void lists.fetchNextPage()} /> : null}
      <Text style={styles.heading}>Nova lista</Text>
      <TextInput accessibilityLabel="Nome da nova lista" placeholder="Nome da lista" placeholderTextColor={colors.textSecondary} value={listName} onChangeText={setListName} maxLength={80} onFocus={() => setNameFocused(true)} onBlur={() => setNameFocused(false)} style={[styles.input, nameFocused && styles.inputFocused]} editable={!listBusy} />
      <PrimaryButton label="Criar e adicionar" disabled={!listName.trim() || listBusy} loading={createList.isPending} onPress={() => { setFeedback(null); createList.mutate(listName.trim(), { onSuccess: () => setListName('') }); }} />
      {feedback ? <Text accessibilityLiveRegion="polite" style={styles.success}>{feedback}</Text> : null}
    </ModalDialog>
  </>;
}

const styles = StyleSheet.create({
  actions: { flexDirection: 'row', gap: spacing.small, zIndex: 1 },
  listRow: { minHeight: dimensions.minimumTouchTarget, flexDirection: 'row', alignItems: 'center', gap: spacing.medium, borderBottomWidth: 1, borderBottomColor: colors.border, paddingVertical: spacing.small },
  listName: { flex: 1, color: colors.textPrimary, fontFamily: fontFamily.medium, fontSize: typography.body },
  body: { color: colors.textSecondary, fontFamily: fontFamily.regular, fontSize: typography.bodySmall },
  heading: { color: colors.textPrimary, fontFamily: fontFamily.bold, fontSize: typography.section },
  input: { minHeight: dimensions.minimumTouchTarget, padding: spacing.medium, borderWidth: 1, borderColor: colors.border, borderRadius: radii.small, color: colors.textPrimary, fontFamily: fontFamily.regular, fontSize: typography.body, backgroundColor: colors.surface },
  inputFocused: { borderColor: colors.brandSecondary },
  success: { color: colors.success, fontFamily: fontFamily.medium, fontSize: typography.bodySmall },
});