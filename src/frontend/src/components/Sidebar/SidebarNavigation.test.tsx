import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import SidebarNavigation from './SidebarNavigation';
import { useSystemListCounts } from '@/hooks/useSystemListCounts';
import { useProjects } from '@/hooks/useProjects';
import { SystemList, ProjectStatus } from '@/types';

// Mock the hooks
jest.mock('@/hooks/useSystemListCounts', () => ({
  useSystemListCounts: jest.fn(),
}));

jest.mock('@/hooks/useProjects', () => ({
  useProjects: jest.fn(),
}));

jest.mock('@/hooks/useTaskRefresh', () => ({
  useTaskRefresh: jest.fn(),
}));

jest.mock('@/contexts/ProjectModalContext', () => ({
  useProjectModalContext: jest.fn(() => ({
    openCreateModal: jest.fn(),
    openEditModal: jest.fn(),
    closeModal: jest.fn(),
    isOpen: false,
    editingProject: null,
  })),
}));

// Mock Next.js usePathname hook
jest.mock('next/navigation', () => ({
  usePathname: jest.fn(),
}));

const mockUseSystemListCounts = useSystemListCounts as jest.MockedFunction<
  typeof useSystemListCounts
>;

const mockUseProjects = useProjects as jest.MockedFunction<typeof useProjects>;

import { usePathname } from 'next/navigation';

const mockUsePathname = usePathname as jest.MockedFunction<typeof usePathname>;

const defaultCounts = {
  counts: {
    [SystemList.Inbox]: 0,
    [SystemList.Next]: 0,
    [SystemList.Upcoming]: 0,
    [SystemList.Someday]: 0,
  },
  isLoading: false,
  error: null,
  refetch: jest.fn(),
};

const defaultProjects = {
  projects: [],
  isLoading: false,
  error: null,
  refetch: jest.fn(),
};

describe('SidebarNavigation Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockUsePathname.mockReturnValue('/inbox');
    mockUseProjects.mockReturnValue(defaultProjects);
  });

  it('renders all four system lists', () => {
    mockUseSystemListCounts.mockReturnValue({
      ...defaultCounts,
      counts: {
        [SystemList.Inbox]: 5,
        [SystemList.Next]: 3,
        [SystemList.Upcoming]: 2,
        [SystemList.Someday]: 1,
      },
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('Inbox')).toBeInTheDocument();
    expect(screen.getByText('Next')).toBeInTheDocument();
    expect(screen.getByText('Upcoming')).toBeInTheDocument();
    expect(screen.getByText('Someday')).toBeInTheDocument();
  });

  it('displays task counts for each system list', () => {
    mockUseSystemListCounts.mockReturnValue({
      ...defaultCounts,
      counts: {
        [SystemList.Inbox]: 5,
        [SystemList.Next]: 3,
        [SystemList.Upcoming]: 2,
        [SystemList.Someday]: 0,
      },
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    // Upcoming count is hidden
    expect(screen.getByText('0')).toBeInTheDocument();
  });

  it('displays system list icons', () => {
    mockUseSystemListCounts.mockReturnValue(defaultCounts);

    render(<SidebarNavigation />);

    expect(screen.getByText('📥')).toBeInTheDocument();
    expect(screen.getByText('⭐')).toBeInTheDocument();
    expect(screen.getByText('📅')).toBeInTheDocument();
    expect(screen.getByText('🔮')).toBeInTheDocument();
  });

  it('shows System Lists section header', () => {
    mockUseSystemListCounts.mockReturnValue(defaultCounts);

    render(<SidebarNavigation />);

    expect(screen.getByText('System Lists')).toBeInTheDocument();
  });

  it('shows Projects section header with empty state when no projects', () => {
    mockUseSystemListCounts.mockReturnValue(defaultCounts);
    mockUseProjects.mockReturnValue(defaultProjects);

    render(<SidebarNavigation />);

    expect(screen.getByText('Projects')).toBeInTheDocument();
    expect(screen.getByText('Create your first project')).toBeInTheDocument();
  });

  it('shows active projects in sidebar', () => {
    mockUseSystemListCounts.mockReturnValue(defaultCounts);
    mockUseProjects.mockReturnValue({
      ...defaultProjects,
      projects: [
        {
          id: '1',
          name: 'Project Alpha',
          status: ProjectStatus.Active,
          sortOrder: 0,
          totalTaskCount: 5,
          completedTaskCount: 2,
          completionPercentage: 40,
          createdAt: '2026-01-01',
          updatedAt: '2026-01-01',
        },
        {
          id: '2',
          name: 'Project Beta',
          status: ProjectStatus.Active,
          sortOrder: 1,
          totalTaskCount: 3,
          completedTaskCount: 0,
          completionPercentage: 0,
          createdAt: '2026-01-01',
          updatedAt: '2026-01-01',
        },
      ],
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('Project Alpha')).toBeInTheDocument();
    expect(screen.getByText('Project Beta')).toBeInTheDocument();
    // Open task counts: 5-2=3 for Alpha, 3-0=3 for Beta
    expect(screen.getAllByText('3')).toHaveLength(2);
  });

  it('shows placeholder text for Labels section', () => {
    mockUseSystemListCounts.mockReturnValue(defaultCounts);

    render(<SidebarNavigation />);

    expect(screen.getByText('Labels')).toBeInTheDocument();
    expect(screen.getByText('Labels coming soon...')).toBeInTheDocument();
  });

  it('shows zero counts when loading', () => {
    mockUseSystemListCounts.mockReturnValue({
      ...defaultCounts,
      isLoading: true,
    });

    render(<SidebarNavigation />);

    const countElements = screen.getAllByText('0');
    expect(countElements.length).toBeGreaterThan(0);
  });

  it('handles counts of different sizes', () => {
    mockUseSystemListCounts.mockReturnValue({
      ...defaultCounts,
      counts: {
        [SystemList.Inbox]: 100,
        [SystemList.Next]: 0,
        [SystemList.Upcoming]: 1,
        [SystemList.Someday]: 999,
      },
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('100')).toBeInTheDocument();
    expect(screen.getByText('999')).toBeInTheDocument();
  });

  it('is scrollable', () => {
    mockUseSystemListCounts.mockReturnValue(defaultCounts);

    const { container } = render(<SidebarNavigation />);
    const nav = container.querySelector('nav');

    expect(nav).toHaveClass('overflow-y-auto');
  });
});
