/**
 * Type definitions for the GTD Todo Application
 * These mirror the backend domain model in TodoApp.Domain
 */

// Enums
export enum Priority {
  P1 = 'P1', // Highest
  P2 = 'P2',
  P3 = 'P3',
  P4 = 'P4', // Lowest
}

export enum TaskStatus {
  Open = 'Open',
  Done = 'Done',
}

export enum SystemList {
  Inbox = 'Inbox',
  Next = 'Next',
  Upcoming = 'Upcoming',
  Someday = 'Someday',
}

export enum ProjectStatus {
  Active = 'Active',
  Completed = 'Completed',
}

// Domain Models
export interface User {
  id: string;
  email: string;
  displayName: string;
  createdAt: string;
  updatedAt: string;
}

export interface TodoTask {
  id: string;
  userId: string;
  name: string;
  description?: string;
  priority: Priority;
  status: TaskStatus;
  systemList: SystemList;
  sortOrder: number;
  projectId?: string;
  isArchived: boolean;
  dueDate?: string;
  completedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Project {
  id: string;
  userId: string;
  name: string;
  description?: string;
  dueDate?: string;
  status: ProjectStatus;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface Label {
  id: string;
  userId: string;
  name: string;
  color?: string;
  createdAt: string;
}

export interface TaskLabel {
  taskId: string;
  labelId: string;
}

// API Request/Response Types
export interface AuthResponse {
  user: User;
  token: string;
  expiresIn: number;
}

export interface HealthResponse {
  status: string;
  timestamp: string;
}
