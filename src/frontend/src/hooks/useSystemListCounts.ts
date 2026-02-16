import { useEffect, useState, useCallback } from 'react';
import { apiClient } from '@/services/apiClient';
import { SystemList } from '@/types';

interface SystemListCounts {
  [key: string]: number;
}

interface GetTasksResponse {
  tasks: unknown[];
  totalCount: number;
}

/**
 * useSystemListCounts Hook
 * Fetches open task counts for each system list (Inbox, Next, Upcoming, Someday)
 * Pass a refresh counter to trigger re-fetches when tasks change.
 */
export function useSystemListCounts(refresh: number = 0) {
  const [counts, setCounts] = useState<SystemListCounts>({
    [SystemList.Inbox]: 0,
    [SystemList.Next]: 0,
    [SystemList.Upcoming]: 0,
    [SystemList.Someday]: 0,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchCounts = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      // Fetch counts in parallel: one call per system list
      // Upcoming is excluded — it's a computed cross-list view, not a real list
      const [inboxRes, nextRes, somedayRes] = await Promise.all([
        apiClient.get<GetTasksResponse>(`/tasks?systemList=${SystemList.Inbox}`),
        apiClient.get<GetTasksResponse>(`/tasks?systemList=${SystemList.Next}`),
        apiClient.get<GetTasksResponse>(`/tasks?systemList=${SystemList.Someday}`),
      ]);

      setCounts({
        [SystemList.Inbox]: inboxRes.data.totalCount,
        [SystemList.Next]: nextRes.data.totalCount,
        [SystemList.Someday]: somedayRes.data.totalCount,
      });
    } catch (err) {
      setCounts({
        [SystemList.Inbox]: 0,
        [SystemList.Next]: 0,
        [SystemList.Upcoming]: 0,
        [SystemList.Someday]: 0,
      });
      setError(err instanceof Error ? err.message : 'Failed to fetch task counts');
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Fetch on mount and whenever refresh changes
  useEffect(() => {
    fetchCounts();
  }, [fetchCounts, refresh]);

  return { counts, isLoading, error, refetch: fetchCounts };
}
