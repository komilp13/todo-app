'use client';

/**
 * Next Page
 * Curated focus list of tasks the user has decided to work on soon.
 *
 * This is a placeholder page structure that will be enhanced with
 * the TaskList component in future stories.
 */
export default function NextPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Next</h1>
        <p className="mt-1 text-sm text-gray-600">
          Tasks you&apos;re working on soon. Drag to prioritize.
        </p>
      </div>

      {/* Placeholder for TaskList component (Story 4.2.1) */}
      <div className="rounded-lg border border-gray-200 bg-white p-8 text-center">
        <p className="text-gray-500">
          Task list will be available soon. Move tasks here from Inbox to focus on what matters.
        </p>
      </div>
    </div>
  );
}
