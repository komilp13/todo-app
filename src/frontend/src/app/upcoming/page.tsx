'use client';

/**
 * Upcoming Page
 * Date-driven view showing tasks with due dates in the next 14 days,
 * plus tasks explicitly assigned to the Upcoming system list.
 *
 * This is a placeholder page structure that will be enhanced with
 * date grouping and the TaskList component in future stories.
 */
export default function UpcomingPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Upcoming</h1>
        <p className="mt-1 text-sm text-gray-600">
          Tasks coming up. Plan ahead and stay on schedule.
        </p>
      </div>

      {/* Placeholder for date-grouped TaskList component (Story 5.3.2) */}
      <div className="rounded-lg border border-gray-200 bg-white p-8 text-center">
        <p className="text-gray-500">
          Upcoming tasks will be grouped by date. Set due dates to see them here.
        </p>
      </div>
    </div>
  );
}
