'use client';

import { SystemList } from '@/types';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ReactNode } from 'react';
import { formatSystemList } from '@/utils/enumFormatter';

interface SystemListItemProps {
  systemList: SystemList;
  icon: ReactNode;
  count: number;
  hideCount?: boolean;
  onNavigate?: () => void;
}

/**
 * SystemListItem Component
 * Displays a single system list item (Inbox, Next, Upcoming, Someday)
 * with icon, name, task count badge, and active state
 */
export default function SystemListItem({ systemList, icon, count, hideCount = false, onNavigate }: SystemListItemProps) {
  const pathname = usePathname();

  // Determine the route for this system list
  const getRoute = (list: SystemList): string => {
    switch (list) {
      case SystemList.Inbox:
        return '/inbox';
      case SystemList.Next:
        return '/next';
      case SystemList.Upcoming:
        return '/upcoming';
      case SystemList.Someday:
        return '/someday';
      default:
        return '/inbox';
    }
  };

  const route = getRoute(systemList);
  const isActive = pathname === route;

  return (
    <Link href={route} onClick={onNavigate}>
      <div
        className={`flex items-center justify-between rounded-lg px-3 py-2 transition-colors ${
          isActive
            ? 'bg-blue-50 text-blue-700'
            : 'text-gray-700 hover:bg-gray-100'
        }`}
      >
        <div className="flex items-center gap-3">
          {/* Icon */}
          <div className="text-xl">{icon}</div>

          {/* List name */}
          <span className="text-sm font-medium">{formatSystemList(systemList)}</span>
        </div>

        {/* Task count badge */}
        {!hideCount && (
          <div
            className={`inline-flex items-center justify-center rounded-full px-2 py-0.5 text-xs font-semibold ${
              isActive
                ? 'bg-blue-200 text-blue-700'
                : 'bg-gray-200 text-gray-600'
            }`}
          >
            {count}
          </div>
        )}
      </div>
    </Link>
  );
}
