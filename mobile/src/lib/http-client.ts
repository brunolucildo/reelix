import type { z } from 'zod';

import { ApiContractError, ApiError, ApiNetworkError } from '@/lib/api-error';
import { apiUrl } from '@/services/api-url';
import { supabase } from '@/services/supabase';

export type ApiRequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  /** Serialized as JSON; omit it for requests without a body. */
  body?: unknown;
  signal?: AbortSignal;
};

type ProblemDetails = { title?: unknown; detail?: unknown };

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError';
}

async function readProblem(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = {};
  try {
    problem = (await response.json()) as ProblemDetails;
  } catch {
    // The response can be empty or non-JSON; ApiError falls back to a safe message.
  }
  return new ApiError(
    response.status,
    typeof problem?.title === 'string' ? problem.title : null,
    typeof problem?.detail === 'string' ? problem.detail : null,
  );
}

/**
 * The only function that talks to the Reelix API: attaches the Supabase bearer token,
 * parses ProblemDetails into ApiError and validates every successful body with zod.
 */
export async function httpRequest<T>(
  path: string,
  schema: z.ZodType<T>,
  options: ApiRequestOptions = {},
): Promise<T> {
  const { data, error } = await supabase.auth.getSession();
  if (error) {
    throw error;
  }
  if (!data.session) {
    throw new ApiError(401);
  }

  const hasBody = options.body !== undefined;
  let response: Response;
  try {
    response = await fetch(new URL(path, apiUrl), {
      method: options.method ?? 'GET',
      signal: options.signal,
      headers: {
        Accept: 'application/json',
        ...(hasBody ? { 'Content-Type': 'application/json' } : {}),
        Authorization: `Bearer ${data.session.access_token}`,
      },
      ...(hasBody ? { body: JSON.stringify(options.body) } : {}),
    });
  } catch (fetchError) {
    throw isAbortError(fetchError) ? fetchError : new ApiNetworkError();
  }

  if (!response.ok) {
    throw await readProblem(response);
  }

  let payload: unknown;
  if (response.status !== 204) {
    try {
      payload = await response.json();
    } catch {
      throw new ApiContractError();
    }
  }

  const result = schema.safeParse(payload);
  if (!result.success) {
    throw new ApiContractError();
  }
  return result.data;
}
