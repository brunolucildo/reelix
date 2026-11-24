import { Pressable, StyleSheet, Text, View } from 'react-native';

import { LoadingState } from '@/components/ui/loading-state';
import { SmallButton } from '@/components/ui/small-button';
import { getTotalPages } from '@/lib/pagination';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  page: number;
  pageSize: number;
  totalCount: number;
  onChange: (page: number) => void;
  /** `pages`: "Página 1 de 3" between the arrows. `count`: "Página 1 · 12 registros" above them. */
  variant?: 'pages' | 'count';
  /** `count` only: names the list in the accessibility labels ("Página anterior de histórico"). */
  label?: string;
  /** `count` only: plural noun shown after the total. */
  unit?: string;
  /** `count` only: disables the arrows and shows "Atualizando..." while a page loads. */
  isFetching?: boolean;
};

export function Pager({
  page,
  pageSize,
  totalCount,
  onChange,
  variant = 'pages',
  label = '',
  unit = '',
  isFetching = false,
}: Props) {
  const totalPages = getTotalPages(totalCount, pageSize);
  const hasPrevious = page > 1;
  const hasNext = page < totalPages;

  if (variant === 'pages') {
    if (totalPages < 2) return null;
    return (
      <View style={styles.pages}>
        <SmallButton
          accessibilityLabel="Página anterior"
          disabled={!hasPrevious}
          iconOnly
          label="←"
          onPress={() => onChange(Math.max(1, page - 1))}
        />
        <Text style={styles.muted}>Página {page} de {totalPages}</Text>
        <SmallButton
          accessibilityLabel="Próxima página"
          disabled={!hasNext}
          iconOnly
          label="→"
          onPress={() => onChange(Math.min(totalPages, page + 1))}
        />
      </View>
    );
  }

  if (totalCount === 0 && page === 1) return null;
  const previousDisabled = !hasPrevious || isFetching;
  const nextDisabled = !hasNext || isFetching;

  return (
    <View style={styles.count}>
      <Text accessibilityLiveRegion="polite" style={styles.countSummary}>
        Página {page} · {totalCount} {unit}
      </Text>
      <View style={styles.countControls}>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={`Página anterior de ${label}`}
          accessibilityState={{ disabled: previousDisabled }}
          disabled={previousDisabled}
          onPress={() => onChange(page - 1)}
          style={[styles.arrow, previousDisabled && styles.arrowDisabled]}
        >
          <Text style={styles.arrowText}>←</Text>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={`Próxima página de ${label}`}
          accessibilityState={{ disabled: nextDisabled }}
          disabled={nextDisabled}
          onPress={() => onChange(page + 1)}
          style={[styles.arrow, nextDisabled && styles.arrowDisabled]}
        >
          <Text style={styles.arrowText}>→</Text>
        </Pressable>
      </View>
      {isFetching ? <LoadingState label="Atualizando..." /> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  pages: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
  },
  muted: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  count: {
    gap: spacing.small,
  },
  countSummary: {
    color: colors.textSecondary,
    fontFamily: fontFamily.regular,
    fontSize: typography.body,
  },
  countControls: {
    flexDirection: 'row',
    gap: spacing.small,
  },
  arrow: {
    width: 44,
    height: 44,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.small,
    backgroundColor: colors.surfaceElevated,
  },
  arrowDisabled: { opacity: 0.45 },
  arrowText: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
});
