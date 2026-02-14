import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import SidebarNavigation from './SidebarNavigation';
import { useSystemListCounts } from '@/hooks/useSystemListCounts';
import { SystemList } from '@/types';

// Mock the hook
jest.mock('@/hooks/useSystemListCounts', () => ({
  useSystemListCounts: jest.fn(),
}));

// Mock Next.js usePathname hook
jest.mock('next/navigation', () => ({
  usePathname: jest.fn(),
}));

const mockUseSystemListCounts = useSystemListCounts as jest.MockedFunction<
  typeof useSystemListCounts
>;

import { usePathname } from 'next/navigation';

const mockUsePathname = usePathname as jest.MockedFunction<typeof usePathname>;

describe('SidebarNavigation Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockUsePathname.mockReturnValue('/inbox');
  });

  it('renders all four system lists', () => {
    mockUseSystemListCounts.mockReturnValue({
      counts: {
        [SystemList.Inbox]: 5,
        [SystemList.Next]: 3,
        [SystemList.Upcoming]: 2,
        [SystemList.Someday]: 1,
      },
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('Inbox')).toBeInTheDocument();
    expect(screen.getByText('Next')).toBeInTheDocument();
    expect(screen.getByText('Upcoming')).toBeInTheDocument();
    expect(screen.getByText('Someday')).toBeInTheDocument();
  });

  it('displays task counts for each system list', () => {
    mockUseSystemListCounts.mockReturnValue({
      counts: {
        [SystemList.Inbox]: 5,
        [SystemList.Next]: 3,
        [SystemList.Upcoming]: 2,
        [SystemList.Someday]: 0,
      },
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('0')).toBeInTheDocument();
  });

  it('displays system list icons', () => {
    mockUseSystemListCounts.mockReturnValue({
      counts: {
        [SystemList.Inbox]: 5,
        [SystemList.Next]: 3,
        [SystemList.Upcoming]: 2,
        [SystemList.Someday]: 1,
      },
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('📥')).toBeInTheDocument();
    expect(screen.getByText('⭐')).toBeInTheDocument();
    expect(screen.getByText('📅')).toBeInTheDocument();
    expect(screen.getByText('🔮')).toBeInTheDocument();
  });

  it('shows System Lists section header', () => {
    mockUseSystemListCounts.mockReturnValue({
      counts: {
        [SystemList.Inbox]: 0,
        [SystemList.Next]: 0,
        [SystemList.Upcoming]: 0,
        [SystemList.Someday]: 0,
      },
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('System Lists')).toBeInTheDocument();
  });

  it('shows placeholder text for Projects section', () => {
    mockUseSystemListCounts.mockReturnValue({
      counts: {
        [SystemList.Inbox]: 0,
        [SystemList.Next]: 0,
        [SystemList.Upcoming]: 0,
        [SystemList.Someday]: 0,
      },
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('Projects')).toBeInTheDocument();
    expect(screen.getByText('Projects coming soon...')).toBeInTheDocument();
  });

  it('shows placeholder text for Labels section', () => {
    mockUseSystemListCounts.mockReturnValue({
      counts: {
        [SystemList.Inbox]: 0,
        [SystemList.Next]: 0,
        [SystemList.Upcoming]: 0,
        [SystemList.Someday]: 0,
      },
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('Labels')).toBeInTheDocument();
    expect(screen.getByText('Labels coming soon...')).toBeInTheDocument();
  });

  it('shows zero counts when loading', () => {
    mockUseSystemListCounts.mockReturnValue({
      counts: {
        [SystemList.Inbox]: 0,
        [SystemList.Next]: 0,
        [SystemList.Upcoming]: 0,
        [SystemList.Someday]: 0,
      },
      isLoading: true,
      error: null,
      refetch: jest.fn(),
    });

    render(<SidebarNavigation />);

    // Check that all counts show 0 while loading
    const countElements = screen.getAllByText('0');
    expect(countElements.length).toBeGreaterThan(0);
  });

  it('handles counts of different sizes', () => {
    mockUseSystemListCounts.mockReturnValue({
      counts: {
        [SystemList.Inbox]: 100,
        [SystemList.Next]: 0,
        [SystemList.Upcoming]: 1,
        [SystemList.Someday]: 999,
      },
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });

    render(<SidebarNavigation />);

    expect(screen.getByText('100')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('999')).toBeInTheDocument();
  });

  it('is scrollable', () => {
    mockUseSystemListCounts.mockReturnValue({
      counts: {
        [SystemList.Inbox]: 5,
        [SystemList.Next]: 3,
        [SystemList.Upcoming]: 2,
        [SystemList.Someday]: 1,
      },
      isLoading: false,
      error: null,
      refetch: jest.fn(),
    });

    const { container } = render(<SidebarNavigation />);
    const nav = container.querySelector('nav');

    expect(nav).toHaveClass('overflow-y-auto');
  });
});
