import { useQuery, useQueryClient, useQueries } from '@tanstack/react-query';
import { Redirect, useRouter } from 'expo-router';
import { useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

import { PrimaryButton } from '@/components/auth/primary-button';
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
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Section = 'favorites' | 'watchlist' | 'lists';

export default function ListsScreen() {
  const router = useRouter();
  const { session } = useAuthSession();
  const queryClient = useQueryClient();
  const { showError } = useToast();
  const [section, setSection] = useState<Section>('favorites');
  const [page, setPage] = useState(1);
  const [listPage, setListPage] = useState(1);
  const [watchlistOldestFirst, setWatchlistOldestFirst] = useState(false);
  const [selectedListId, setSelectedListId] = useState<string | null>(null);
  const [searchText, setSearchText] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [editingList, setEditingList] = useState<CustomListSummary | null>(null);
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
    } catch {
      showError('Não foi possível salvar a alteração. Verifique a conexão e tente novamente.');
    }
  };

  const saveList = async () => {
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
    await runMutation(async () => {
      const saved = editingList
        ? await updateCustomList(editingList.id, normalizedName, description.trim())
        : await createCustomList(normalizedName, description.trim());
      setName('');
      setDescription('');
      setEditingList(null);
      setSelectedListId(saved.id);
      changeSection('lists');
    }, editingList ? 'Lista atualizada.' : 'Lista criada.');
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

  return (
    <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
      <View style={styles.heading}>
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
        {search.data?.map((title) => (
          <View key={title.id} style={styles.searchResult}>
            <View style={styles.titleCopy}>
              <Text style={styles.itemTitle}>{title.title}</Text>
              <Text style={styles.muted}>{title.contentType === 'movie' ? 'Filme' : 'Série'}</Text>
            </View>
            <View style={styles.searchActions}>
              {statusByTitleId.get(title.id)?.isFavorite ? (
                <Text style={styles.statusLabel}>Já é favorito</Text>
              ) : null}
              <SmallButton
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
              {statusByTitleId.get(title.id)?.isInWatchlist ? (
                <Text style={styles.statusLabel}>Na watchlist</Text>
              ) : null}
              <SmallButton
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
                <SmallButton label="Adicionar à lista" onPress={() => void addSearchTitle(title, 'lists')} />
              ) : null}
            </View>
          </View>
        ))}
      </View>

      {section === 'lists' ? (
        <View style={styles.listSection}>
          <Text style={styles.sectionTitle}>Listas privadas</Text>
          {customLists.isPending ? <ActivityIndicator color={colors.brand} /> : null}
          {customLists.isError ? (
            <PrimaryButton label="Tentar novamente" onPress={() => void customLists.refetch()} variant="secondary" />
          ) : null}
          {customLists.data?.items.length === 0 && listPage === 1 ? (
            <Text style={styles.muted}>Você ainda não criou listas.</Text>
          ) : null}
          {customLists.data?.items.map((list) => (
            <View key={list.id} style={styles.listCard}>
              <Pressable
                accessibilityLabel={`Abrir lista ${list.name}`}
                accessibilityRole="button"
                accessibilityState={{ selected: list.id === activeListId }}
                onPress={() => {
                  setPage(1);
                  setSelectedListId(list.id);
                }}
                style={[styles.listSelect, list.id === activeListId ? styles.selected : null]}
              >
                <View style={styles.titleCopy}>
                  <Text style={styles.itemTitle}>{list.name}</Text>
                  <Text style={styles.muted}>{list.totalCount} títulos · privada</Text>
                </View>
              </Pressable>
              <SmallButton
                label="Editar lista"
                onPress={() => {
                  setEditingList(list);
                  setName(list.name);
                  setDescription(list.description ?? '');
                }}
              />
              <SmallButton
                label="Excluir lista"
                onPress={() => void runMutation(
                  async () => {
                    await deleteCustomList(list.id);
                    if (selectedListId === list.id) {
                      setSelectedListId(null);
                    }
                  },
                  'Lista excluída.',
                )}
              />
            </View>
          ))}
          {customLists.data ? (
            <Pager
              page={listPage}
              pageSize={customLists.data.pageSize}
              totalCount={customLists.data.totalCount}
              onChange={setListPage}
            />
          ) : null}
          <View style={styles.form}>
            <Text style={styles.sectionTitle}>{editingList ? 'Editar lista' : 'Criar lista'}</Text>
            <TextInput
              accessibilityLabel="Nome da lista"
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
              onChangeText={setDescription}
              onFocus={() => setFocusedField('description')}
              onBlur={() => setFocusedField(null)}
              placeholder="Descrição (opcional)"
              placeholderTextColor={colors.textSecondary}
              style={[
                styles.input,
                styles.descriptionInput,
                focusedField === 'description' ? styles.inputFocused : null,
              ]}
              value={description}
              multiline
            />
            {formError ? <Text style={styles.validation}>{formError}</Text> : null}
            <PrimaryButton label={editingList ? 'Salvar alterações' : 'Criar lista'} onPress={() => void saveList()} />
            {editingList ? (
              <PrimaryButton
                label="Cancelar edição"
                onPress={() => {
                  setEditingList(null);
                  setName('');
                  setDescription('');
                  setFormError(null);
                }}
                variant="quiet"
              />
            ) : null}
          </View>
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
          {displayedPage?.items.map((reference, index) => (
            <View key={reference.titleId} style={styles.titleRow}>
              {titleDetails[index]?.isPending ? (
                <ActivityIndicator color={colors.brand} />
              ) : titleDetails[index]?.data ? (
                <View style={styles.titleCopy}>
                  <Text style={styles.itemTitle}>{titleDetails[index].data.title}</Text>
                  <Text style={styles.muted}>
                    {reference.contentType === 'movie' ? 'Filme' : 'Série'}
                  </Text>
                </View>
              ) : (
                <View style={styles.titleCopy}>
                  <Text style={styles.muted}>Detalhes indisponíveis no catálogo.</Text>
                  <SmallButton
                    label="Tentar carregar detalhes"
                    onPress={() => void titleDetails[index]?.refetch()}
                  />
                </View>
              )}
              <SmallButton label="Remover" onPress={() => void removeTitle(reference)} />
            </View>
          ))}
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

      {section === 'lists' && activeListId ? (
        <View style={styles.listSection}>
          <Text style={styles.sectionTitle}>{selectedList.data?.list.name ?? 'Títulos da lista'}</Text>
          {selectedList.isPending ? <ActivityIndicator color={colors.brand} /> : null}
          {selectedList.isError ? (
            <PrimaryButton label="Tentar novamente" onPress={() => void selectedList.refetch()} variant="secondary" />
          ) : null}
          {selectedList.data?.titles.items.length === 0 ? (
            <Text style={styles.muted}>Esta lista ainda não tem títulos.</Text>
          ) : null}
          {selectedList.data?.titles.items.map((reference, index) => (
            <View key={reference.titleId} style={styles.titleRow}>
              {titleDetails[index]?.isPending ? (
                <ActivityIndicator color={colors.brand} />
              ) : titleDetails[index]?.data ? (
                <View style={styles.titleCopy}>
                  <Text style={styles.itemTitle}>{titleDetails[index].data.title}</Text>
                  <Text style={styles.muted}>
                    {reference.contentType === 'movie' ? 'Filme' : 'Série'}
                  </Text>
                </View>
              ) : (
                <View style={styles.titleCopy}>
                  <Text style={styles.muted}>Detalhes indisponíveis no catálogo.</Text>
                  <SmallButton
                    label="Tentar carregar detalhes"
                    onPress={() => void titleDetails[index]?.refetch()}
                  />
                </View>
              )}
              <SmallButton label="Remover" onPress={() => void removeTitle(reference)} />
              <View style={styles.orderActions}>
                <SmallButton
                  disabled={reference.position === 0}
                  label="Mover para cima"
                  onPress={() => void moveListTitle(reference.titleId, -1)}
                />
                <SmallButton
                  disabled={
                    reference.position !== null &&
                    reference.position === (selectedList.data?.list.totalCount ?? 0) - 1
                  }
                  label="Mover para baixo"
                  onPress={() => void moveListTitle(reference.titleId, 1)}
                />
              </View>
            </View>
          ))}
          {selectedList.data ? (
            <Pager
              page={selectedList.data.titles.page}
              pageSize={selectedList.data.titles.pageSize}
              totalCount={selectedList.data.titles.totalCount}
              onChange={setPage}
            />
          ) : null}
        </View>
      ) : null}

      <PrimaryButton label="Voltar" onPress={() => router.back()} variant="quiet" />
    </ScrollView>
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
}: {
  label: string;
  onPress: () => void;
  disabled?: boolean;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ disabled }}
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [
        styles.smallButton,
        pressed ? styles.pressed : null,
        disabled ? styles.disabled : null,
      ]}
    >
      <Text style={styles.smallButtonLabel}>{label}</Text>
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
      <SmallButton label="Página anterior" onPress={() => onChange(Math.max(1, page - 1))} />
      <Text style={styles.muted}>Página {page} de {totalPages}</Text>
      <SmallButton label="Próxima página" onPress={() => onChange(Math.min(totalPages, page + 1))} />
    </View>
  );
}

const styles = StyleSheet.create({
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
    gap: spacing.small,
  },
  orderActions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.extraSmall,
  },
  titleRow: {
    minHeight: 56,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingVertical: spacing.small,
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
  statusLabel: {
    alignSelf: 'center',
    color: colors.success,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
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
  smallButtonLabel: {
    color: colors.textPrimary,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
    textAlign: 'center',
  },
  listCard: {
    gap: spacing.small,
    borderRadius: radii.medium,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.small,
    backgroundColor: colors.surface,
  },
  listSelect: {
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
  form: {
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
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
