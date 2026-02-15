import { useState, useEffect } from 'react';
import { apiClient } from '@/services/apiClient';

export function useApiHealth() {
  const [isHealthy, setIsHealthy] = useState<boolean | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const checkHealth = async () => {
      try {
        // Try to reach the health endpoint
        const response = await apiClient.get('/health', { skipAuth: true });
        setIsHealthy(true);
        setError(null);
      } catch (err: any) {
        setIsHealthy(false);
        const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

        if (err.statusCode === 0) {
          setError(`Cannot connect to the backend at ${apiUrl}. Is the backend running?`);
        } else {
          setError(`Backend health check failed: ${err.message}`);
        }
        console.error('API health check failed:', err);
      }
    };

    // Check health on mount, but don't block the UI
    checkHealth();
  }, []);

  return { isHealthy, error };
}
