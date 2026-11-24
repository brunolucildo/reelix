import { StyleSheet, View } from 'react-native';

import { IconAction } from '@/components/ui/icon-action';
import GlobeIcon from '@/assets/brand/navigation-icons/globe.svg';
import LockIcon from '@/assets/brand/navigation-icons/lock.svg';
import type { ReviewAudience } from '@/features/ratings/api/ratings-api';
import { dimensions, spacing } from '@/theme/tokens';

type Props = {
  audience: ReviewAudience;
  onChange: (audience: ReviewAudience) => void;
  disabled?: boolean;
};

/** Private / public choice for a rating or comment. */
export function AudienceSelector({ audience, onChange, disabled = false }: Props) {
  return (
    <View accessibilityRole="radiogroup" style={styles.row}>
      <IconAction radio label="Privada: somente você" selected={audience === 'private'} disabled={disabled} onPress={() => onChange('private')}><LockIcon width={dimensions.iconSize} height={dimensions.iconSize} /></IconAction>
      <IconAction radio label="Pública: visível para outras pessoas" selected={audience === 'public'} disabled={disabled} onPress={() => onChange('public')}><GlobeIcon width={dimensions.iconSize} height={dimensions.iconSize} /></IconAction>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    gap: spacing.small,
  },
});
