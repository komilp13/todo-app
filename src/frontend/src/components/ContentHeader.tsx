'use client';

/**
 * ContentHeader Component
 * Provides the header bar for the main content area
 * Can contain page title, breadcrumbs, action buttons, etc.
 */
export default function ContentHeader() {
  return (
    <header className="border-b border-gray-200 bg-white px-6 py-4 shadow-sm">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            GTD Todo
          </h1>
        </div>
        <div className="flex items-center gap-4">
          {/* Space for action buttons, search, etc. */}
        </div>
      </div>
    </header>
  );
}
