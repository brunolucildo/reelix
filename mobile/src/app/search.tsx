import { useQuery } from '@tanstack/react-query';
import { Redirect, useRouter } from 'expo-router';
import { useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

import { TitlePosterCard } from '@/components/discovery/title-poster-card';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast } from '@/providers/toast-provider';
import { searchCatalog } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type ContentType = 'movie' | 'series';

export default function CatalogSearchScreen() {
  const router = useRouter();
  const { session } = useAuthSession();
  const [type, setType] = useState<ContentType>('movie');
  const [searchText, setSearchText] = useState('');
  const [submittedQuery, setSubmittedQuery] = useState('');
  const [validationMessage, setValidationMessage] = useState<string | null>(null);

  const results = useQuery({
    queryKey: ['catalog', 'search', type, submittedQuery],
    queryFn: ({ signal }) => searchCatalog(submittedQuery, type, signal),
    enabled: submittedQuery.length > 0,
    staleTime: 60_000,
  });
  useErrorToast(results.error, 'Não foi possível pesquisar no catálogo. Tente novamente.');

  if (!session) {
    return <Redirect href="/" />;
  }

  const submitSearch = () => {
    const query = searchText.trim();
    if (query.length === 0 || query.length > 200) {
      setValidationMessage('Digite um termo de até 200 caracteres.');
      return;
    }

    setValidationMessage(null);
    setSubmittedQuery(query);
  };

  return (
    <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <Pressable
        accessibilityRole="button"
        onPress={() => router.back()}
        style={styles.backButton}
      >
        <Text style={styles.backText}>Voltar</Text>
      </Pressable>
      <Text accessibilityRole="header" style={styles.heading}>Pesquisar catálogo</Text>

      <View accessibilityRole="tablist" style={styles.tabs}>
        {(['movie', 'series'] as const).map((contentType) => {
          const selected = type === contentType;
          return (
            <Pressable
              key={contentType}
              accessibilityRole="tab"
              accessibilityState={{ selected }}
              onPress={() => {
                setType(contentType);
                setSubmittedQuery('');
              }}
              style={[styles.tab, selected && styles.selectedTab]}
            >
              <Text style={[styles.tabText, selected && styles.selectedTabText]}>
                {contentType === 'movie' ? 'Filmes' : 'Séries'}
              </Text>
            </Pressable>
          );
        })}
      </View>

      <View style={styles.searchRow}>
        <TextInput
          accessibilityLabel="Nome do filme ou série"
          onChangeText={setSearchText}
          onSubmitEditing={submitSearch}
          placeholder="Título para pesquisar"
          placeholderTextColor={colors.textSecondary}
          returnKeyType="search"
          style={styles.input}
          value={searchText}
        />
        <Pressable
          accessibilityRole="button"
          onPress={submitSearch}
          style={styles.submitButton}
        >
          <Text style={styles.submitText}>Buscar</Text>
        </Pressable>
      </View>
      {validationMessage ? <Text style={styles.validation}>{validationMessage}</Text> : null}

      {submittedQuery.length === 0 ? (
        <Text style={styles.status}>Pesquise filmes ou séries do catálogo.</Text>
      ) : results.isPending ? (
        <View style={styles.statusRow}>
          <ActivityIndicator color={colors.brand} />
          <Text style={styles.status}>Buscando títulos...</Text>
        </View>
      ) : results.isError ? (
        <View style={styles.statusRow}>
          <Text style={styles.status}>A pesquisa não está disponível agora.</Text>
          <Pressable
            accessibilityRole="button"
            onPress={() => void results.refetch()}
            style={styles.retry}
          >
            <Text style={styles.retryText}>Tentar novamente</Text>
          </Pressable>
        </View>
      ) : results.data.length === 0 ? (
        <Text style={styles.status}>Nenhum título encontrado para essa pesquisa.</Text>
      ) : (
        <ScrollView
          contentContainerStyle={styles.results}
          horizontal
          showsHorizontalScrollIndicator={false}
        >
          {results.data.map((title) => <TitlePosterCard key={title.id} title={title} />)}
        </ScrollView>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    gap: spacing.medium,
    padding: spacing.medium,
    paddingTop: spacing.large,
    backgroundColor: colors.background,
  },
  backButton: {
    minHeight: dimensions.minimumTouchTarget,
    alignSelf: 'flex-start',
    justifyContent: 'center',
    paddingHorizontal: spacing.small,
  },
  backText: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  heading: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.bold,
  },
  tabs: {
    flexDirection: 'row',
    gap: spacing.small,
  },
  tab: {
    minHeight: dimensions.minimumTouchTarget,
    minWidth: dimensions.posterWidth,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    backgroundColor: colors.surface,
  },
  selectedTab: {
    backgroundColor: colors.brand,
  },
  tabText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  selectedTabText: {
    color: colors.background,
  },
  searchRow: {
    flexDirection: 'row',
    gap: spacing.small,
  },
  input: {
    flex: 1,
    minHeight: dimensions.minimumTouchTarget,
    paddingHorizontal: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  submitButton: {
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.brand,
  },
  submitText: {
    color: colors.background,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  validation: {
    color: colors.error,
    fontSize: typography.caption,
    fontFamily: fontFamily.medium,
  },
  statusRow: {
    alignItems: 'flex-start',
    gap: spacing.small,
  },
  status: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  retry: {
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.small,
  },
  retryText: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  results: {
    gap: spacing.medium,
    paddingEnd: spacing.medium,
    paddingTop: spacing.small,
  },
});
