'use client';

import { useState, useCallback } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import SystemListItem from './SystemListItem';
import ProjectListSection from './ProjectListSection';
import LabelListSection from './LabelListSection';
import { useSystemListCounts } from '@/hooks/useSystemListCounts';
import { useProjects } from '@/hooks/useProjects';
import { useLabels } from '@/hooks/useLabels';
import { useTaskRefresh } from '@/hooks/useTaskRefresh';
import { useProjectModalContext } from '@/contexts/ProjectModalContext';
import { SystemList } from '@/types';

interface SidebarNavigationProps {
  onNavigate?: () => void;
}

/**
 * SidebarNavigation Component
 * Scrollable navigation area with system lists (Inbox, Next, Upcoming, Someday),
 * projects with task counts, and labels placeholder.
 */
export default function SidebarNavigation({ onNavigate }: SidebarNavigationProps) {
  const pathname = usePathname();
  const [refreshCounter, setRefreshCounter] = useState(0);
  const { counts, isLoading } = useSystemListCounts(refreshCounter);
  const { projects, isLoading: projectsLoading } = useProjects(refreshCounter);
  const { labels, isLoading: labelsLoading } = useLabels(refreshCounter);
  const { openCreateModal } = useProjectModalContext();

  // Re-fetch counts whenever any task action triggers a global refresh
  useTaskRefresh('sidebar-counts', useCallback(() => {
    setRefreshCounter(prev => prev + 1);
  }, []));

  // Define system lists with their icons
  const systemLists = [
    { list: SystemList.Inbox, icon: '📥', label: 'Inbox' },
    { list: SystemList.Next, icon: '⭐', label: 'Next' },
    { list: SystemList.Upcoming, icon: '📅', label: 'Upcoming' },
    { list: SystemList.Someday, icon: '🔮', label: 'Someday' },
  ];

  return (
    <nav className="flex-1 overflow-y-auto px-4 py-4">
      {/* System Lists Section */}
      <div className="space-y-2">
        <div className="text-xs font-semibold uppercase tracking-wide text-gray-500">
          System Lists
        </div>

        {/* System list items */}
        <div className="space-y-1">
          {systemLists.map(({ list, icon }) => (
            <SystemListItem
              key={list}
              systemList={list}
              icon={icon}
              count={isLoading ? 0 : counts[list] || 0}
              hideCount={list === SystemList.Upcoming}
              onNavigate={onNavigate}
            />
          ))}
        </div>
      </div>

      {/* Completed/Archive Section */}
      <div className="mt-4 border-t border-gray-200 pt-4">
        <Link href="/archive" onClick={onNavigate}>
          <div
            className={`flex items-center justify-between rounded-lg px-3 py-2 transition-colors ${
              pathname === '/archive'
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-100'
            }`}
          >
            <div className="flex items-center gap-3">
              <div className="text-xl">✓</div>
              <span className="text-sm font-medium">Completed</span>
            </div>
          </div>
        </Link>
      </div>

      {/* Projects Section */}
      <ProjectListSection
        projects={projects}
        isLoading={projectsLoading}
        onNavigate={onNavigate}
        onAddProject={openCreateModal}
      />

      {/* Labels Section */}
      <LabelListSection
        labels={labels}
        isLoading={labelsLoading}
        onNavigate={onNavigate}
      />
    </nav>
  );
}
