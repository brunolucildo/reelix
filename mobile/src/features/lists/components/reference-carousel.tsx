import type { UseQueryResult } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { ActivityIndicator, ScrollView, Text, View } from 'react-native';

import { SmallButton } from '@/components/ui/small-button';
import type { CatalogTitle } from '@/features/catalog/api/catalog-api';
import { MediaTitleRow } from '@/features/catalog/components/media-title-row';
import type { TitleReference } from '@/features/lists/api/lists-api';
import { ListActionButton } from '@/features/lists/components/list-action-button';
import { listsStyles as styles } from '@/features/lists/components/lists-styles';
import TrashIcon from '@/assets/brand/navigation-icons/trash.svg';
import { colors } from '@/theme/tokens';

type Props = {
  references: TitleReference[];
  details: UseQueryResult<CatalogTitle>[];
  /** Changes when the visible set changes, so the carousel scrolls back to the start. */
  scrollKey: string;
  accessibilityLabel: string;
  nested?: boolean;
  onRemove: (reference: TitleReference) => void;
  /** Extra actions rendered after "Remover" (e.g. reorder buttons). */
  renderExtra?: (reference: TitleReference) => ReactNode;
};

/** Horizontal rail of saved titles with a remove action; titles without catalog details get a retry card. */
export function ReferenceCarousel({
  references,
  details,
  scrollKey,
  accessibilityLabel,
  nested = false,
  onRemove,
  renderExtra,
}: Props) {
  return (
    <ScrollView
      key={scrollKey}
      accessibilityLabel={accessibilityLabel}
      horizontal
      nestedScrollEnabled={nested ? true : undefined}
      directionalLockEnabled
      keyboardShouldPersistTaps="handled"
      contentContainerStyle={styles.horizontalCards}
    >
      {references.map((reference, index) => {
        const title = details[index]?.data;
        const remove = (
          <ListActionButton Icon={TrashIcon} label="Remover" onPress={() => onRemove(reference)} />
        );
        return title ? (
          <MediaTitleRow key={reference.titleId} title={title} layout="poster">
            {remove}
            {renderExtra?.(reference)}
          </MediaTitleRow>
        ) : (
          <View key={reference.titleId} style={styles.unavailableCard}>
            {details[index]?.isPending ? (
              <ActivityIndicator color={colors.brand} />
            ) : (
              <View style={styles.titleCopy}>
                <Text style={styles.muted}>Detalhes indisponíveis no catálogo.</Text>
                <SmallButton
                  label="Tentar carregar detalhes"
                  onPress={() => void details[index]?.refetch()}
                />
              </View>
            )}
            {remove}
          </View>
        );
      })}
    </ScrollView>
  );
}
