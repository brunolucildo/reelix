import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, Text } from 'react-native';

import { colors, dimensions, fontFamily, typography } from '@/theme/tokens';

type Props = {
  onPress?: () => void;
};

export function BackButton({ onPress }: Props) {
  const router = useRouter();

  const goBack = () => {
    if (onPress) {
      onPress();
    } else if (router.canGoBack()) {
      router.back();
    } else {
      router.replace('/home');
    }
  };

  return (
    <Pressable
      accessibilityLabel="Voltar"
      accessibilityRole="button"
      onPress={goBack}
      style={({ pressed }) => [styles.button, pressed && styles.pressed]}
    >
      <Text style={styles.icon}>←</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: {
    width: dimensions.minimumTouchTarget,
    height: dimensions.minimumTouchTarget,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: dimensions.minimumTouchTarget / 2,
    backgroundColor: colors.surface,
  },
  icon: {
    color: colors.textPrimary,
    fontSize: typography.title,
    lineHeight: typography.title + 4,
    fontFamily: fontFamily.medium,
  },
  pressed: { opacity: 0.7 },
});
