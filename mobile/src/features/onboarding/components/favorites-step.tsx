import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';

import { onboardingStyles as shared } from '@/features/onboarding/components/onboarding-styles';
import { TitleResult } from '@/features/onboarding/components/title-result';
import { MAX_FAVORITES } from '@/features/onboarding/schemas';
import type { OnboardingFlowState } from '@/features/onboarding/hooks/use-onboarding-flow';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Steps "favoriteMovies" and "favoriteSeries": search the catalog and pick up to 10 titles (optional). */
export function FavoritesStep({ flow }: { flow: OnboardingFlowState }) {
  const [focused, setFocused] = useState(false);
  const { favoriteType, search, searchText, submittedSearch, favoriteIds, isBusy } = flow;
  const canSearch = searchText.trim().length >= 2 && !search.isFetching;

  return (
    <View style={shared.section}>
      <Text style={shared.eyebrow}>{favoriteType === 'movie' ? 'FILMES' : 'SÉRIES'}</Text>
      <Text accessibilityRole="header" style={shared.title}>
        {favoriteType === 'movie' ? 'Quais filmes você ama?' : 'Quais séries você acompanha?'}
      </Text>
      <Text style={shared.secondaryText}>Escolha até {MAX_FAVORITES}. Esta etapa é opcional.</Text>
      <View style={styles.searchRow}>
        <TextInput
          accessibilityLabel={`Buscar ${favoriteType === 'movie' ? 'filmes' : 'séries'}`}
          autoCapitalize="sentences"
          onBlur={() => setFocused(false)}
          onChangeText={flow.setSearchText}
          onFocus={() => setFocused(true)}
          onSubmitEditing={flow.submitSearch}
          placeholder="Buscar no catálogo"
          placeholderTextColor={colors.textSecondary}
          returnKeyType="search"
          style={[styles.searchInput, focused ? styles.searchInputFocused : null]}
          value={searchText}
        />
        <Pressable
          accessibilityRole="button"
          disabled={!canSearch}
          onPress={flow.submitSearch}
          style={({ pressed }) => [
            styles.searchButton,
            !canSearch ? styles.disabled : null,
            pressed && canSearch ? shared.pressed : null,
          ]}
        >
          <Text style={styles.searchButtonLabel}>Buscar</Text>
        </Pressable>
      </View>
      {search.isFetching ? (
        <View accessibilityLiveRegion="polite" style={styles.loadingRow}>
          <ActivityIndicator color={colors.brand} />
          <Text style={shared.emptyText}>Buscando no catálogo...</Text>
        </View>
      ) : null}
      {submittedSearch.trim().length < 2 ? (
        <Text style={shared.emptyText}>Digite ao menos 2 caracteres para buscar no catálogo.</Text>
      ) : null}
      {search.data?.length === 0 && !search.isFetching ? (
        <Text style={shared.emptyText}>Nenhum título encontrado.</Text>
      ) : null}
      <View style={styles.titleList}>
        {search.data?.map((title) => (
          <TitleResult
            key={title.id}
            title={title}
            selected={favoriteIds.includes(title.id)}
            onPress={() => flow.toggleFavorite(title)}
          />
        ))}
      </View>
      {favoriteIds.length > 0 ? (
        <>
          <Text style={shared.selectedCount}>{favoriteIds.length} de {MAX_FAVORITES} selecionados</Text>
          <Pressable
            accessibilityRole="button"
            disabled={isBusy}
            onPress={flow.clearFavorites}
            style={({ pressed }) => [shared.skipButton, pressed ? shared.pressed : null]}
          >
            <Text style={shared.skipLabel}>Remover seleções desta etapa</Text>
          </Pressable>
        </>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  searchRow: {
    flexDirection: 'row',
    gap: spacing.small,
  },
  searchInput: {
    flex: 1,
    minHeight: 52,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  searchInputFocused: {
    borderColor: colors.brandSecondary,
  },
  searchButton: {
    minHeight: 52,
    minWidth: 84,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.small,
    paddingHorizontal: 12,
    backgroundColor: colors.brand,
  },
  disabled: {
    opacity: 0.5,
  },
  searchButtonLabel: {
    color: colors.white,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  loadingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
  },
  titleList: {
    gap: spacing.small,
  },
});
