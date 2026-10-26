import { z } from 'zod';

import { supabase } from '@/services/supabase';

const apiUrl = process.env.EXPO_PUBLIC_API_URL?.trim();

if (!apiUrl) {
  throw new Error('Configure EXPO_PUBLIC_API_URL in mobile/.env before starting the app.');
}

const parsedApiUrl = new URL(apiUrl);
if (
  parsedApiUrl.protocol !== 'https:' &&
  !(parsedApiUrl.protocol === 'http:' && ['localhost', '127.0.0.1', '10.0.2.2'].includes(parsedApiUrl.hostname))
) {
  throw new Error('EXPO_PUBLIC_API_URL must use HTTPS except for a local development server.');
}

const accountSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email().nullable(),
  createdAt: z.string().datetime({ offset: true }),
});

export type AuthenticatedAccount = z.infer<typeof accountSchema>;

export async function fetchAuthenticatedAccount(): Promise<AuthenticatedAccount> {
  const { data, error } = await supabase.auth.getSession();
  if (error) {
    throw error;
  }

  if (!data.session) {
    throw new Error('A sessão autenticada não está disponível.');
  }

  const response = await fetch(new URL('/api/v1/auth/me', apiUrl), {
    headers: {
      Accept: 'application/json',
      Authorization: `Bearer ${data.session.access_token}`,
    },
  });

  if (!response.ok) {
    const error = new Error(
      response.status === 401
        ? 'A API não aceitou a sessão autenticada.'
        : 'Não foi possível consultar a conta autenticada.',
    );
    Object.assign(error, { status: response.status });
    throw error;
  }

  const result = accountSchema.safeParse(await response.json());
  if (!result.success) {
    throw new Error('A API retornou uma resposta de conta inválida.');
  }

  return result.data;
}
