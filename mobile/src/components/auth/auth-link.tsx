import { Link } from 'expo-router';
import { StyleSheet } from 'react-native';

import { colors, fontFamily } from '@/theme/tokens';

type AuthLinkProps = {
  href: '/' | '/sign-up' | '/forgot-password';
  label: string;
};

export function AuthLink({ href, label }: AuthLinkProps) {
  return (
    <Link accessibilityRole="link" href={href} style={styles.link}>
      {label}
    </Link>
  );
}

const styles = StyleSheet.create({
  link: {
    color: colors.brand,
    fontSize: 14,
    fontFamily: fontFamily.bold,
    minHeight: 44,
    justifyContent: 'center',
    paddingVertical: 12,
    textAlign: 'center',
  },
});
