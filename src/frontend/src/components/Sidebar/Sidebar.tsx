'use client';

import { useEffect, useState } from 'react';
import SidebarHeader from './SidebarHeader';
import SidebarNavigation from './SidebarNavigation';
import SidebarFooter from './SidebarFooter';
import { useSidebar } from '@/contexts/SidebarContext';
import { useWindowSize } from '@/hooks/useWindowSize';

/**
 * Sidebar Component
 * Responsive sidebar with:
 * - Desktop (>= 1024px): Full width (320px)
 * - Mobile (< 1024px): Hidden by default, slide-in overlay on hamburger click
 */
export default function Sidebar() {
  const { isMobileOpen, closeMobileSidebar } = useSidebar();
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
          {/* Sidebar Header - Logo only */}
          <SidebarHeader />

          {/* Sidebar Navigation - Scrollable */}
          <SidebarNavigation onNavigate={closeMobileSidebar} />

          {/* Sidebar Footer - User Profile Menu */}
          <SidebarFooter />
        </aside>
      </>
    );
  }

  // Desktop layout - full width sidebar
  return (
    <aside className="flex flex-col w-80 border-r border-gray-200 bg-white">
      {/* Sidebar Header - Logo only */}
      <SidebarHeader />

      {/* Sidebar Navigation - Scrollable */}
      <SidebarNavigation />

      {/* Sidebar Footer - User Profile Menu */}
      <SidebarFooter />
    </aside>
  );
}
