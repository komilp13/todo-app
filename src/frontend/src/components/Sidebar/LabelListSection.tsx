'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { LabelItem } from '@/types';

interface LabelListSectionProps {
  labels: LabelItem[];
  isLoading: boolean;
  onNavigate?: () => void;
  onAddLabel?: () => void;
}

/**
 * LabelListSection Component
 * Renders the "Labels" section in the sidebar with color dots,
 * task count badges, and a "+" add button.
 */
export default function LabelListSection({
  labels,
  isLoading,
  onNavigate,
  onAddLabel,
}: LabelListSectionProps) {
  const pathname = usePathname();

  return (
    <div className="mt-6 space-y-2">
      {/* Section Header */}
      <div className="flex items-center justify-between">
        <div className="text-xs font-semibold uppercase tracking-wide text-gray-500">
          Labels
        </div>
        <button
          onClick={onAddLabel}
          className="flex h-5 w-5 items-center justify-center rounded text-gray-400 transition-colors hover:bg-gray-200 hover:text-gray-600"
          title="Add label"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
        </button>
      </div>

      {/* Loading skeleton */}
      {isLoading && (
        <div className="space-y-1">
          {[1, 2].map(i => (
            <div key={i} className="flex items-center gap-3 rounded-lg px-3 py-2">
              <div className="h-3 w-3 animate-pulse rounded-full bg-gray-200" />
              <div className="h-4 w-20 animate-pulse rounded bg-gray-200" />
            </div>
          ))}
        </div>
      )}

      {/* Empty state */}
      {!isLoading && labels.length === 0 && (
        <p className="px-3 text-sm text-gray-400">
          Create your first label
        </p>
      )}

      {/* Label list */}
      {!isLoading && labels.length > 0 && (
        <div className="space-y-0.5">
          {labels.map(label => {
            const route = `/labels/${label.id}`;
            const isActive = pathname === route;

            return (
              <Link key={label.id} href={route} onClick={onNavigate}>
                <div
                  className={`flex items-center justify-between rounded-lg px-3 py-1.5 transition-colors ${
                    isActive
                      ? 'bg-blue-50 text-blue-700'
                      : 'text-gray-700 hover:bg-gray-100'
                  }`}
                >
                  <div className="flex items-center gap-3 min-w-0">
                    <div
                      className="h-3 w-3 shrink-0 rounded-full"
                      style={{ backgroundColor: label.color || '#9ca3af' }}
                    />
                    <span className="truncate text-sm font-medium">{label.name}</span>
                  </div>
                  {label.taskCount > 0 && (
                    <div
                      className={`ml-2 inline-flex shrink-0 items-center justify-center rounded-full px-2 py-0.5 text-xs font-semibold ${
                        isActive
                          ? 'bg-blue-200 text-blue-700'
                          : 'bg-gray-200 text-gray-600'
                      }`}
                    >
                      {label.taskCount}
                    </div>
                  )}
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}
