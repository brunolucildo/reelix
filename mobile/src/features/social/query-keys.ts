export const socialQueryKeys = {
  all: ['social'] as const,
  search: (query: string) => ['social', 'search', query] as const,
  friends: ['social', 'friends'] as const,
  requests: (direction: 'incoming' | 'outgoing') => ['social', 'requests', direction] as const,
  feed: ['social', 'feed'] as const,
  friendProfileAll: ['social', 'friend-profile'] as const,
  friendProfile: (accountId: string) => ['social', 'friend-profile', accountId] as const,
  friendActivitiesAll: ['social', 'friend-activities'] as const,
  friendActivities: (accountId: string) => ['social', 'friend-activities', accountId] as const,
};
