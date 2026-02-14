import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import SystemListItem from './SystemListItem';
import { SystemList } from '@/types';

// Mock Next.js usePathname hook
jest.mock('next/navigation', () => ({
  usePathname: jest.fn(),
}));

import { usePathname } from 'next/navigation';

const mockUsePathname = usePathname as jest.MockedFunction<typeof usePathname>;

describe('SystemListItem Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders system list item with name and icon', () => {
    mockUsePathname.mockReturnValue('/other');

    render(
      <SystemListItem
        systemList={SystemList.Inbox}
        icon="📥"
        count={5}
      />
    );

    expect(screen.getByText('Inbox')).toBeInTheDocument();
    expect(screen.getByText('📥')).toBeInTheDocument();
  });

  it('displays task count badge', () => {
    mockUsePathname.mockReturnValue('/other');

    render(
      <SystemListItem
        systemList={SystemList.Next}
        icon="⭐"
        count={3}
      />
    );

    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('highlights active system list', () => {
    mockUsePathname.mockReturnValue('/inbox');

    const { container } = render(
      <SystemListItem
        systemList={SystemList.Inbox}
        icon="📥"
        count={5}
      />
    );

    const listItem = container.querySelector('div.bg-blue-50');
    expect(listItem).toBeInTheDocument();
  });

  it('does not highlight inactive system list', () => {
    mockUsePathname.mockReturnValue('/next');

    const { container } = render(
      <SystemListItem
        systemList={SystemList.Inbox}
        icon="📥"
        count={5}
      />
    );

    const listItem = container.querySelector('div:not(.bg-blue-50)');
    expect(listItem).toBeInTheDocument();
  });

  it('renders correct link for each system list', () => {
    mockUsePathname.mockReturnValue('/other');

    const { container: inboxContainer } = render(
      <SystemListItem
        systemList={SystemList.Inbox}
        icon="📥"
        count={0}
      />
    );

    const inboxLink = inboxContainer.querySelector('a');
    expect(inboxLink).toHaveAttribute('href', '/inbox');
  });

  it('renders correct link for Next list', () => {
    mockUsePathname.mockReturnValue('/other');

    const { container } = render(
      <SystemListItem
        systemList={SystemList.Next}
        icon="⭐"
        count={0}
      />
    );

    const link = container.querySelector('a');
    expect(link).toHaveAttribute('href', '/next');
  });

  it('renders correct link for Upcoming list', () => {
    mockUsePathname.mockReturnValue('/other');

    const { container } = render(
      <SystemListItem
        systemList={SystemList.Upcoming}
        icon="📅"
        count={0}
      />
    );

    const link = container.querySelector('a');
    expect(link).toHaveAttribute('href', '/upcoming');
  });

  it('renders correct link for Someday list', () => {
    mockUsePathname.mockReturnValue('/other');

    const { container } = render(
      <SystemListItem
        systemList={SystemList.Someday}
        icon="🔮"
        count={0}
      />
    );

    const link = container.querySelector('a');
    expect(link).toHaveAttribute('href', '/someday');
  });

  it('shows zero count', () => {
    mockUsePathname.mockReturnValue('/other');

    render(
      <SystemListItem
        systemList={SystemList.Inbox}
        icon="📥"
        count={0}
      />
    );

    expect(screen.getByText('0')).toBeInTheDocument();
  });

  it('shows large count correctly', () => {
    mockUsePathname.mockReturnValue('/other');

    render(
      <SystemListItem
        systemList={SystemList.Inbox}
        icon="📥"
        count={42}
      />
    );

    expect(screen.getByText('42')).toBeInTheDocument();
  });
});
