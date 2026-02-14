'use client';

import { useEffect, useState } from 'react';
import SidebarHeader from './SidebarHeader';
import SidebarNavigation from './SidebarNavigation';
import SidebarFooter from './SidebarFooter';
import { useSidebar } from '@/contexts/SidebarContext';
import { useSidebarState } from '@/hooks/useSidebarState';
import { useWindowSize } from '@/hooks/useWindowSize';

/**
 * Sidebar Component
 * Responsive sidebar with:
 * - Desktop (>= 1024px): Full width (320px) or collapsed (48px)
 * - Mobile (< 1024px): Hidden by default, slide-in overlay on hamburger click
 */
export default function Sidebar() {
  const { isMobileOpen, closeMobileSidebar } = useSidebar();
  const { isCollapsed, toggleCollapse } = useSidebarState();
  const windowSize = useWindowSize();
  const [isMobile, setIsMobile] = useState(false);

  // Determine if we're on mobile based on window width
  useEffect(() => {
    setIsMobile(windowSize.width < 1024);
  }, [windowSize.width]);

  // For mobile, return either nothing or overlay
  if (isMobile) {
    return (
      <>
        {/* Mobile Backdrop */}
        {isMobileOpen && (
          <div
            className="fixed inset-0 z-30 bg-black/50 transition-opacity duration-200 lg:hidden"
            onClick={closeMobileSidebar}
            role="presentation"
          />
        )}

        {/* Mobile Sidebar Overlay */}
        <aside
          className={`fixed left-0 top-0 z-40 flex h-screen w-80 flex-col border-r border-gray-200 bg-white transition-transform duration-200 lg:hidden ${
            isMobileOpen ? 'translate-x-0' : '-translate-x-full'
          }`}
        >
          {/* Sidebar Header - Logo/User Info */}
          <SidebarHeader />

          {/* Sidebar Navigation - Scrollable */}
          <SidebarNavigation onNavigate={closeMobileSidebar} />

          {/* Sidebar Footer */}
          <SidebarFooter />
        </aside>
      </>
    );
  }

  // Desktop layout
  return (
    <aside
      className={`flex flex-col border-r border-gray-200 bg-white transition-all duration-200 ${
        isCollapsed ? 'w-12' : 'w-80'
      }`}
    >
      {/* Collapsed View - Icon Only */}
      {isCollapsed ? (
        <>
          {/* Collapse Toggle Button */}
          <div className="flex items-center justify-center border-b border-gray-200 px-3 py-4">
            <button
              onClick={toggleCollapse}
              className="rounded-lg p-2 hover:bg-gray-100 transition-colors"
              title="Expand sidebar"
              aria-label="Expand sidebar"
            >
              ▶️
            </button>
          </div>

          {/* Icons only navigation */}
          <nav className="flex-1 overflow-y-auto px-2 py-2 space-y-1">
            <div className="text-center text-lg">📥</div>
            <div className="text-center text-lg">⭐</div>
            <div className="text-center text-lg">📅</div>
            <div className="text-center text-lg">🔮</div>
          </nav>

          {/* Collapsed Footer */}
          <div className="border-t border-gray-200 px-2 py-2">
            <button
              className="w-full rounded-lg bg-gray-100 px-2 py-2 text-xs font-medium text-gray-700 hover:bg-gray-200 transition-colors"
              title="Logout"
              onClick={() => {
                // Logout handled in SidebarFooter
              }}
            >
              🚪
            </button>
          </div>
        </>
      ) : (
        <>
          {/* Expanded View - Full Width */}
          {/* Sidebar Header - Logo/User Info */}
          <SidebarHeader />

          {/* Sidebar Navigation - Scrollable */}
          <SidebarNavigation />

          {/* Sidebar Footer with Collapse Button */}
          <div className="border-t border-gray-200 px-4 py-4 space-y-2">
            <SidebarFooter />
            <button
              onClick={toggleCollapse}
              className="w-full rounded-lg border border-gray-200 px-4 py-2 text-xs font-medium text-gray-700 hover:bg-gray-50 transition-colors"
              title="Collapse sidebar"
              aria-label="Collapse sidebar"
            >
              ← Collapse
            </button>
          </div>
        </>
      )}
    </aside>
  );
}
