/**
 * Type definitions for the GTD Todo Application
 * These mirror the backend domain model in TodoApp.Domain
 */

// Enums (values use camelCase to match backend JSON serialization)
export enum Priority {
  P1 = 'p1', // Highest
  P2 = 'p2',
  P3 = 'p3',
  P4 = 'p4', // Lowest
}

export enum TaskStatus {
  Open = 'open',
  Done = 'done',
}

export enum SystemList {
  Inbox = 'inbox',
  Next = 'next',
  Upcoming = 'upcoming',
  Someday = 'someday',
}

export enum ProjectStatus {
  Active = 'active',
  Completed = 'completed',
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
  priority?: Priority | null;
  status: TaskStatus;
  systemList: SystemList;
  sortOrder: number;
  projectId?: string;
  projectName?: string;
  labels?: { id: string; name: string; color?: string }[];
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

export interface ProjectItem {
  id: string;
  name: string;
  description?: string;
  dueDate?: string;
  status: ProjectStatus;
  sortOrder: number;
  totalTaskCount: number;
  completedTaskCount: number;
  completionPercentage: number;
  createdAt: string;
  updatedAt: string;
}

export interface GetProjectsResponse {
  projects: ProjectItem[];
  totalCount: number;
}

export interface Label {
  id: string;
  userId: string;
  name: string;
  color?: string;
  createdAt: string;
}

export interface LabelItem {
  id: string;
  name: string;
  color?: string;
  taskCount: number;
  createdAt: string;
}

export interface GetLabelsResponse {
  labels: LabelItem[];
  totalCount: number;
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
