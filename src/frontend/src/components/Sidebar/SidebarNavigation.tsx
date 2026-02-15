'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import SystemListItem from './SystemListItem';
import { useSystemListCounts } from '@/hooks/useSystemListCounts';
import { SystemList } from '@/types';

interface SidebarNavigationProps {
  onNavigate?: () => void;
}

/**
 * SidebarNavigation Component
 * Scrollable navigation area with system lists (Inbox, Next, Upcoming, Someday)
 * Each list shows open task count and highlights active list
 */
export default function SidebarNavigation({ onNavigate }: SidebarNavigationProps) {
  const pathname = usePathname();
  const { counts, isLoading } = useSystemListCounts();

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

      {/* Future sections: Projects and Labels coming in later stories */}
      <div className="mt-6 space-y-2">
        <div className="text-xs font-semibold uppercase tracking-wide text-gray-500">
          Projects
        </div>
        <p className="text-sm text-gray-400">
          Projects coming soon...
        </p>
      </div>

      <div className="mt-6 space-y-2">
        <div className="text-xs font-semibold uppercase tracking-wide text-gray-500">
          Labels
        </div>
        <p className="text-sm text-gray-400">
          Labels coming soon...
        </p>
      </div>
    </nav>
  );
}
