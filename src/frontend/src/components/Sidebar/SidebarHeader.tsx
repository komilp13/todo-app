'use client';

/**
 * SidebarHeader Component
 * Displays application logo only
 */
export default function SidebarHeader() {
  return (
    <div className="border-b border-gray-200 px-4 py-4">
      {/* Logo/App Name */}
      <div className="px-2">
        <h2 className="text-xl font-bold text-blue-600">
          GTD Todo
        </h2>
        <p className="text-xs text-gray-500">
          Getting Things Done
        </p>
      </div>
    </div>
  );
}
