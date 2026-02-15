'use client';

import { useTaskCreateModalContext } from '@/contexts/TaskCreateModalContext';

/**
 * ContentHeader Component
 * Provides the header bar for the main content area
 * Contains action buttons, search, and other controls
 */
export default function ContentHeader() {
  const { openModal } = useTaskCreateModalContext();

  return (
    <header className="bg-white border-b border-gray-200 px-6 py-4">
      <div className="flex items-center justify-between">
        {/* Left side - empty for future search/filters */}
        <div></div>

        {/* Right side - action buttons */}
        <div className="flex items-center gap-2">
          {/* Create Task Button */}
          <button
            onClick={openModal}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
            title="Create task (or press Q)"
          >
            <svg
              className="w-5 h-5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 4v16m8-8H4"
              />
            </svg>
            <span className="hidden sm:inline">New Task</span>
            <span className="text-xs text-blue-100 hidden md:inline">
              (Q)
            </span>
          </button>
        </div>
      </div>
    </header>
  );
}
