import { useLocalSearchParams } from 'expo-router';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { BackButton } from '@/components/navigation/back-button';
import { TabButton } from '@/components/ui/chip';
import { CatalogSearchSection } from '@/features/lists/components/catalog-search-section';
import { CollectionSection } from '@/features/lists/components/collection-section';
import { CustomListsSection } from '@/features/lists/components/custom-lists-section';
import { ListDialog } from '@/features/lists/components/list-dialog';
import { useListDialog } from '@/features/lists/hooks/use-list-dialog';
import { useListMutations } from '@/features/lists/hooks/use-list-mutations';
import { useListsScreen } from '@/features/lists/hooks/use-lists-screen';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

export default function ListsScreen() {
  const params = useLocalSearchParams<{ section?: string }>();
  const screen = useListsScreen(params.section);
  const mutations = useListMutations();
  const dialog = useListDialog();
  const { section, changeSection } = screen;

  return (
    <SafeAreaView style={styles.screen}>
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <View style={styles.heading}>
          <BackButton />
          <Text accessibilityRole="header" style={styles.title}>Seus títulos e listas</Text>
          <Text style={styles.subtitle}>Organize o que você gosta e quer assistir.</Text>
        </View>
        <View style={styles.tabs}>
          <TabButton label="Favoritos" active={section === 'favorites'} onPress={() => changeSection('favorites')} />
          <TabButton label="Watchlist" active={section === 'watchlist'} onPress={() => changeSection('watchlist')} />
          <TabButton label="Minhas listas" active={section === 'lists'} onPress={() => changeSection('lists')} />
        </View>
        {mutations.successMessage ? <Text style={styles.successMessage}>{mutations.successMessage}</Text> : null}
        <CatalogSearchSection
          mutations={mutations}
          activeListId={screen.activeListId}
          showAddToList={section === 'lists'}
        />
        {section === 'lists' ? (
          <CustomListsSection screen={screen} mutations={mutations} dialog={dialog} />
        ) : (
          <CollectionSection screen={screen} mutations={mutations} />
        )}
        <ListDialog
          dialog={dialog}
          mutations={mutations}
          onListSaved={screen.showList}
          onListDeleted={screen.resetAfterDelete}
        />
      </ScrollView>
    </SafeAreaView>
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
  successMessage: {
    color: colors.success,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
});
