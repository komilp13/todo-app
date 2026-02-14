'use client';

import { useAuth } from '@/contexts/AuthContext';

/**
 * SidebarHeader Component
 * Displays application logo and user information
 */
export default function SidebarHeader() {
  const { user } = useAuth();

  return (
    <div className="border-b border-gray-200 px-6 py-6">
      {/* Logo/App Name */}
      <div className="mb-6">
        <h2 className="text-xl font-bold text-blue-600">
          GTD Todo
        </h2>
        <p className="text-xs text-gray-500">
          Getting Things Done
        </p>
      </div>

      {/* User Info */}
      {user && (
        <div className="rounded-lg bg-gray-50 p-3">
          <p className="text-sm font-medium text-gray-900">
            {user.displayName}
          </p>
          <p className="text-xs text-gray-600">
            {user.email}
          </p>
        </div>
      )}
    </div>
  );
}
