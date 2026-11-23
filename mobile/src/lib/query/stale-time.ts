/** Freshness windows (ms) for TanStack Query. `default` is applied globally by the QueryClient. */
export const staleTime = {
  default: 30_000,
  short: 60_000,
  catalog: 300_000,
} as const;
