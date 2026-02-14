import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import UserAvatar from './UserAvatar';

describe('UserAvatar Component', () => {
  it('renders user initials', () => {
    render(<UserAvatar displayName="John Doe" />);
    expect(screen.getByText('JD')).toBeInTheDocument();
  });

  it('extracts initials from multiple word names', () => {
    render(<UserAvatar displayName="Mary Jane Watson" />);
    expect(screen.getByText('MJ')).toBeInTheDocument();
  });

  it('handles single word names', () => {
    render(<UserAvatar displayName="Madonna" />);
    expect(screen.getByText('M')).toBeInTheDocument();
  });

  it('converts initials to uppercase', () => {
    render(<UserAvatar displayName="john doe" />);
    expect(screen.getByText('JD')).toBeInTheDocument();
  });

  it('limits initials to 2 characters', () => {
    render(<UserAvatar displayName="John Paul Michael Jones" />);
    expect(screen.getByText('JP')).toBeInTheDocument();
  });

  it('applies medium size by default', () => {
    const { container } = render(<UserAvatar displayName="John Doe" />);
    const avatar = container.firstChild as HTMLElement;
    expect(avatar).toHaveClass('w-8', 'h-8', 'text-sm');
  });

  it('applies small size when specified', () => {
    const { container } = render(
      <UserAvatar displayName="John Doe" size="sm" />
    );
    const avatar = container.firstChild as HTMLElement;
    expect(avatar).toHaveClass('w-6', 'h-6', 'text-xs');
  });

  it('applies large size when specified', () => {
    const { container } = render(
      <UserAvatar displayName="John Doe" size="lg" />
    );
    const avatar = container.firstChild as HTMLElement;
    expect(avatar).toHaveClass('w-10', 'h-10', 'text-base');
  });

  it('has rounded shape', () => {
    const { container } = render(<UserAvatar displayName="John Doe" />);
    const avatar = container.firstChild as HTMLElement;
    expect(avatar).toHaveClass('rounded-full');
  });

  it('has centered text', () => {
    const { container } = render(<UserAvatar displayName="John Doe" />);
    const avatar = container.firstChild as HTMLElement;
    expect(avatar).toHaveClass('flex', 'items-center', 'justify-center');
  });

  it('has white text color', () => {
    const { container } = render(<UserAvatar displayName="John Doe" />);
    const avatar = container.firstChild as HTMLElement;
    expect(avatar).toHaveClass('text-white');
  });

  it('has title attribute with display name', () => {
    const { container } = render(<UserAvatar displayName="John Doe" />);
    const avatar = container.firstChild as HTMLElement;
    expect(avatar).toHaveAttribute('title', 'John Doe');
  });

  it('assigns consistent color for same name', () => {
    const { container: container1 } = render(
      <UserAvatar displayName="John Doe" />
    );
    const avatar1 = container1.firstChild as HTMLElement;
    const color1 = Array.from(avatar1.classList).find((cls) =>
      cls.startsWith('bg-')
    );

    const { container: container2 } = render(
      <UserAvatar displayName="John Doe" />
    );
    const avatar2 = container2.firstChild as HTMLElement;
    const color2 = Array.from(avatar2.classList).find((cls) =>
      cls.startsWith('bg-')
    );

    expect(color1).toBe(color2);
  });

  it('may assign different colors for different names', () => {
    const { container: container1 } = render(
      <UserAvatar displayName="Alice" />
    );
    const avatar1 = container1.firstChild as HTMLElement;
    const color1 = Array.from(avatar1.classList).find((cls) =>
      cls.startsWith('bg-')
    );

    const { container: container2 } = render(
      <UserAvatar displayName="Bob" />
    );
    const avatar2 = container2.firstChild as HTMLElement;
    const color2 = Array.from(avatar2.classList).find((cls) =>
      cls.startsWith('bg-')
    );

    // Different names may have different colors (not guaranteed)
    // We just verify both have a background color
    expect(color1).toBeDefined();
    expect(color2).toBeDefined();
  });
});
