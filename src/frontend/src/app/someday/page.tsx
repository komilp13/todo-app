'use client';

/**
 * Someday Page
 * Deferred tasks using GTD's Someday/Maybe list concept.
 *
 * This is a placeholder page structure that will be enhanced with
 * the TaskList component in future stories.
 */
export default function SomedayPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Someday</h1>
        <p className="mt-1 text-sm text-gray-600">
          Ideas and wishes for the future. Revisit when ready.
        </p>
      </div>

      {/* Placeholder for TaskList component (Story 4.2.1) */}
      <div className="rounded-lg border border-gray-200 bg-white p-8 text-center">
        <p className="text-gray-500">
          Task list will be available soon. Move tasks here to clear your mind.
        </p>
      </div>
    </div>
  );
}
