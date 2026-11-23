import { z } from 'zod';

import { httpRequest } from '@/lib/http-client';

const accountSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email().nullable(),
  createdAt: z.string().datetime({ offset: true }),
});

export type AuthenticatedAccount = z.infer<typeof accountSchema>;

export async function fetchAuthenticatedAccount(): Promise<AuthenticatedAccount> {
  return httpRequest('/api/v1/auth/me', accountSchema);
}
