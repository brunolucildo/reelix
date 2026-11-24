/** `getNextPageParam` for endpoints paginated by an opaque `nextCursor`. */
export function getNextCursor(lastPage: { nextCursor: string | null }): string | undefined {
  return lastPage.nextCursor ?? undefined;
}

/** `getNextPageParam` for numbered pages described by `page`, `pageSize` and `totalCount`. */
export function getNextPageByCount(lastPage: {
  page: number;
  pageSize: number;
  totalCount: number;
}): number | undefined {
  return lastPage.page * lastPage.pageSize < lastPage.totalCount ? lastPage.page + 1 : undefined;
}

/** `getNextPageParam` for numbered pages described by `page` and `totalPages`. */
export function getNextPageByTotal(lastPage: { page: number; totalPages: number }): number | undefined {
  return lastPage.page < lastPage.totalPages ? lastPage.page + 1 : undefined;
}

/** Number of pages for a numbered pager (at least 1, even when there are no items). */
export function getTotalPages(totalCount: number, pageSize: number): number {
  return Math.max(1, Math.ceil(totalCount / pageSize));
}
