'use client';

/**
 * Inbox Page
 * Default entry point for tasks. Shows all tasks in the Inbox system list.
 *
 * This is a placeholder page structure that will be enhanced with
 * the TaskList component in future stories.
 */
export default function InboxPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Inbox</h1>
        <p className="mt-1 text-sm text-gray-600">
          Capture everything, then organize and prioritize
        </p>
      </div>

      {/* Placeholder for TaskList component (Story 4.2.1) */}
      <div className="rounded-lg border border-gray-200 bg-white p-8 text-center">
        <p className="text-gray-500">
          Task list will be available soon. Quick-add and task management features coming next.
        </p>
      </div>
    </div>
  );
}
