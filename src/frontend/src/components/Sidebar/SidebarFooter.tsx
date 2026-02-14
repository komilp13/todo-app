'use client';

import UserProfileMenu from '../UserProfileMenu';
import { useAuth } from '@/contexts/AuthContext';

/**
 * SidebarFooter Component
 * Contains user profile menu with dropdown actions
 */
export default function SidebarFooter() {
  const { user, isLoading } = useAuth();

  return (
    <div className="border-t border-gray-200 px-4 py-4">
      {/* User Profile Menu */}
      {user && !isLoading && (
        <UserProfileMenu displayName={user.displayName} email={user.email} />
      )}

      {/* Loading placeholder */}
      {isLoading && (
        <div className="h-14 bg-gray-100 rounded-lg animate-pulse" />
      )}
    </div>
  );
}
