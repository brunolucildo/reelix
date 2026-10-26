import * as Linking from 'expo-linking';

export type AuthRedirectFlow = 'signup' | 'recovery';

export function createAuthRedirectUrl(flow: AuthRedirectFlow): string {
  return Linking.createURL('/auth/callback', {
    queryParams: { flow },
  });
}
