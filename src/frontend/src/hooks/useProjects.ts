import { useEffect, useState, useCallback } from 'react';
import { apiClient } from '@/services/apiClient';
import { ProjectItem, GetProjectsResponse } from '@/types';

/**
 * useProjects Hook
 * Fetches all projects for the authenticated user with task statistics.
 * Pass a refresh counter to trigger re-fetches when projects or tasks change.
 */
export function useProjects(refresh: number = 0) {
  const [projects, setProjects] = useState<ProjectItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchProjects = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      const response = await apiClient.get<GetProjectsResponse>('/projects');
      setProjects(response.data.projects);
    } catch (err) {
      setProjects([]);
      setError(err instanceof Error ? err.message : 'Failed to fetch projects');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchProjects();
  }, [fetchProjects, refresh]);

  return { projects, isLoading, error, refetch: fetchProjects };
}
