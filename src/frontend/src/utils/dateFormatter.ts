/**
 * Date formatting utilities for the GTD Todo Application
 * Formats dates as relative strings (Today, Tomorrow, Overdue, etc.)
 */

export function formatRelativeDate(dateString?: string): string {
  if (!dateString) return '';

  const dueDate = new Date(dateString);
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const tomorrow = new Date(today);
  tomorrow.setDate(tomorrow.getDate() + 1);

  const yesterday = new Date(today);
  yesterday.setDate(yesterday.getDate() - 1);

  dueDate.setHours(0, 0, 0, 0);

  if (dueDate.getTime() === today.getTime()) {
    return 'Today';
  }

  if (dueDate.getTime() === tomorrow.getTime()) {
    return 'Tomorrow';
  }

  if (dueDate.getTime() === yesterday.getTime()) {
    return 'Yesterday';
  }

  if (dueDate.getTime() < today.getTime()) {
    return 'Overdue';
  }

  // Format as date string for future dates
  const options: Intl.DateTimeFormatOptions = {
    month: 'short',
    day: 'numeric',
  };

  // Add year if it's not the current year
  const currentYear = today.getFullYear();
  if (dueDate.getFullYear() !== currentYear) {
    options.year = 'numeric';
  }

  return dueDate.toLocaleDateString('en-US', options);
}

/**
 * Determine if a date is overdue
 */
export function isOverdue(dateString?: string): boolean {
  if (!dateString) return false;

  const dueDate = new Date(dateString);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  dueDate.setHours(0, 0, 0, 0);

  return dueDate.getTime() < today.getTime();
}

/**
 * Get priority color class
 * Note: priority values are now lowercase (p1, p2, p3, p4) to match backend camelCase serialization
 */
export function getPriorityColor(priority: string): string {
  switch (priority.toLowerCase()) {
    case 'p1':
      return '#ff4440';
    case 'p2':
      return '#ff9933';
    case 'p3':
      return '#4073ff';
    case 'p4':
    default:
      return '#999999';
  }
}

/**
 * Get priority color Tailwind class
 * Note: priority values are now lowercase (p1, p2, p3, p4) to match backend camelCase serialization
 */
export function getPriorityTailwindClass(priority: string): string {
  switch (priority.toLowerCase()) {
    case 'p1':
      return 'bg-red-100 text-red-800';
    case 'p2':
      return 'bg-orange-100 text-orange-800';
    case 'p3':
      return 'bg-blue-100 text-blue-800';
    case 'p4':
    default:
      return 'bg-gray-100 text-gray-800';
  }
}
