import { StyleSheet, Text } from 'react-native';

import { colors, fontFamily } from '@/theme/tokens';

type FormMessageProps = {
  message: string;
  tone: 'error' | 'success';
};

export function FormMessage({ message, tone }: FormMessageProps) {
  return (
    <Text
      accessibilityLiveRegion="polite"
      accessibilityRole={tone === 'error' ? 'alert' : 'text'}
      style={[styles.message, tone === 'error' ? styles.error : styles.success]}
    >
      {message}
    </Text>
  );
}

const styles = StyleSheet.create({
  message: {
    fontSize: 14,
    fontFamily: fontFamily.regular,
    lineHeight: 21,
  },
  error: {
    color: colors.error,
  },
  success: {
    color: colors.success,
  },
});
