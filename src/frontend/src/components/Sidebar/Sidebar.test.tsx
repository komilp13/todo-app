import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import Sidebar from './Sidebar';
import { SidebarProvider } from '@/contexts/SidebarContext';

// Mock the child components
jest.mock('./SidebarHeader', () => {
  return function MockSidebarHeader() {
    return <div data-testid="sidebar-header">Header</div>;
  };
});

jest.mock('./SidebarNavigation', () => {
  return function MockSidebarNavigation() {
    return <div data-testid="sidebar-navigation">Navigation</div>;
  };
});

jest.mock('./SidebarFooter', () => {
  return function MockSidebarFooter() {
    return <div data-testid="sidebar-footer">Footer</div>;
  };
});

// Mock hooks
jest.mock('@/hooks/useSidebarState', () => ({
  useSidebarState: () => ({
    isCollapsed: false,
    setIsCollapsed: jest.fn(),
    toggleCollapse: jest.fn(),
    isLoaded: true,
  }),
}));

jest.mock('@/hooks/useWindowSize', () => ({
  useWindowSize: () => ({
    width: 1200,
    height: 800,
  }),
}));

const renderWithProviders = (component: React.ReactElement) => {
  return render(<SidebarProvider>{component}</SidebarProvider>);
};

describe('Sidebar Component', () => {
  it('renders without crashing', () => {
    renderWithProviders(<Sidebar />);
    expect(screen.getByTestId('sidebar-header')).toBeInTheDocument();
    expect(screen.getByTestId('sidebar-navigation')).toBeInTheDocument();
    expect(screen.getByTestId('sidebar-footer')).toBeInTheDocument();
  });

  it('renders all three sub-components in desktop mode', () => {
    renderWithProviders(<Sidebar />);
    expect(screen.getByText('Header')).toBeInTheDocument();
    expect(screen.getByText('Navigation')).toBeInTheDocument();
    expect(screen.getByText('Footer')).toBeInTheDocument();
  });

  it('has correct layout structure with flex column in desktop mode', () => {
    const { container } = renderWithProviders(<Sidebar />);
    const sidebar = container.querySelector('aside:not(.fixed)');
    expect(sidebar).toHaveClass('flex', 'flex-col');
  });

  it('has fixed width on desktop mode', () => {
    const { container } = renderWithProviders(<Sidebar />);
    const sidebar = container.querySelector('aside:not(.fixed)');
    expect(sidebar).toHaveClass('w-80');
  });

  it('has border and white background', () => {
    const { container } = renderWithProviders(<Sidebar />);
    const sidebar = container.querySelector('aside:not(.fixed)');
    expect(sidebar).toHaveClass('border-r', 'border-gray-200', 'bg-white');
  });
});
