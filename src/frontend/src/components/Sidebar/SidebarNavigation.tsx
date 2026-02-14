'use client';

/**
 * SidebarNavigation Component
 * Scrollable navigation area for future menu items
 * Will contain system lists, projects, and labels
 */
export default function SidebarNavigation() {
  return (
    <nav className="flex-1 overflow-y-auto px-4 py-4">
      {/* Navigation items will be added in future stories */}
      <div className="space-y-2">
        <div className="text-xs font-semibold uppercase tracking-wide text-gray-500">
          Navigation
        </div>
        <p className="text-sm text-gray-400">
          System lists and projects coming soon...
        </p>
      </div>
    </nav>
  );
}
