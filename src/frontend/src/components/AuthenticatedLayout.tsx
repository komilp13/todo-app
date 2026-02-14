'use client';

import { ReactNode } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import AppShell from './AppShell';

interface AuthenticatedLayoutProps {
  children: ReactNode;
}

/**
 * AuthenticatedLayout Component
 * Conditionally renders AppShell (sidebar + content) for authenticated users
 * Shows children without shell for unauthenticated users (login/register pages)
 */
export default function AuthenticatedLayout({ children }: AuthenticatedLayoutProps) {
  const { isAuthenticated, isLoading } = useAuth();

  // Show nothing while checking authentication status
  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="mb-4 h-8 w-8 animate-spin rounded-full border-4 border-blue-200 border-t-blue-600 mx-auto"></div>
          <p className="text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  // Render with AppShell for authenticated users
  if (isAuthenticated) {
    return <AppShell>{children}</AppShell>;
  }

  // Render without shell for unauthenticated users (login/register pages)
  return children;
}
