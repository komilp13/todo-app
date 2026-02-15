/**
 * Enum Formatting Utilities
 * Helper functions to display enum values in a user-friendly format
 * Since enum values are now lowercase (to match backend camelCase serialization),
 * we need helpers to capitalize them for display
 */

import { Priority, SystemList, TaskStatus, ProjectStatus } from '@/types';

/**
 * Capitalize first letter of a string
 */
function capitalizeFirst(str: string): string {
  if (!str) return str;
  return str.charAt(0).toUpperCase() + str.slice(1);
}

/**
 * Format SystemList enum for display
 * Examples: "inbox" -> "Inbox", "next" -> "Next"
 */
export function formatSystemList(systemList: SystemList): string {
  return capitalizeFirst(systemList);
}

/**
 * Format TaskStatus enum for display
 * Examples: "open" -> "Open", "done" -> "Done"
 */
export function formatTaskStatus(status: TaskStatus): string {
  return capitalizeFirst(status);
}

/**
 * Format ProjectStatus enum for display
 * Examples: "active" -> "Active", "completed" -> "Completed"
 */
export function formatProjectStatus(status: ProjectStatus): string {
  return capitalizeFirst(status);
}

/**
 * Format Priority enum for display
 * Examples: "p1" -> "P1", "p2" -> "P2"
 */
export function formatPriority(priority: Priority): string {
  return priority.toUpperCase();
}

/**
 * Get all SystemList values for dropdowns/selects
 * Returns array of objects with value and display label
 */
export function getSystemListOptions() {
  return Object.values(SystemList).map((value) => ({
    value,
    label: formatSystemList(value),
  }));
}

/**
 * Get all Priority values for dropdowns/selects
 * Returns array of objects with value and display label
 */
export function getPriorityOptions() {
  return Object.values(Priority).map((value) => ({
    value,
    label: formatPriority(value),
  }));
}
