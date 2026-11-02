import { useQuery, useQueryClient, useQueries } from '@tanstack/react-query';
import { Redirect, useLocalSearchParams } from 'expo-router';
import { useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
  useWindowDimensions,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { PrimaryButton } from '@/components/auth/primary-button';
import { MediaTitleRow } from '@/components/discovery/media-title-row';
import { BackButton } from '@/components/navigation/back-button';
import { ModalDialog } from '@/components/modal-dialog';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast, useToast } from '@/providers/toast-provider';
import {
  createCustomList,
  deleteCustomList,
  fetchCustomList,
  fetchCustomLists,
  fetchFavorites,
  fetchTitleDetails,
  fetchTitleStatuses,
  fetchWatchlist,
  moveCustomListTitle,
  removeCustomListTitle,
  removeFavorite,
  removeWatchlistTitle,
  searchCatalogTitles,
  setCustomListTitle,
  setFavorite,
  setWatchlistTitle,
  updateCustomList,
  type CatalogTitle,
  type CustomListSummary,
  type TitleReference,
} from '@/services/lists-api';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import AddListIcon from '../../assets/brand/navigation-icons/list-add.svg';
import FavoritesIcon from '../../assets/brand/navigation-icons/favorites.svg';
import ClockIcon from '../../assets/brand/navigation-icons/clock.svg';
import CheckIcon from '../../assets/brand/navigation-icons/check.svg';
import TrashIcon from '../../assets/brand/navigation-icons/trash.svg';
import ArrowUpIcon from '../../assets/brand/navigation-icons/arrow-up.svg';
import ArrowDownIcon from '../../assets/brand/navigation-icons/arrow-down.svg';

type Section = 'favorites' | 'watchlist' | 'lists';

export default function ListsScreen() {
  const params = useLocalSearchParams<{ section?: string }>();
  const { width } = useWindowDimensions();
  const listCardWidth = Math.min(dimensions.drawerWidth, width - spacing.medium * 2);
  const { session } = useAuthSession();
  const queryClient = useQueryClient();
  const { showError } = useToast();
  const [section, setSection] = useState<Section>(() =>
    params.section === 'lists' || params.section === 'watchlist' ? params.section : 'favorites');
  const [page, setPage] = useState(1);
  const [listPage, setListPage] = useState(1);
  const [watchlistOldestFirst, setWatchlistOldestFirst] = useState(false);
  const [selectedListId, setSelectedListId] = useState<string | null>(null);
  const [searchText, setSearchText] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [editingList, setEditingList] = useState<CustomListSummary | null>(null);
  const [formVisible, setFormVisible] = useState(false);
  const [menuList, setMenuList] = useState<CustomListSummary | null>(null);
  const [deletingList, setDeletingList] = useState<CustomListSummary | null>(null);
  const [savingList, setSavingList] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [focusedField, setFocusedField] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const favorites = useQuery({
    queryKey: ['lists', 'favorites', page],
    queryFn: () => fetchFavorites(page),
    enabled: section === 'favorites',
  });
  const watchlist = useQuery({
    queryKey: ['lists', 'watchlist', page, watchlistOldestFirst],
    queryFn: () => fetchWatchlist(page, watchlistOldestFirst),
    enabled: section === 'watchlist',
  });
  const customLists = useQuery({
    queryKey: ['lists', 'custom', listPage],
    queryFn: () => fetchCustomLists(listPage),
    enabled: section === 'lists',
  });
  const activeListId = selectedListId ?? customLists.data?.items[0]?.id ?? null;
  const selectedList = useQuery({
    queryKey: ['lists', 'custom', activeListId, page],
    queryFn: () => fetchCustomList(activeListId!, page),
    enabled: section === 'lists' && activeListId !== null,
  });
  const search = useQuery({
    queryKey: ['lists', 'catalog-search', searchText.trim()],
    queryFn: () => searchCatalogTitles(searchText.trim()),
    enabled: searchText.trim().length >= 2,
  });
  const searchTitleIds = search.data?.map((title) => title.id) ?? [];
  const searchStatuses = useQuery({
    queryKey: ['lists', 'title-status', searchTitleIds],
    queryFn: () => fetchTitleStatuses(searchTitleIds),
    enabled: searchTitleIds.length > 0,
  });

  useErrorToast(favorites.error, 'Não foi possível carregar seus favoritos. Verifique a conexão e tente novamente.');
  useErrorToast(watchlist.error, 'Não foi possível carregar sua watchlist. Verifique a conexão e tente novamente.');
  useErrorToast(customLists.error, 'Não foi possível carregar suas listas. Verifique a conexão e tente novamente.');
  useErrorToast(selectedList.error, 'Não foi possível carregar esta lista. Verifique a conexão e tente novamente.');
  useErrorToast(search.error, 'Não foi possível buscar títulos. Verifique a conexão e tente novamente.');
  useErrorToast(searchStatuses.error, 'Não foi possível verificar seus títulos salvos. Tente novamente.');

  const changeSection = (nextSection: Section) => {
    setPage(1);
    setSection(nextSection);
  };

  const currentReferences = useMemo(() => {
    if (section === 'favorites') {
      return favorites.data?.items ?? [];
    }
    if (section === 'watchlist') {
      return watchlist.data?.items ?? [];
    }
    return selectedList.data?.titles.items ?? [];
  }, [favorites.data, section, selectedList.data, watchlist.data]);

  const titleDetails = useQueries({
    queries: currentReferences.map((reference) => ({
      queryKey: ['catalog', 'title', reference.provider, reference.contentType, reference.externalId],
      queryFn: () => fetchTitleDetails(reference),
      staleTime: 300_000,
    })),
  });
  const detailsFailed = titleDetails.some((result) => result.isError);
  useEffect(() => {
    if (detailsFailed) {
      showError('Não foi possível carregar os detalhes de alguns títulos. Tente novamente.');
    }
  }, [detailsFailed, showError]);

  if (!session) {
    return <Redirect href="/" />;
  }

  const invalidateLists = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['lists', 'favorites'] }),
      queryClient.invalidateQueries({ queryKey: ['lists', 'watchlist'] }),
      queryClient.invalidateQueries({ queryKey: ['lists', 'custom'] }),
      queryClient.invalidateQueries({ queryKey: ['lists', 'title-status'] }),
    ]);
  };

  const runMutation = async (action: () => Promise<unknown>, successMessage: string) => {
    setSuccessMessage(null);
    try {
      await action();
      await invalidateLists();
      setSuccessMessage(successMessage);
    } catch (error) {
      showError(
        error instanceof Error
          ? error.message
          : 'Não foi possível salvar a alteração. Verifique a conexão e tente novamente.',
      );
    }
  };

  const saveList = async () => {
    if (savingList) return;
    const normalizedName = name.trim();
    if (!normalizedName || normalizedName.length > 80) {
      setFormError('Informe um nome com até 80 caracteres.');
      return;
    }
    if (description.trim().length > 500) {
      setFormError('A descrição deve ter no máximo 500 caracteres.');
      return;
    }
    setFormError(null);
    setSavingList(true);
    await runMutation(async () => {
      const saved = editingList
        ? await updateCustomList(editingList.id, normalizedName, description.trim())
        : await createCustomList(normalizedName, description.trim());
      setName('');
      setDescription('');
      setEditingList(null);
      setFormVisible(false);
      setSelectedListId(saved.id);
      setListPage(1);
      changeSection('lists');
    }, editingList ? 'Lista atualizada.' : 'Lista criada.');
    setSavingList(false);
  };

  const openListForm = (list: CustomListSummary | null) => {
    setEditingList(list);
    setName(list?.name ?? '');
    setDescription(list?.description ?? '');
    setFormError(null);
    setMenuList(null);
    setFormVisible(true);
  };

  const confirmDeleteList = async () => {
    if (!deletingList || savingList) return;
    setSavingList(true);
    await runMutation(async () => {
      await deleteCustomList(deletingList.id);
      setDeletingList(null);
      setSelectedListId(null);
      setListPage(1);
      setPage(1);
    }, 'Lista excluída.');
    setSavingList(false);
  };

  const removeTitle = async (reference: TitleReference) => {
    if (section === 'favorites') {
      await runMutation(() => removeFavorite(reference.titleId), 'Título removido dos favoritos.');
    } else if (section === 'watchlist') {
      await runMutation(() => removeWatchlistTitle(reference.titleId), 'Título removido da watchlist.');
    } else if (activeListId) {
      await runMutation(
        () => removeCustomListTitle(activeListId, reference.titleId),
        'Título removido da lista.',
      );
    }
  };

  const moveListTitle = async (titleId: string, offset: -1 | 1) => {
    if (activeListId) {
      await runMutation(
        () => moveCustomListTitle(activeListId, titleId, offset),
        'Ordem da lista atualizada.',
      );
    }
  };

  const addSearchTitle = async (title: CatalogTitle, destination: Section) => {
    if (destination === 'favorites') {
      await runMutation(async () => {
        await setFavorite(title);
        changeSection('favorites');
      }, 'Título adicionado aos favoritos.');
    } else if (destination === 'watchlist') {
      await runMutation(async () => {
        await setWatchlistTitle(title);
        changeSection('watchlist');
      }, 'Título adicionado à watchlist.');
    } else if (activeListId) {
      await runMutation(
        () => setCustomListTitle(activeListId, title),
        'Título adicionado à lista.',
      );
    }
  };

  const statusByTitleId = new Map(
    (searchStatuses.data ?? []).map((status) => [status.titleId, status]),
  );
  const displayedPage = section === 'favorites'
    ? favorites.data
    : section === 'watchlist'
      ? watchlist.data
      : selectedList.data?.titles;
  const isPending = section === 'favorites'
    ? favorites.isPending
    : section === 'watchlist'
      ? watchlist.isPending
      : selectedList.isPending;
  const shouldShowRetry = section === 'favorites'
    ? favorites.isError
    : section === 'watchlist'
      ? watchlist.isError
      : selectedList.isError;

  const lists = customLists.data?.items ?? [];
  const visibleLists = selectedList.data && !lists.some((list) => list.id === selectedList.data.list.id)
    ? [selectedList.data.list, ...lists]
    : lists;
  const listContents = (
    <View style={styles.listContents}>
      {selectedList.isPending ? <ActivityIndicator color={colors.brand} /> : null}
      {selectedList.isError ? (
        <PrimaryButton label="Tentar novamente" onPress={() => void selectedList.refetch()} variant="secondary" />
      ) : null}
      {selectedList.data?.titles.items.length === 0 ? (
        <Text style={styles.muted}>Esta lista ainda não tem títulos. Use a busca para adicionar.</Text>
      ) : null}
      <ScrollView
        key={`${activeListId}-${page}`}
        accessibilityLabel="Conteúdos da lista privada"
        horizontal
        nestedScrollEnabled
        directionalLockEnabled
        keyboardShouldPersistTaps="handled"
        contentContainerStyle={styles.horizontalCards}
      >
      {selectedList.data?.titles.items.map((reference, index) => titleDetails[index]?.data ? (
        <MediaTitleRow key={reference.titleId} title={titleDetails[index].data} layout="poster">
          <ListActionButton Icon={TrashIcon} label="Remover" onPress={() => void removeTitle(reference)} />
          <View style={styles.orderActions}>
            <ListActionButton
              Icon={ArrowUpIcon}
              disabled={reference.position === 0}
              label="Mover para cima"
              onPress={() => void moveListTitle(reference.titleId, -1)}
            />
            <ListActionButton
              Icon={ArrowDownIcon}
              disabled={reference.position === (selectedList.data?.list.totalCount ?? 0) - 1}
              label="Mover para baixo"
              onPress={() => void moveListTitle(reference.titleId, 1)}
            />
          </View>
        </MediaTitleRow>
      ) : (
        <View key={reference.titleId} style={styles.unavailableCard}>
          {titleDetails[index]?.isPending ? (
            <ActivityIndicator color={colors.brand} />
          ) : (
            <View style={styles.titleCopy}>
              <Text style={styles.muted}>Detalhes indisponíveis no catálogo.</Text>
              <SmallButton label="Tentar carregar detalhes" onPress={() => void titleDetails[index]?.refetch()} />
            </View>
          )}
          <ListActionButton Icon={TrashIcon} label="Remover" onPress={() => void removeTitle(reference)} />
        </View>
      ))}
      </ScrollView>
      {selectedList.data ? (
        <Pager
          page={selectedList.data.titles.page}
          pageSize={selectedList.data.titles.pageSize}
          totalCount={selectedList.data.titles.totalCount}
          onChange={setPage}
        />
      ) : null}
    </View>
  );

  return (
    <SafeAreaView style={styles.screen}>
    <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
      <View style={styles.heading}>
        <BackButton />
        <Text accessibilityRole="header" style={styles.title}>Seus títulos e listas</Text>
        <Text style={styles.subtitle}>Organize o que você gosta e quer assistir.</Text>
      </View>
      <View style={styles.tabs}>
        <SectionButton label="Favoritos" active={section === 'favorites'} onPress={() => changeSection('favorites')} />
        <SectionButton label="Watchlist" active={section === 'watchlist'} onPress={() => changeSection('watchlist')} />
        <SectionButton label="Minhas listas" active={section === 'lists'} onPress={() => changeSection('lists')} />
      </View>
      {successMessage ? <Text style={styles.successMessage}>{successMessage}</Text> : null}

      <View style={styles.searchSection}>
        <Text style={styles.sectionTitle}>Buscar no catálogo</Text>
        <TextInput
          accessibilityLabel="Buscar filmes e séries no catálogo"
          onChangeText={setSearchText}
          onFocus={() => setFocusedField('search')}
          onBlur={() => setFocusedField(null)}
          placeholder="Nome de um filme ou série"
          placeholderTextColor={colors.textSecondary}
          style={[styles.input, focusedField === 'search' ? styles.inputFocused : null]}
          value={searchText}
        />
        {searchText.trim().length >= 2 && search.isPending ? (
          <ActivityIndicator color={colors.brand} />
        ) : null}
        {search.isError ? (
          <PrimaryButton label="Tentar novamente" onPress={() => void search.refetch()} variant="secondary" />
        ) : null}
        {searchStatuses.isError ? (
          <PrimaryButton
            label="Tentar verificar títulos salvos"
            onPress={() => void searchStatuses.refetch()}
            variant="secondary"
          />
        ) : null}
        {search.data?.length === 0 ? <Text style={styles.muted}>Nenhum título encontrado.</Text> : null}
        <ScrollView
          key={searchText.trim()}
          accessibilityLabel="Resultados do catálogo"
          horizontal
          directionalLockEnabled
          keyboardShouldPersistTaps="handled"
          contentContainerStyle={styles.horizontalCards}
        >
        {search.data?.map((title) => (
          <MediaTitleRow key={title.id} title={title} layout="poster">
            <View style={styles.searchActions}>
              <ListActionButton
                Icon={FavoritesIcon}
                selected={statusByTitleId.get(title.id)?.isFavorite}
                label={statusByTitleId.get(title.id)?.isFavorite ? 'Remover favorito' : 'Adicionar favorito'}
                onPress={() => void runMutation(
                  () => statusByTitleId.get(title.id)?.isFavorite
                    ? removeFavorite(title.id)
                    : setFavorite(title),
                  statusByTitleId.get(title.id)?.isFavorite
                    ? 'Título removido dos favoritos.'
                    : 'Título adicionado aos favoritos.',
                )}
              />
              <ListActionButton
                Icon={ClockIcon}
                selected={statusByTitleId.get(title.id)?.isInWatchlist}
                label={statusByTitleId.get(title.id)?.isInWatchlist ? 'Remover da watchlist' : 'Adicionar à watchlist'}
                onPress={() => void runMutation(
                  () => statusByTitleId.get(title.id)?.isInWatchlist
                    ? removeWatchlistTitle(title.id)
                    : setWatchlistTitle(title),
                  statusByTitleId.get(title.id)?.isInWatchlist
                    ? 'Título removido da watchlist.'
                    : 'Título adicionado à watchlist.',
                )}
              />
              {section === 'lists' && activeListId ? (
                <ListActionButton Icon={AddListIcon} label="Adicionar à lista" onPress={() => void addSearchTitle(title, 'lists')} />
              ) : null}
            </View>
          </MediaTitleRow>
        ))}
        </ScrollView>
      </View>

      {section === 'lists' ? (
        <View style={styles.listSection}>
          <View style={styles.listHeader}>
            <Text style={styles.sectionTitle}>Listas privadas</Text>
            <Pressable
              accessibilityLabel="Adicionar nova lista"
              accessibilityRole="button"
              onPress={() => openListForm(null)}
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
            <Text style={styles.muted}>Você ainda não criou listas.</Text>
          ) : null}
          <ScrollView
            key={listPage}
            accessibilityLabel="Listas privadas"
            horizontal
            nestedScrollEnabled
            directionalLockEnabled
            keyboardShouldPersistTaps="handled"
            contentContainerStyle={styles.horizontalCards}
          >
          {visibleLists.map((summary) => {
            const list = summary.id === activeListId && selectedList.data
              ? selectedList.data.list
              : summary;
            return (
              <View key={list.id} style={[styles.listCard, { width: listCardWidth }]}>
                <View style={styles.listHeader}>
                  <Pressable
                    accessibilityLabel={`Abrir lista ${list.name}`}
                    accessibilityRole="button"
                    accessibilityState={{ selected: list.id === activeListId, expanded: list.id === activeListId }}
                    onPress={() => {
                      setPage(1);
                      setSelectedListId(list.id);
                    }}
                    style={[styles.listSelect, list.id === activeListId ? styles.selected : null]}
                  >
                    <View style={styles.titleCopy}>
                      <Text style={styles.itemTitle}>{list.name}</Text>
                      <Text style={styles.muted}>{list.totalCount} filmes e séries · privada</Text>
                      {list.description ? <Text style={styles.muted}>{list.description}</Text> : null}
                    </View>
                  </Pressable>
                  <SmallButton label="..." accessibilityLabel={`Opções da lista ${list.name}`} onPress={() => setMenuList(list)} iconOnly />
                </View>
                {list.id === activeListId ? listContents : null}
              </View>
            );
          })}
          </ScrollView>
          {customLists.data ? (
            <Pager
              page={listPage}
              pageSize={customLists.data.pageSize}
              totalCount={customLists.data.totalCount}
              onChange={(nextPage) => {
                setSelectedListId(null);
                setPage(1);
                setListPage(nextPage);
              }}
            />
          ) : null}
        </View>
      ) : (
        <View style={styles.listSection}>
          <Text style={styles.sectionTitle}>
            {section === 'favorites' ? 'Favoritos' : 'Watchlist'}
            {displayedPage ? ` · ${displayedPage.totalCount}` : ''}
          </Text>
          {section === 'watchlist' ? (
            <SmallButton
              label={`Ordenar: ${watchlistOldestFirst ? 'mais antigas' : 'mais recentes'}`}
              onPress={() => {
                setPage(1);
                setWatchlistOldestFirst((current) => !current);
              }}
            />
          ) : null}
          {isPending ? <ActivityIndicator color={colors.brand} /> : null}
          {shouldShowRetry ? (
            <PrimaryButton
              label="Tentar novamente"
              onPress={() => void (section === 'favorites' ? favorites.refetch() : watchlist.refetch())}
              variant="secondary"
            />
          ) : null}
          {!isPending && displayedPage?.items.length === 0 ? (
            <Text style={styles.muted}>Ainda não há títulos nesta seção.</Text>
          ) : null}
          <ScrollView
            key={`${section}-${page}-${watchlistOldestFirst}`}
            accessibilityLabel={section === 'favorites' ? 'Títulos favoritos' : 'Títulos da watchlist'}
            horizontal
            directionalLockEnabled
            keyboardShouldPersistTaps="handled"
            contentContainerStyle={styles.horizontalCards}
          >
          {displayedPage?.items.map((reference, index) => titleDetails[index]?.data ? (
            <MediaTitleRow key={reference.titleId} title={titleDetails[index].data} layout="poster">
              <ListActionButton Icon={TrashIcon} label="Remover" onPress={() => void removeTitle(reference)} />
            </MediaTitleRow>
          ) : (
            <View key={reference.titleId} style={styles.unavailableCard}>
              {titleDetails[index]?.isPending ? (
                <ActivityIndicator color={colors.brand} />
              ) : (
                <View style={styles.titleCopy}>
                  <Text style={styles.muted}>Detalhes indisponíveis no catálogo.</Text>
                  <SmallButton
                    label="Tentar carregar detalhes"
                    onPress={() => void titleDetails[index]?.refetch()}
                  />
                </View>
              )}
              <ListActionButton Icon={TrashIcon} label="Remover" onPress={() => void removeTitle(reference)} />
            </View>
          ))}
          </ScrollView>
          {displayedPage ? (
            <Pager
              page={displayedPage.page}
              pageSize={displayedPage.pageSize}
              totalCount={displayedPage.totalCount}
              onChange={setPage}
            />
          ) : null}
        </View>
      )}

      <ModalDialog
        visible={formVisible || menuList !== null || deletingList !== null}
        title={deletingList ? 'Excluir lista?' : menuList ? menuList.name : editingList ? 'Editar lista' : 'Criar lista'}
        busy={savingList}
        onClose={() => {
          setFormVisible(false);
          setMenuList(null);
          setDeletingList(null);
        }}
      >
        {formVisible ? (
          <>
        <TextInput
          accessibilityLabel="Nome da lista"
          editable={!savingList}
          onChangeText={setName}
          onFocus={() => setFocusedField('name')}
          onBlur={() => setFocusedField(null)}
          placeholder="Nome"
          placeholderTextColor={colors.textSecondary}
          style={[styles.input, focusedField === 'name' ? styles.inputFocused : null]}
          value={name}
        />
        <TextInput
          accessibilityLabel="Descrição da lista"
          editable={!savingList}
          onChangeText={setDescription}
          onFocus={() => setFocusedField('description')}
          onBlur={() => setFocusedField(null)}
          placeholder="Descrição (opcional)"
          placeholderTextColor={colors.textSecondary}
          style={[styles.input, styles.descriptionInput, focusedField === 'description' ? styles.inputFocused : null]}
          value={description}
          multiline
        />
        {formError ? <Text accessibilityRole="alert" style={styles.validation}>{formError}</Text> : null}
        <PrimaryButton
          label={editingList ? 'Salvar alterações' : 'Criar lista'}
          loading={savingList}
          onPress={() => void saveList()}
        />
        <PrimaryButton label="Cancelar" disabled={savingList} variant="secondary" onPress={() => setFormVisible(false)} />
          </>
        ) : menuList ? (
          <>
        <PrimaryButton label="Editar lista" variant="secondary" onPress={() => openListForm(menuList)} />
        <PrimaryButton label="Excluir lista" variant="secondary" onPress={() => {
          setDeletingList(menuList);
          setMenuList(null);
        }} />
          </>
        ) : deletingList ? (
          <>
        <Text style={styles.subtitle}>Excluir a lista &quot;{deletingList?.name}&quot;?</Text>
        <Text style={styles.muted}>
          Apenas a lista e seus vínculos serão removidos. Filmes e séries do catálogo, Favoritos e Watchlist não serão alterados.
        </Text>
        <PrimaryButton label="Confirmar exclusão" loading={savingList} onPress={() => void confirmDeleteList()} />
        <PrimaryButton label="Cancelar" disabled={savingList} variant="secondary" onPress={() => setDeletingList(null)} />
          </>
        ) : null}
      </ModalDialog>
    </ScrollView>
    </SafeAreaView>
  );
}

function SectionButton({
  label,
  active,
  onPress,
}: {
  label: string;
  active: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="tab"
      accessibilityLabel={label}
      accessibilityState={{ selected: active }}
      onPress={onPress}
      style={[styles.tab, active ? styles.activeTab : null]}
    >
      <Text style={[styles.tabLabel, active ? styles.activeTabLabel : null]}>{label}</Text>
    </Pressable>
  );
}

function SmallButton({
  label,
  onPress,
  disabled = false,
  accessibilityLabel = label,
  iconOnly = false,
}: {
  label: string;
  onPress: () => void;
  disabled?: boolean;
  accessibilityLabel?: string;
  iconOnly?: boolean;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel}
      accessibilityState={{ disabled }}
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [
        styles.smallButton,
        iconOnly ? styles.pagerIconButton : null,
        pressed ? styles.pressed : null,
        disabled ? styles.disabled : null,
      ]}
    >
      <Text style={[styles.smallButtonLabel, iconOnly ? styles.pagerIconLabel : null]}>{label}</Text>
    </Pressable>
  );
}

function ListActionButton({
  Icon,
  label,
  selected = false,
  disabled = false,
  onPress,
}: {
  Icon: typeof AddListIcon;
  label: string;
  selected?: boolean;
  disabled?: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ selected, disabled }}
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [
        styles.catalogAction,
        selected && styles.selected,
        pressed && styles.pressed,
        disabled && styles.disabled,
      ]}
    >
      <Icon width={dimensions.iconSize} height={dimensions.iconSize} />
      {selected ? (
        <View pointerEvents="none" style={styles.actionCheck}>
          <CheckIcon width={dimensions.iconSize / 2} height={dimensions.iconSize / 2} />
        </View>
      ) : null}
    </Pressable>
  );
}

function Pager({
  page,
  pageSize,
  totalCount,
  onChange,
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  onChange: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  if (totalPages < 2) {
    return null;
  }

  return (
    <View style={styles.pager}>
      <SmallButton
        accessibilityLabel="Página anterior"
        disabled={page <= 1}
        iconOnly
        label="←"
        onPress={() => onChange(Math.max(1, page - 1))}
      />
      <Text style={styles.muted}>Página {page} de {totalPages}</Text>
      <SmallButton
        accessibilityLabel="Próxima página"
        disabled={page >= totalPages}
        iconOnly
        label="→"
        onPress={() => onChange(Math.min(totalPages, page + 1))}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background },
  container: {
    flexGrow: 1,
    gap: spacing.large,
    padding: spacing.medium,
    paddingBottom: spacing.extraLarge,
    backgroundColor: colors.background,
  },
  heading: {
    gap: spacing.small,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.bold,
  },
  subtitle: {
    color: colors.textSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  tabs: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.small,
  },
  tab: {
    minHeight: 44,
    justifyContent: 'center',
    borderRadius: radii.pill,
    borderColor: colors.border,
    borderWidth: 1,
    paddingHorizontal: spacing.medium,
  },
  activeTab: {
    backgroundColor: colors.brand,
    borderColor: colors.brand,
  },
  tabLabel: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  activeTabLabel: {
    color: colors.background,
  },
  searchSection: {
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
  },
  listSection: {
    gap: spacing.medium,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  input: {
    minHeight: 48,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    color: colors.textPrimary,
    backgroundColor: colors.surfaceElevated,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  inputFocused: {
    borderColor: colors.brandSecondary,
    borderWidth: 2,
  },
  descriptionInput: {
    minHeight: 88,
    paddingTop: spacing.medium,
    textAlignVertical: 'top',
  },
  searchResult: {
    gap: spacing.small,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: spacing.small,
  },
  searchActions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.extraSmall,
  },
  orderActions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.extraSmall,
  },
  horizontalCards: {
    gap: spacing.small,
    alignItems: 'flex-start',
    paddingBottom: spacing.small,
  },
  unavailableCard: {
    width: dimensions.minimumTouchTarget * 3 + spacing.small * 4,
    minHeight: dimensions.posterHeight,
    gap: spacing.small,
    padding: spacing.small,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  titleCopy: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  itemTitle: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  muted: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  catalogAction: {
    width: dimensions.minimumTouchTarget,
    height: dimensions.minimumTouchTarget,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.small,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceElevated,
  },
  actionCheck: {
    position: 'absolute',
    right: spacing.extraSmall,
    bottom: spacing.extraSmall,
    borderRadius: radii.pill,
    backgroundColor: colors.surface,
  },
  smallButton: {
    minHeight: 44,
    justifyContent: 'center',
    borderRadius: radii.small,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.small,
    backgroundColor: colors.surfaceElevated,
  },
  pagerIconButton: {
    minWidth: 44,
    paddingHorizontal: spacing.medium,
  },
  smallButtonLabel: {
    color: colors.textPrimary,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
    textAlign: 'center',
  },
  pagerIconLabel: {
    fontSize: typography.section,
    lineHeight: typography.section + 4,
  },
  listCard: {
    gap: spacing.small,
    borderRadius: radii.medium,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.small,
    backgroundColor: colors.surface,
  },
  listHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
  },
  listContents: { gap: spacing.small },
  iconButton: {
    minWidth: dimensions.minimumTouchTarget,
    minHeight: dimensions.minimumTouchTarget,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.small,
    backgroundColor: colors.surfaceElevated,
  },
  listSelect: {
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
  validation: {
    color: colors.warning,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  successMessage: {
    color: colors.success,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  pager: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
  },
  pressed: {
    opacity: 0.75,
  },
  disabled: {
    opacity: 0.55,
  },
});
