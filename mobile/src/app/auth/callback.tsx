import { useEffect, useState } from 'react';
import { ActivityIndicator } from 'react-native';
import * as Linking from 'expo-linking';
import { router, useLocalSearchParams } from 'expo-router';

import { AuthScreen } from '@/components/auth/auth-screen';
import { AuthLink } from '@/components/auth/auth-link';
import { FormMessage } from '@/components/auth/form-message';
import { supabase } from '@/services/supabase';
import { colors } from '@/theme/tokens';

function getParameter(url: URL | null, name: string): string | null {
  if (!url) {
    return null;
  }

  const queryValue = url.searchParams.get(name);
  if (queryValue) {
    return queryValue;
  }

  return new URLSearchParams(url.hash.replace(/^#/, '')).get(name);
}

function getEmailOtpType(
  value: string | null,
): 'signup' | 'invite' | 'magiclink' | 'recovery' | 'email_change' | 'email' | null {
  switch (value) {
    case 'signup':
    case 'invite':
    case 'magiclink':
    case 'recovery':
    case 'email_change':
    case 'email':
      return value;
    default:
      return null;
  }
}

export default function AuthCallbackScreen() {
  const params = useLocalSearchParams<{
    code?: string | string[];
    flow?: string | string[];
    token_hash?: string | string[];
    type?: string | string[];
    access_token?: string | string[];
    refresh_token?: string | string[];
    error?: string | string[];
  }>();
  const incomingUrl = Linking.useLinkingURL();
  const [error, setError] = useState<string | null>(null);
  const first = (value: string | string[] | undefined) =>
    Array.isArray(value) ? value[0] : value;

  useEffect(() => {
    let active = true;

    const completeAuthentication = async () => {
      try {
        const url = incomingUrl ? new URL(incomingUrl) : null;
        const code = getParameter(url, 'code') ?? first(params.code);
        const tokenHash = getParameter(url, 'token_hash') ?? first(params.token_hash);
        const otpType = getEmailOtpType(getParameter(url, 'type') ?? first(params.type) ?? null);
        const accessToken = getParameter(url, 'access_token') ?? first(params.access_token);
        const refreshToken = getParameter(url, 'refresh_token') ?? first(params.refresh_token);
        const flow = getParameter(url, 'flow') ?? first(params.flow);
        const linkError = getParameter(url, 'error') ?? first(params.error);

        if (linkError) {
          throw new Error('The authentication link is invalid or expired.');
        }

        if (code) {
          const { error: exchangeError } = await supabase.auth.exchangeCodeForSession(code);
          if (exchangeError) {
            throw exchangeError;
          }
        } else if (tokenHash && otpType) {
          const { error: verifyError } = await supabase.auth.verifyOtp({
            token_hash: tokenHash,
            type: otpType,
          });
          if (verifyError) {
            throw verifyError;
          }
        } else if (accessToken && refreshToken) {
          const { error: sessionError } = await supabase.auth.setSession({
            access_token: accessToken,
            refresh_token: refreshToken,
          });
          if (sessionError) {
            throw sessionError;
          }
        } else {
          throw new Error('The authentication link does not contain a supported credential.');
        }

        if (active) {
          router.replace(flow === 'recovery' || otpType === 'recovery' ? '/auth/reset-password' : '/');
        }
      } catch {
        if (active) {
          setError('Este link de autenticação é inválido ou expirou. Solicite um novo link e tente novamente.');
        }
      }
    };

    void completeAuthentication();

    return () => {
      active = false;
    };
  }, [incomingUrl, params.access_token, params.code, params.error, params.flow, params.refresh_token, params.token_hash, params.type]);

  return (
    <AuthScreen title="Confirmando seu acesso." subtitle="Estamos validando o link com segurança.">
      {error ? (
        <>
          <FormMessage message={error} tone="error" />
          <AuthLink href="/" label="Voltar para entrar" />
        </>
      ) : (
        <ActivityIndicator accessibilityLabel="Validando link de autenticação" color={colors.brand} size="large" />
      )}
    </AuthScreen>
  );
}
