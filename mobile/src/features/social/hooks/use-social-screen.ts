import { useEffect, useState } from 'react';

import {
  type RequestDirection,
  type SocialTab,
  useSocialQueries,
} from '@/features/social/hooks/use-social-queries';
import { useSocialMutations } from '@/features/social/hooks/use-social-mutations';

export type ActivityFilter = 'all' | 'watched' | 'ratings';

/** Tab, filter and search state of the social screen together with its queries and mutations. */
export function useSocialScreen() {
  const [activeTab, setActiveTab] = useState<SocialTab>('feed');
  const [requestDirection, setRequestDirection] = useState<RequestDirection>('incoming');
  const [activityFilter, setActivityFilter] = useState<ActivityFilter>('all');
  const [searchText, setSearchText] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [friendToRemove, setFriendToRemove] = useState<{ id: string; name: string } | null>(null);

  useEffect(() => {
    const timer = setTimeout(() => setSearchQuery(searchText.trim()), 300);
    return () => clearTimeout(timer);
  }, [searchText]);

  const queries = useSocialQueries(activeTab, requestDirection, searchQuery);
  const mutations = useSocialMutations();
  const { allActivities } = queries;
  const visibleActivities = activeTab !== 'activities' || activityFilter === 'all'
    ? allActivities
    : allActivities.filter((activity) => activityFilter === 'ratings'
      ? activity.activityType === 'ratingUpdated'
      : activity.activityType === 'movieWatched' || activity.activityType === 'episodeWatched');

  return {
    activeTab,
    setActiveTab,
    requestDirection,
    setRequestDirection,
    activityFilter,
    setActivityFilter,
    searchText,
    setSearchText,
    friendToRemove,
    setFriendToRemove,
    queries,
    mutations,
    visibleActivities,
  };
}

export type SocialScreenState = ReturnType<typeof useSocialScreen>;
