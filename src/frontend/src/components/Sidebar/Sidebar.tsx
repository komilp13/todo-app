'use client';

import SidebarHeader from './SidebarHeader';
import SidebarNavigation from './SidebarNavigation';
import SidebarFooter from './SidebarFooter';

/**
 * Sidebar Component
 * Fixed-width (280px) sidebar containing header, navigation, and footer
 */
export default function Sidebar() {
  return (
    <aside className="flex w-80 flex-col border-r border-gray-200 bg-white">
      {/* Sidebar Header - Logo/User Info */}
      <SidebarHeader />

      {/* Sidebar Navigation - Scrollable */}
      <SidebarNavigation />

      {/* Sidebar Footer */}
      <SidebarFooter />
    </aside>
  );
}
