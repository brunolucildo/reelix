import { z } from 'zod';

import { catalogTitleSchema } from '@/services/api-client';
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

const titleSchema = catalogTitleSchema;

const titleReferenceSchema = z.object({
  titleId: z.string().uuid(),
  contentType: z.enum(['movie', 'series']),
  provider: z.enum(['tmdb', 'omdb']),
  externalId: z.string().min(1),
  addedAt: z.string().datetime({ offset: true }).nullable(),
  position: z.number().int().nonnegative().nullable(),
});

const pagedTitleListSchema = z.object({
  items: z.array(titleReferenceSchema),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
});

const customListSummarySchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  description: z.string().nullable(),
  createdAt: z.string().datetime({ offset: true }),
  updatedAt: z.string().datetime({ offset: true }),
  totalCount: z.number().int().nonnegative(),
});

const pagedCustomListsSchema = z.object({
  items: z.array(customListSummarySchema),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
});

const customListSchema = z.object({
  list: customListSummarySchema,
  titles: pagedTitleListSchema,
});

const mutationSchema = z.object({ added: z.boolean() });
const titleStatusSchema = z.object({
  titleId: z.string().uuid(),
  isFavorite: z.boolean(),
  isInWatchlist: z.boolean(),
});

export type CatalogTitle = z.infer<typeof titleSchema>;
export type TitleReference = z.infer<typeof titleReferenceSchema>;
export type CustomListSummary = z.infer<typeof customListSummarySchema>;
export type PagedTitleList = z.infer<typeof pagedTitleListSchema>;
export type CustomListData = z.infer<typeof customListSchema>;
export type TitleRelationStatus = z.infer<typeof titleStatusSchema>;

async function request<T>(
  path: string,
  schema: z.ZodType<T>,
  init?: RequestInit,
): Promise<T> {
  const { data, error } = await supabase.auth.getSession();
  if (error) {
    throw error;
  }
  if (!data.session) {
    throw new Error('Sua sessão expirou. Entre novamente para continuar.');
  }

  const response = await fetch(new URL(path, apiUrl), {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      Authorization: `Bearer ${data.session.access_token}`,
      ...init?.headers,
    },
  });
  if (!response.ok) {
    let message = response.status === 401
      ? 'Sua sessão expirou. Entre novamente para continuar.'
      : 'Não foi possível concluir a solicitação. Tente novamente.';
    try {
      const problem = await response.json() as { detail?: string; title?: string };
      message = problem.detail ?? problem.title ?? message;
    } catch {
      // Keep the generic message when the server does not return ProblemDetails.
    }
    throw Object.assign(new Error(message), { status: response.status });
  }
  if (response.status === 204) {
    return undefined as T;
  }

  const result = schema.safeParse(await response.json());
  if (!result.success) {
    throw new Error('A API retornou dados em formato inesperado.');
  }
  return result.data;
}

function pageQuery(page: number, pageSize = 20) {
  return `page=${page}&pageSize=${pageSize}`;
}

export function fetchFavorites(page: number, pageSize = 20): Promise<PagedTitleList> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return request(`/api/v1/favorites?${query}`, pagedTitleListSchema);
}

export function fetchWatchlist(page: number, oldestFirst: boolean): Promise<PagedTitleList> {
  const search = new URLSearchParams({
    page: String(page),
    pageSize: '20',
    oldestFirst: String(oldestFirst),
  });
  return request(`/api/v1/watchlist?${search}`, pagedTitleListSchema);
}

export function fetchCustomLists(page: number): Promise<z.infer<typeof pagedCustomListsSchema>> {
  return request(`/api/v1/lists?${pageQuery(page)}`, pagedCustomListsSchema);
}

export function fetchCustomList(
  listId: string,
  page: number,
  pageSize = 20,
): Promise<CustomListData> {
  return request(`/api/v1/lists/${listId}?${pageQuery(page, pageSize)}`, customListSchema);
}

export function searchCatalogTitles(query: string): Promise<CatalogTitle[]> {
  const search = new URLSearchParams({
    query,
    provider: 'tmdb',
    language: 'pt-BR',
    page: '1',
  });
  return request(`/api/v1/catalog/search?${search}`, z.object({ items: z.array(titleSchema) }))
    .then((result) => result.items);
}

export function fetchTitleStatuses(titleIds: string[]): Promise<TitleRelationStatus[]> {
  return request('/api/v1/title-status', z.array(titleStatusSchema), {
    method: 'POST',
    body: JSON.stringify({ titleIds }),
  });
}

export function fetchTitleDetails(
  reference: TitleReference,
  signal?: AbortSignal,
): Promise<CatalogTitle> {
  return request(
    `/api/v1/catalog/${reference.provider}/${reference.contentType}/${reference.externalId}?language=pt-BR`,
    titleSchema,
    { signal },
  );
}

export function setFavorite(title: CatalogTitle): Promise<{ added: boolean }> {
  return request(`/api/v1/favorites/${title.id}`, mutationSchema, {
    method: 'PUT',
    body: JSON.stringify({ contentType: title.contentType }),
  });
}

export function removeFavorite(titleId: string): Promise<void> {
  return request(`/api/v1/favorites/${titleId}`, z.void(), { method: 'DELETE' });
}

export function setWatchlistTitle(title: CatalogTitle): Promise<{ added: boolean }> {
  return request(`/api/v1/watchlist/${title.id}`, mutationSchema, {
    method: 'PUT',
    body: JSON.stringify({ contentType: title.contentType }),
  });
}

export function removeWatchlistTitle(titleId: string): Promise<void> {
  return request(`/api/v1/watchlist/${titleId}`, z.void(), { method: 'DELETE' });
}

export function createCustomList(name: string, description: string): Promise<CustomListSummary> {
  return request('/api/v1/lists', customListSummarySchema, {
    method: 'POST',
    body: JSON.stringify({ name, description }),
  });
}

export function updateCustomList(
  listId: string,
  name: string,
  description: string,
): Promise<CustomListSummary> {
  return request(`/api/v1/lists/${listId}`, customListSummarySchema, {
    method: 'PUT',
    body: JSON.stringify({ name, description }),
  });
}

export function deleteCustomList(listId: string): Promise<void> {
  return request(`/api/v1/lists/${listId}`, z.void(), { method: 'DELETE' });
}

export function setCustomListTitle(
  listId: string,
  title: CatalogTitle,
): Promise<{ added: boolean }> {
  return request(`/api/v1/lists/${listId}/titles/${title.id}`, mutationSchema, {
    method: 'PUT',
    body: JSON.stringify({ contentType: title.contentType }),
  });
}

export function removeCustomListTitle(listId: string, titleId: string): Promise<void> {
  return request(`/api/v1/lists/${listId}/titles/${titleId}`, z.void(), { method: 'DELETE' });
}

export async function moveCustomListTitle(
  listId: string,
  titleId: string,
  offset: -1 | 1,
): Promise<void> {
  const firstPage = await fetchCustomList(listId, 1, 100);
  const titleIds = [...firstPage.titles.items.map((item) => item.titleId)];
  const pageCount = Math.ceil(firstPage.titles.totalCount / firstPage.titles.pageSize);
  for (let page = 2; page <= pageCount; page += 1) {
    const nextPage = await fetchCustomList(listId, page, 100);
    titleIds.push(...nextPage.titles.items.map((item) => item.titleId));
  }

  const currentIndex = titleIds.indexOf(titleId);
  const destinationIndex = currentIndex + offset;
  if (currentIndex < 0 || destinationIndex < 0 || destinationIndex >= titleIds.length) {
    return;
  }

  [titleIds[currentIndex], titleIds[destinationIndex]] = [
    titleIds[destinationIndex],
    titleIds[currentIndex],
  ];
  await request(`/api/v1/lists/${listId}/titles/order`, z.void(), {
    method: 'PUT',
    body: JSON.stringify({ titleIds }),
  });
}
