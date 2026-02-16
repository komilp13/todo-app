'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ProjectItem, ProjectStatus } from '@/types';

interface ProjectListSectionProps {
  projects: ProjectItem[];
  isLoading: boolean;
  onNavigate?: () => void;
  onAddProject?: () => void;
}

/**
 * ProjectListSection Component
 * Renders the "Projects" section in the sidebar with active projects,
 * a collapsible "Completed" subsection, and a "+" add button.
 */
export default function ProjectListSection({
  projects,
  isLoading,
  onNavigate,
  onAddProject,
}: ProjectListSectionProps) {
  const pathname = usePathname();
  const [showCompleted, setShowCompleted] = useState(false);

  const activeProjects = projects.filter(p => p.status === ProjectStatus.Active);
  const completedProjects = projects.filter(p => p.status === ProjectStatus.Completed);

  const getOpenTaskCount = (project: ProjectItem) =>
    project.totalTaskCount - project.completedTaskCount;

  return (
    <div className="mt-6 space-y-2">
      {/* Section Header */}
      <div className="flex items-center justify-between">
        <div className="text-xs font-semibold uppercase tracking-wide text-gray-500">
          Projects
        </div>
        <button
          onClick={onAddProject}
          className="flex h-5 w-5 items-center justify-center rounded text-gray-400 transition-colors hover:bg-gray-200 hover:text-gray-600"
          title="Add project"
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
              <div className="h-4 w-4 animate-pulse rounded bg-gray-200" />
              <div className="h-4 w-24 animate-pulse rounded bg-gray-200" />
            </div>
          ))}
        </div>
      )}

      {/* Empty state */}
      {!isLoading && projects.length === 0 && (
        <p className="px-3 text-sm text-gray-400">
          Create your first project
        </p>
      )}

      {/* Active projects */}
      {!isLoading && activeProjects.length > 0 && (
        <div className="space-y-0.5">
          {activeProjects.map(project => {
            const route = `/projects/${project.id}`;
            const isActive = pathname === route;
            const openCount = getOpenTaskCount(project);

            return (
              <Link key={project.id} href={route} onClick={onNavigate}>
                <div
                  className={`flex items-center justify-between rounded-lg px-3 py-1.5 transition-colors ${
                    isActive
                      ? 'bg-blue-50 text-blue-700'
                      : 'text-gray-700 hover:bg-gray-100'
                  }`}
                >
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="text-sm">📁</div>
                    <span className="truncate text-sm font-medium">{project.name}</span>
                  </div>
                  {openCount > 0 && (
                    <div
                      className={`ml-2 inline-flex shrink-0 items-center justify-center rounded-full px-2 py-0.5 text-xs font-semibold ${
                        isActive
                          ? 'bg-blue-200 text-blue-700'
                          : 'bg-gray-200 text-gray-600'
                      }`}
                    >
                      {openCount}
                    </div>
                  )}
                </div>
              </Link>
            );
          })}
        </div>
      )}

      {/* Completed projects (collapsible) */}
      {!isLoading && completedProjects.length > 0 && (
        <div>
          <button
            onClick={() => setShowCompleted(!showCompleted)}
            className="flex w-full items-center gap-1 px-3 py-1 text-xs text-gray-400 transition-colors hover:text-gray-600"
          >
            <svg
              className={`h-3 w-3 transition-transform ${showCompleted ? 'rotate-90' : ''}`}
              fill="none"
              viewBox="0 0 24 24"
              strokeWidth={2}
              stroke="currentColor"
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
            </svg>
            Completed ({completedProjects.length})
          </button>

          {showCompleted && (
            <div className="mt-0.5 space-y-0.5">
              {completedProjects.map(project => {
                const route = `/projects/${project.id}`;
                const isActive = pathname === route;

                return (
                  <Link key={project.id} href={route} onClick={onNavigate}>
                    <div
                      className={`flex items-center gap-3 rounded-lg px-3 py-1.5 transition-colors ${
                        isActive
                          ? 'bg-blue-50 text-blue-700'
                          : 'text-gray-500 hover:bg-gray-100'
                      }`}
                    >
                      <div className="text-sm">📁</div>
                      <span className="truncate text-sm font-medium line-through">
                        {project.name}
                      </span>
                    </div>
                  </Link>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
