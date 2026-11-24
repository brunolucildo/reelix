import { useState } from 'react';

import type { CatalogSeasonSummary } from '@/features/catalog/api/catalog-api';

/**
 * Season picked per series (kept while the screen stays mounted). Falls back to the first
 * regular season, then to the first available one, when nothing valid is selected.
 */
export function useSeasonSelection(seriesKey: string, seasons: CatalogSeasonSummary[]) {
  const [selections, setSelections] = useState<Record<string, number>>({});
  const saved = selections[seriesKey];
  const fallback = seasons.find((season) => season.seasonNumber > 0)?.seasonNumber ??
    seasons[0]?.seasonNumber ?? null;
  const seasonNumber = seasons.some((season) => season.seasonNumber === saved) ? saved : fallback;
  const select = (number: number) => {
    setSelections((current) => ({ ...current, [seriesKey]: number }));
  };

  return { seasonNumber, select };
}
