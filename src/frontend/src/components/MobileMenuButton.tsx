'use client';

import { useSidebar } from '@/contexts/SidebarContext';
import { useWindowSize } from '@/hooks/useWindowSize';
import { useEffect, useState } from 'react';

/**
 * MobileMenuButton Component
 * Hamburger button for toggling mobile sidebar
 * Only visible on mobile (< 1024px)
 */
export default function MobileMenuButton() {
  const { isMobileOpen, setIsMobileOpen } = useSidebar();
  const windowSize = useWindowSize();
  const [isMobile, setIsMobile] = useState(false);

  // Determine if we're on mobile based on window width
  useEffect(() => {
    setIsMobile(windowSize.width < 1024);
  }, [windowSize.width]);

  // Only show on mobile
  if (!isMobile) {
    return null;
  }

  return (
    <div className="flex items-center justify-between border-b border-gray-200 bg-white px-4 py-3 lg:hidden">
      <button
        onClick={() => setIsMobileOpen(!isMobileOpen)}
        className="rounded-lg p-2 hover:bg-gray-100 transition-colors"
        aria-label="Toggle sidebar"
        title="Menu"
      >
        <svg
          className="h-6 w-6 text-gray-700"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M4 6h16M4 12h16M4 18h16"
          />
        </svg>
      </button>
    </div>
  );
}
