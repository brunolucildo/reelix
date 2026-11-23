import { z } from 'zod';

import { httpRequest } from '@/lib/http-client';
import { catalogTitleSchema } from '@/services/api-client';

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

function pageQuery(page: number, pageSize = 20) {
  return `page=${page}&pageSize=${pageSize}`;
}

export function fetchFavorites(page: number, pageSize = 20): Promise<PagedTitleList> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return httpRequest(`/api/v1/favorites?${query}`, pagedTitleListSchema);
}

export function fetchWatchlist(page: number, oldestFirst: boolean): Promise<PagedTitleList> {
  const search = new URLSearchParams({
    page: String(page),
    pageSize: '20',
    oldestFirst: String(oldestFirst),
  });
  return httpRequest(`/api/v1/watchlist?${search}`, pagedTitleListSchema);
}

export function fetchCustomLists(page: number): Promise<z.infer<typeof pagedCustomListsSchema>> {
  return httpRequest(`/api/v1/lists?${pageQuery(page)}`, pagedCustomListsSchema);
}

export function fetchCustomList(
  listId: string,
  page: number,
  pageSize = 20,
): Promise<CustomListData> {
  return httpRequest(`/api/v1/lists/${listId}?${pageQuery(page, pageSize)}`, customListSchema);
}

export function searchCatalogTitles(query: string): Promise<CatalogTitle[]> {
  const search = new URLSearchParams({
    query,
    provider: 'tmdb',
    language: 'pt-BR',
    page: '1',
  });
  return httpRequest(`/api/v1/catalog/search?${search}`, z.object({ items: z.array(titleSchema) }))
    .then((result) => result.items);
}

export function fetchTitleStatuses(titleIds: string[]): Promise<TitleRelationStatus[]> {
  return httpRequest('/api/v1/title-status', z.array(titleStatusSchema), {
    method: 'POST',
    body: { titleIds },
  });
}

export function fetchTitleDetails(
  reference: TitleReference,
  signal?: AbortSignal,
): Promise<CatalogTitle> {
  return httpRequest(
    `/api/v1/catalog/${reference.provider}/${reference.contentType}/${reference.externalId}?language=pt-BR`,
    titleSchema,
    { signal },
  );
}

export function setFavorite(title: CatalogTitle): Promise<{ added: boolean }> {
  return httpRequest(`/api/v1/favorites/${title.id}`, mutationSchema, {
    method: 'PUT',
    body: { contentType: title.contentType },
  });
}

export function removeFavorite(titleId: string): Promise<void> {
  return httpRequest(`/api/v1/favorites/${titleId}`, z.void(), { method: 'DELETE' });
}

export function setWatchlistTitle(title: CatalogTitle): Promise<{ added: boolean }> {
  return httpRequest(`/api/v1/watchlist/${title.id}`, mutationSchema, {
    method: 'PUT',
    body: { contentType: title.contentType },
  });
}

export function removeWatchlistTitle(titleId: string): Promise<void> {
  return httpRequest(`/api/v1/watchlist/${titleId}`, z.void(), { method: 'DELETE' });
}

export function createCustomList(name: string, description: string): Promise<CustomListSummary> {
  return httpRequest('/api/v1/lists', customListSummarySchema, {
    method: 'POST',
    body: { name, description },
  });
}

export function updateCustomList(
  listId: string,
  name: string,
  description: string,
): Promise<CustomListSummary> {
  return httpRequest(`/api/v1/lists/${listId}`, customListSummarySchema, {
    method: 'PUT',
    body: { name, description },
  });
}

export function deleteCustomList(listId: string): Promise<void> {
  return httpRequest(`/api/v1/lists/${listId}`, z.void(), { method: 'DELETE' });
}

export function setCustomListTitle(
  listId: string,
  title: CatalogTitle,
): Promise<{ added: boolean }> {
  return httpRequest(`/api/v1/lists/${listId}/titles/${title.id}`, mutationSchema, {
    method: 'PUT',
    body: { contentType: title.contentType },
  });
}

export function removeCustomListTitle(listId: string, titleId: string): Promise<void> {
  return httpRequest(`/api/v1/lists/${listId}/titles/${titleId}`, z.void(), { method: 'DELETE' });
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
  await httpRequest(`/api/v1/lists/${listId}/titles/order`, z.void(), {
    method: 'PUT',
    body: { titleIds },
  });
}
