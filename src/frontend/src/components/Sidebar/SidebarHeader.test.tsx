import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import SidebarHeader from './SidebarHeader';

describe('SidebarHeader Component', () => {
  it('renders application logo and name', () => {
    render(<SidebarHeader />);
    expect(screen.getByText('GTD Todo')).toBeInTheDocument();
    expect(screen.getByText('Getting Things Done')).toBeInTheDocument();
  });

  it('does not render user information', () => {
    const { container } = render(<SidebarHeader />);
    const userCard = container.querySelector('.bg-gray-50');
    expect(userCard).not.toBeInTheDocument();
  });

  it('has border-bottom separator', () => {
    const { container } = render(<SidebarHeader />);
    const header = container.firstChild as HTMLElement;
    expect(header).toHaveClass('border-b', 'border-gray-200');
  });

  it('has proper padding', () => {
    const { container } = render(<SidebarHeader />);
    const header = container.firstChild as HTMLElement;
    expect(header).toHaveClass('px-6', 'py-6');
  });

  it('has app title with blue color', () => {
    const { container } = render(<SidebarHeader />);
    const title = container.querySelector('.text-blue-600');
    expect(title).toBeInTheDocument();
    expect(title).toHaveTextContent('GTD Todo');
  });

  it('has subtitle with gray color', () => {
    const { container } = render(<SidebarHeader />);
    const subtitle = container.querySelector('.text-gray-500');
    expect(subtitle).toBeInTheDocument();
    expect(subtitle).toHaveTextContent('Getting Things Done');
  });
});
