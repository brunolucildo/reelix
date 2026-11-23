export const gamificationQueryKeys = {
  all: ['gamification'] as const,
  dashboard: ['gamification', 'dashboard'] as const,
  statistics: (periodStart: string, periodEnd: string) =>
    ['gamification', 'statistics', periodStart, periodEnd] as const,
  rankingAll: ['gamification', 'ranking'] as const,
  ranking: (year: number, month: number) => ['gamification', 'ranking', year, month] as const,
  publicBadges: (accountId: string) => ['gamification', 'public-badges', accountId] as const,
};
