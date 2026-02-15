/**
 * useTaskRefresh Hook
 * Register a task list refresh callback with the global refresh context
 * Automatically unregisters when component unmounts
 *
 * Usage:
 * const { refetchTasks } = useQuery();
 * useTaskRefresh('inbox', refetchTasks);
 */

import { useEffect, useRef } from 'react';
import { useTaskRefreshContext } from '@/contexts/TaskRefreshContext';

export function useTaskRefresh(
  pageId: string,
  refreshCallback: () => void | Promise<void>
) {
  const { registerRefreshCallback, unregisterRefreshCallback } =
    useTaskRefreshContext();
  const callbackRef = useRef(refreshCallback);

  // Update callback ref when it changes
  useEffect(() => {
    callbackRef.current = refreshCallback;
  }, [refreshCallback]);

  // Register on mount, unregister on unmount
  useEffect(() => {
    registerRefreshCallback(pageId, () => callbackRef.current());

    return () => {
      unregisterRefreshCallback(pageId);
    };
  }, [pageId, registerRefreshCallback, unregisterRefreshCallback]);
}
