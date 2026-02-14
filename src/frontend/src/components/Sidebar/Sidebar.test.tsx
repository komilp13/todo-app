import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import Sidebar from './Sidebar';

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

describe('Sidebar Component', () => {
  it('renders without crashing', () => {
    render(<Sidebar />);
    expect(screen.getByTestId('sidebar-header')).toBeInTheDocument();
    expect(screen.getByTestId('sidebar-navigation')).toBeInTheDocument();
    expect(screen.getByTestId('sidebar-footer')).toBeInTheDocument();
  });

  it('renders all three sub-components', () => {
    render(<Sidebar />);
    expect(screen.getByText('Header')).toBeInTheDocument();
    expect(screen.getByText('Navigation')).toBeInTheDocument();
    expect(screen.getByText('Footer')).toBeInTheDocument();
  });

  it('has correct layout structure with flex column', () => {
    const { container } = render(<Sidebar />);
    const sidebar = container.querySelector('aside');
    expect(sidebar).toHaveClass('flex', 'flex-col', 'w-80', 'border-r', 'border-gray-200', 'bg-white');
  });

  it('renders components in correct order: header, navigation, footer', () => {
    const { container } = render(<Sidebar />);
    const sidebar = container.querySelector('aside');
    const children = Array.from(sidebar?.children || []);

    expect(children[0]).toHaveAttribute('data-testid', 'sidebar-header');
    expect(children[1]).toHaveAttribute('data-testid', 'sidebar-navigation');
    expect(children[2]).toHaveAttribute('data-testid', 'sidebar-footer');
  });

  it('has fixed width and right border', () => {
    const { container } = render(<Sidebar />);
    const sidebar = container.querySelector('aside');
    expect(sidebar).toHaveClass('w-80');
    expect(sidebar).toHaveClass('border-r', 'border-gray-200');
  });

  it('has white background', () => {
    const { container } = render(<Sidebar />);
    const sidebar = container.querySelector('aside');
    expect(sidebar).toHaveClass('bg-white');
  });
});
