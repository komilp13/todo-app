'use client';

import { useAuth } from '@/contexts/AuthContext';
import UserProfileMenu from '../UserProfileMenu';

/**
 * SidebarHeader Component
 * Displays application logo and user profile menu
 */
export default function SidebarHeader() {
  const { user, isLoading } = useAuth();

  return (
    <div className="border-b border-gray-200 px-4 py-4 space-y-4">
      {/* Logo/App Name */}
      <div className="px-2">
        <h2 className="text-xl font-bold text-blue-600">
          GTD Todo
        </h2>
        <p className="text-xs text-gray-500">
          Getting Things Done
        </p>
      </div>

      {/* User Profile Menu */}
      {user && !isLoading && (
        <UserProfileMenu displayName={user.displayName} email={user.email} />
      )}

      {/* Loading placeholder */}
      {isLoading && (
        <div className="px-2 h-14 bg-gray-100 rounded-lg animate-pulse" />
      )}
    </div>
  );
}
