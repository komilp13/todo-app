'use client';

import { ReactNode } from 'react';
import Sidebar from './Sidebar/Sidebar';
import MainContent from './MainContent';
import MobileMenuButton from './MobileMenuButton';
import TaskCreateModal from './Tasks/TaskCreateModal';
import { useTaskCreateModalContext } from '@/contexts/TaskCreateModalContext';
import { useTaskCreateModal } from '@/hooks/useTaskCreateModal';

interface AppShellProps {
  children: ReactNode;
}

/**
 * AppShell Layout Component
 * Divides the screen into a fixed-width sidebar (280px) and flexible content area
 * Used for all authenticated pages in the application
 *
 * Responsive:
 * - Desktop (>= 1024px): Fixed sidebar with collapse toggle
 * - Mobile (< 1024px): Hidden sidebar with hamburger button toggle
 */
export default function AppShell({ children }: AppShellProps) {
  const { isOpen, closeModal } = useTaskCreateModalContext();

  // Initialize keyboard shortcut listener
  useTaskCreateModal();

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar - Fixed width on desktop, overlay on mobile */}
      <Sidebar />

      {/* Main Content Area - Flexible width */}
      <div className="flex flex-1 flex-col overflow-hidden relative">
        {/* Mobile Menu Button */}
        <MobileMenuButton />

        {/* Main Content */}
        <MainContent>
          {children}
        </MainContent>
      </div>

      {/* Task Creation Modal - Global, opens via "Q" key or button */}
      <TaskCreateModal
        isOpen={isOpen}
        onClose={closeModal}
      />
    </div>
  );
}
