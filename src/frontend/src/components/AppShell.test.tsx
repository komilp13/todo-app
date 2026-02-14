import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import AppShell from './AppShell';
import { SidebarProvider } from '@/contexts/SidebarContext';

// Mock the child components to avoid dependency issues
jest.mock('./Sidebar/Sidebar', () => {
  return function MockSidebar() {
    return <aside data-testid="sidebar">Sidebar</aside>;
  };
});

jest.mock('./MainContent', () => {
  return function MockMainContent({ children }: { children: React.ReactNode }) {
    return <div data-testid="main-content">{children}</div>;
  };
});

jest.mock('./MobileMenuButton', () => {
  return function MockMobileMenuButton() {
    return <div data-testid="mobile-menu" />;
  };
});

const renderWithProviders = (component: React.ReactElement) => {
  return render(<SidebarProvider>{component}</SidebarProvider>);
};

describe('AppShell Component', () => {
  it('renders without crashing', () => {
    renderWithProviders(
      <AppShell>
        <div>Test Content</div>
      </AppShell>
    );
    expect(screen.getByTestId('sidebar')).toBeInTheDocument();
    expect(screen.getByTestId('main-content')).toBeInTheDocument();
  });

  it('renders children in the main content area', () => {
    const testContent = 'Test Page Content';
    renderWithProviders(
      <AppShell>
        <div>{testContent}</div>
      </AppShell>
    );
    expect(screen.getByText(testContent)).toBeInTheDocument();
  });

  it('has proper layout structure with flex container', () => {
    const { container } = renderWithProviders(
      <AppShell>
        <div>Content</div>
      </AppShell>
    );
    const appShellDiv = container.firstChild as HTMLElement;
    expect(appShellDiv).toHaveClass('flex', 'h-screen', 'bg-gray-50');
  });

  it('renders sidebar and content in correct order', () => {
    const { container } = renderWithProviders(
      <AppShell>
        <div>Content</div>
      </AppShell>
    );
    const children = Array.from(container.querySelector('.flex')?.children || []);
    expect(children[0]).toHaveAttribute('data-testid', 'sidebar');
    expect(children[1]).toHaveClass('flex-1');
  });

  it('takes full screen height', () => {
    const { container } = renderWithProviders(
      <AppShell>
        <div>Content</div>
      </AppShell>
    );
    const appShellDiv = container.firstChild as HTMLElement;
    expect(appShellDiv).toHaveClass('h-screen');
  });

  it('renders mobile menu button', () => {
    renderWithProviders(
      <AppShell>
        <div>Content</div>
      </AppShell>
    );
    expect(screen.getByTestId('mobile-menu')).toBeInTheDocument();
  });
});
