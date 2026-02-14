'use client';

import { ReactNode } from 'react';
import ContentHeader from './ContentHeader';

interface MainContentProps {
  children: ReactNode;
}

/**
 * MainContent Component
 * Provides the flexible content area with header bar and scrollable content
 */
export default function MainContent({ children }: MainContentProps) {
  return (
    <div className="flex flex-1 flex-col overflow-hidden">
      {/* Header Bar */}
      <ContentHeader />

      {/* Scrollable Content Area */}
      <main className="flex-1 overflow-y-auto overflow-x-hidden bg-white">
        <div className="px-6 py-4">
          {children}
        </div>
      </main>
    </div>
  );
}
