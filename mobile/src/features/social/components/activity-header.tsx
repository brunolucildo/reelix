import { Pressable, StyleSheet, Text, View } from 'react-native';

import FriendsIcon from '@/assets/brand/navigation-icons/friends.svg';
import { PrimaryButton } from '@/components/ui/primary-button';
import { socialStyles as styles } from '@/features/social/components/social-styles';
import type { ActivityFilter } from '@/features/social/hooks/use-social-screen';
import { dimensions, spacing } from '@/theme/tokens';

const filters = [
  { key: 'all', label: 'Todas' },
  { key: 'watched', label: 'Assistidos' },
  { key: 'ratings', label: 'Avaliações' },
] as const;

type Props = {
  tab: 'feed' | 'activities';
  filter: ActivityFilter;
  onFilterChange: (filter: ActivityFilter) => void;
  /** Number of activities loaded, before the filter. */
  loadedCount: number;
  /** Number shown after the filter. */
  visibleCount: number;
  isEmptyState: boolean;
  onFindPeople: () => void;
};

/** Heading, filter chips and empty state above the activity list of the "Feed" and "Atividades" tabs. */
export function ActivityHeader({ tab, filter, onFilterChange, loadedCount, visibleCount, isEmptyState, onFindPeople }: Props) {
  return (
    <View style={styles.heading}>
      <Text style={styles.sectionTitle}>{tab === 'feed' ? 'Na sua rede' : 'Histórico de atividades'}</Text>
      {tab === 'activities' ? <View accessibilityRole="tablist" style={local.filterRow}>
        {filters.map(({ key, label }) => (
          <Pressable key={key} accessibilityRole="tab" accessibilityState={{ selected: filter === key }} onPress={() => onFilterChange(key)} style={[styles.actionButton, filter === key && styles.navigationTabActive]}><Text style={styles.actionText}>{label}</Text></Pressable>
        ))}
      </View> : null}
      {visibleCount === 0 && isEmptyState ? <View style={styles.emptyState}>
        <FriendsIcon width={dimensions.avatarSize} height={dimensions.avatarSize} />
        <Text style={styles.sectionTitle}>{loadedCount ? 'Nenhuma atividade deste tipo' : 'Sua próxima descoberta vem de uma amizade'}</Text>
        <Text style={styles.secondaryText}>{loadedCount ? 'Não há atividades deste tipo entre as carregadas.' : 'As atividades que seus amigos compartilharem com você aparecerão aqui.'}</Text>
        {!loadedCount ? <PrimaryButton label="Encontrar pessoas" onPress={onFindPeople} /> : null}
      </View> : null}
    </View>
  );
}

const local = StyleSheet.create({
  filterRow: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.small },
});
