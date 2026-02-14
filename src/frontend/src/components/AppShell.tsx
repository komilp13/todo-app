'use client';

import { ReactNode } from 'react';
import Sidebar from './Sidebar/Sidebar';
import MainContent from './MainContent';

interface AppShellProps {
  children: ReactNode;
}

/**
 * AppShell Layout Component
 * Divides the screen into a fixed-width sidebar (280px) and flexible content area
 * Used for all authenticated pages in the application
 */
export default function AppShell({ children }: AppShellProps) {
  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar - Fixed width */}
      <Sidebar />

      {/* Main Content Area - Flexible width */}
      <MainContent>
        {children}
      </MainContent>
    </div>
  );
}
