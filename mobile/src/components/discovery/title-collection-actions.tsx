import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';

import ClockIcon from '../../../assets/brand/navigation-icons/clock.svg';
import FavoritesIcon from '../../../assets/brand/navigation-icons/favorites.svg';
import ListIcon from '../../../assets/brand/navigation-icons/list-add.svg';
import CheckIcon from '../../../assets/brand/navigation-icons/check.svg';
import { IconAction } from '@/components/icon-action';
import { ModalDialog } from '@/components/modal-dialog';
import { PrimaryButton } from '@/components/auth/primary-button';
import { useErrorToast } from '@/providers/toast-provider';
import {
  createCustomList, fetchCustomLists, fetchTitleStatuses, removeFavorite, removeWatchlistTitle,
  setCustomListTitle, setFavorite, setWatchlistTitle, type CatalogTitle,
} from '@/services/lists-api';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export function TitleCollectionActions({ title }: { title: CatalogTitle }) {
  const queryClient = useQueryClient();
  const [listPickerVisible, setListPickerVisible] = useState(false);
  const [listName, setListName] = useState('');
  const [nameFocused, setNameFocused] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [addedLists, setAddedLists] = useState<string[]>([]);
  const status = useQuery({
    queryKey: ['lists', 'title-status', [title.id]],
    queryFn: () => fetchTitleStatuses([title.id]),
  });
  const lists = useInfiniteQuery({
    queryKey: ['lists', 'custom', 'title-picker'],
    queryFn: ({ pageParam }) => fetchCustomLists(pageParam),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => lastPage.page * lastPage.pageSize < lastPage.totalCount ? lastPage.page + 1 : undefined,
    enabled: listPickerVisible,
  });
  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['lists'] }),
      queryClient.invalidateQueries({ queryKey: ['social', 'friend-profile'] }),
    ]);
  };
  const relation = status.data?.find((item) => item.titleId === title.id);
  const update = useMutation({
    mutationFn: async (target: 'favorite' | 'watchlist') => {
      if (!relation) throw new Error('Atualize o estado do título antes de continuar.');
      if (target === 'favorite') return relation.isFavorite ? removeFavorite(title.id) : setFavorite(title);
      return relation.isInWatchlist ? removeWatchlistTitle(title.id) : setWatchlistTitle(title);
    },
    onSuccess: refresh,
  });
  const addToList = useMutation({
    mutationFn: async (listId: string) => ({ listId, result: await setCustomListTitle(listId, title) }),
    onSuccess: async ({ listId, result }) => {
      setAddedLists((current) => [...new Set([...current, listId])]);
      setFeedback(result.added ? 'Título adicionado à lista.' : 'Este título já está nesta lista.');
      await refresh();
    },
  });
  const createList = useMutation({
    mutationFn: async () => {
      const list = await createCustomList(listName.trim(), '');
      await setCustomListTitle(list.id, title);
      return list;
    },
    onSuccess: async (list) => {
      setAddedLists((current) => [...current, list.id]);
      setListName('');
      setFeedback('Lista criada e título adicionado.');
      await refresh();
    },
    onError: () => { void queryClient.invalidateQueries({ queryKey: ['lists', 'custom'] }); },
  });
  useErrorToast(status.error, 'Não foi possível verificar seus títulos salvos.');
  useErrorToast(lists.error, 'Não foi possível carregar suas listas.');
  useErrorToast(update.error, 'Não foi possível salvar o título.');
  useErrorToast(addToList.error, 'Não foi possível adicionar à lista.');
  useErrorToast(createList.error, 'Não foi possível criar a lista e adicionar o título.');
  const busy = update.isPending || status.isFetching || !relation;
  const listBusy = addToList.isPending || createList.isPending;

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
      <PrimaryButton label="Criar e adicionar" disabled={!listName.trim() || listBusy} loading={createList.isPending} onPress={() => { setFeedback(null); createList.mutate(); }} />
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