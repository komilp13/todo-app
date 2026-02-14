'use client';

import { useAuth } from '@/contexts/AuthContext';
import { ReactNode } from 'react';

interface AuthLoadingProps {
  children: ReactNode;
}

/**
 * Wrapper component that shows a loading spinner while auth state is being determined.
 * Useful for protecting page content until we know if user is authenticated.
 */
export function AuthLoading({ children }: AuthLoadingProps) {
  const { isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full border-4 border-gray-300 border-t-blue-600 h-12 w-12 mb-4"></div>
          <p className="text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
