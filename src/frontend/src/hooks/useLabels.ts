import { useEffect, useState, useCallback } from 'react';
import { apiClient } from '@/services/apiClient';
import { LabelItem, GetLabelsResponse } from '@/types';

/**
 * useLabels Hook
 * Fetches all labels for the authenticated user with task counts.
 * Pass a refresh counter to trigger re-fetches when labels or tasks change.
 */
export function useLabels(refresh: number = 0) {
  const [labels, setLabels] = useState<LabelItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchLabels = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      const response = await apiClient.get<GetLabelsResponse>('/labels');
      setLabels(response.data.labels);
    } catch (err) {
      setLabels([]);
      setError(err instanceof Error ? err.message : 'Failed to fetch labels');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchLabels();
  }, [fetchLabels, refresh]);

  return { labels, isLoading, error, refetch: fetchLabels };
}
