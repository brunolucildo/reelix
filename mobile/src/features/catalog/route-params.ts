// Validation of the catalog route params (deep links): routes parse them, hooks and components
// receive typed values. `isCatalogProvider` is defined once in the catalog API module.
export { isCatalogProvider } from '@/features/catalog/api/catalog-api';

export function isCatalogContentType(value: unknown): value is 'movie' | 'series' {
  return value === 'movie' || value === 'series';
}
