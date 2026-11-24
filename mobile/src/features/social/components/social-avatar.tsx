import { Image } from 'expo-image';
import { StyleSheet, Text, View } from 'react-native';

import { getInitials } from '@/lib/format';
import { colors, dimensions, fontFamily, radii, typography } from '@/theme/tokens';

/** 44 px avatar for people lists: the photo when there is one, otherwise the initial. */
export function SocialAvatar({ name, uri }: { name: string; uri?: string | null }) {
  return uri ? (
    <Image accessibilityLabel={`Avatar de ${name}`} contentFit="cover" source={{ uri }} style={styles.avatar} />
  ) : (
    <View accessibilityLabel={`Avatar de ${name}`} style={styles.fallback}>
      <Text style={styles.initial}>{getInitials(name)}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  avatar: {
    width: dimensions.minimumTouchTarget,
    height: dimensions.minimumTouchTarget,
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
  },
  fallback: {
    width: dimensions.minimumTouchTarget,
    height: dimensions.minimumTouchTarget,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
  },
  initial: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
});
