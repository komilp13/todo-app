'use client';

import { useAuth } from '@/contexts/AuthContext';

/**
 * SidebarFooter Component
 * Contains user actions like logout
 */
export default function SidebarFooter() {
  const { logout } = useAuth();

  const handleLogout = async () => {
    await logout();
  };

  return (
    <div className="border-t border-gray-200 px-4 py-4">
      <button
        onClick={handleLogout}
        className="w-full rounded-lg bg-gray-100 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-200 transition-colors"
      >
        Logout
      </button>
    </div>
  );
}
