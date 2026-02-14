import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import AppShell from './AppShell';

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

describe('AppShell Component', () => {
  it('renders without crashing', () => {
    render(
      <AppShell>
        <div>Test Content</div>
      </AppShell>
    );
    expect(screen.getByTestId('sidebar')).toBeInTheDocument();
    expect(screen.getByTestId('main-content')).toBeInTheDocument();
  });

  it('renders children in the main content area', () => {
    const testContent = 'Test Page Content';
    render(
      <AppShell>
        <div>{testContent}</div>
      </AppShell>
    );
    expect(screen.getByText(testContent)).toBeInTheDocument();
  });

  it('has proper layout structure with flex container', () => {
    const { container } = render(
      <AppShell>
        <div>Content</div>
      </AppShell>
    );
    const appShellDiv = container.firstChild as HTMLElement;
    expect(appShellDiv).toHaveClass('flex', 'h-screen', 'bg-gray-50');
  });

  it('renders sidebar and content in correct order', () => {
    const { container } = render(
      <AppShell>
        <div>Content</div>
      </AppShell>
    );
    const children = Array.from(container.querySelector('.flex')?.children || []);
    expect(children[0]).toHaveAttribute('data-testid', 'sidebar');
    expect(children[1]).toHaveAttribute('data-testid', 'main-content');
  });

  it('takes full screen height', () => {
    const { container } = render(
      <AppShell>
        <div>Content</div>
      </AppShell>
    );
    const appShellDiv = container.firstChild as HTMLElement;
    expect(appShellDiv).toHaveClass('h-screen');
  });
});
