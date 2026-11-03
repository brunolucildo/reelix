import { useRouter } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';

import { IconAction } from '@/components/icon-action';
import { useAuthSession } from '@/providers/auth-provider';
import { colors, dimensions, fontFamily, spacing, typography } from '@/theme/tokens';
import HomeIcon from '../../../assets/brand/navigation-icons/home.svg';

type Props = {
  onPress?: () => void;
};

export function BackButton({ onPress }: Props) {
  const router = useRouter();
  const { session } = useAuthSession();

  const goBack = () => {
    if (onPress) {
      onPress();
    } else if (router.canGoBack()) {
      router.back();
    } else {
      router.replace(session ? '/home' : '/');
    }
  };

  return (
    <View style={styles.navigation}>
      <IconAction label="Voltar" onPress={goBack}><Text style={styles.icon}>←</Text></IconAction>
      <IconAction label="Ir para Home" onPress={() => { if (session) router.dismissTo('/home'); else router.replace('/'); }}><HomeIcon width={dimensions.iconSize} height={dimensions.iconSize} /></IconAction>
    </View>
  );
}

const styles = StyleSheet.create({
  navigation: { flexDirection: 'row', alignItems: 'center', gap: spacing.small, alignSelf: 'flex-start', zIndex: 1 },
  icon: {
    color: colors.textPrimary,
    fontSize: typography.title,
    lineHeight: typography.title + 4,
    fontFamily: fontFamily.medium,
  },
});
