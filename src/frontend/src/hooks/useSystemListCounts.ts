import { useEffect, useState } from 'react';
import { apiClient } from '@/services/apiClient';
import { SystemList, TodoTask } from '@/types';

interface SystemListCounts {
  [key: string]: number;
}

/**
 * useSystemListCounts Hook
 * Fetches open task counts for each system list (Inbox, Next, Upcoming, Someday)
 * Caches results and allows manual refresh
 */
export function useSystemListCounts() {
  const [counts, setCounts] = useState<SystemListCounts>({
    [SystemList.Inbox]: 0,
    [SystemList.Next]: 0,
    [SystemList.Upcoming]: 0,
    [SystemList.Someday]: 0,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchCounts = async () => {
    try {
      setIsLoading(true);
      setError(null);

      // Fetch all open, non-archived tasks
      // The API should support filtering by systemList via query params
      const systemLists = [
        SystemList.Inbox,
        SystemList.Next,
        SystemList.Upcoming,
        SystemList.Someday,
      ];

      const newCounts: SystemListCounts = {};

      // Fetch counts for each system list
      // For now, we'll fetch all tasks and filter client-side
      // In the future, the API can optimize this with a dedicated endpoint
      try {
        const response = await apiClient.get<TodoTask[]>('/tasks', {
          params: {
            status: 'Open',
            archived: 'false',
          },
        });

        const tasks = response.data || [];

        // Count tasks by system list
        systemLists.forEach((list) => {
          newCounts[list] = tasks.filter((task) => task.systemList === list).length;
        });

        setCounts(newCounts);
      } catch (err) {
        // If the API call fails, set all counts to 0
        systemLists.forEach((list) => {
          newCounts[list] = 0;
        });
        setCounts(newCounts);
        setError(err instanceof Error ? err.message : 'Failed to fetch task counts');
      }
    } finally {
      setIsLoading(false);
    }
  };

  // Fetch counts on mount
  useEffect(() => {
    fetchCounts();
  }, []);

  return { counts, isLoading, error, refetch: fetchCounts };
}
