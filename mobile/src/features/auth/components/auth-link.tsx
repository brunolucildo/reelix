import { Link } from 'expo-router';
import { StyleSheet } from 'react-native';

import { colors, fontFamily, typography } from '@/theme/tokens';

type AuthLinkProps = {
  href: '/' | '/sign-up' | '/forgot-password';
  label: string;
  align?: 'left' | 'center' | 'right';
};

export function AuthLink({ href, label, align = 'center' }: AuthLinkProps) {
  return (
    <Link accessibilityRole="link" href={href} style={[styles.link, { textAlign: align }]}>
      {label}
    </Link>
  );
}

const styles = StyleSheet.create({
  link: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
    minHeight: 44,
    justifyContent: 'center',
    paddingVertical: 10,
  },
});
