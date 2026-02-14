'use client';

/**
 * SidebarHeader Component
 * Displays application logo
 */
export default function SidebarHeader() {
  return (
    <div className="border-b border-gray-200 px-6 py-6">
      {/* Logo/App Name */}
      <div>
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
