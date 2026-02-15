/**
 * TaskListSkeleton Component
 * Loading skeleton for task list showing placeholder rows
 */

export default function TaskListSkeleton() {
  return (
    <div className="space-y-2">
      {Array.from({ length: 5 }).map((_, i) => (
        <div
          key={i}
          className="flex items-start gap-3 rounded-lg border border-gray-200 bg-white px-3 py-2.5 animate-pulse"
        >
          {/* Checkbox skeleton */}
          <div className="mt-1 h-5 w-5 rounded border border-gray-200 bg-gray-100" />

          {/* Content skeleton */}
          <div className="flex-1 space-y-2">
            <div className="h-4 w-3/4 rounded bg-gray-200" />
            <div className="flex gap-2">
              <div className="h-5 w-12 rounded bg-gray-200" />
              <div className="h-5 w-16 rounded bg-gray-200" />
              <div className="h-5 w-20 rounded bg-gray-200" />
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
