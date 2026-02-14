import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import MainContent from './MainContent';

// Mock ContentHeader component
jest.mock('./ContentHeader', () => {
  return function MockContentHeader() {
    return <header data-testid="content-header">Header</header>;
  };
});

describe('MainContent Component', () => {
  it('renders without crashing', () => {
    render(
      <MainContent>
        <div>Test Content</div>
      </MainContent>
    );
    expect(screen.getByTestId('content-header')).toBeInTheDocument();
    expect(screen.getByText('Test Content')).toBeInTheDocument();
  });

  it('renders children in the main content area', () => {
    const testContent = 'Test Page Content';
    render(
      <MainContent>
        <div>{testContent}</div>
      </MainContent>
    );
    expect(screen.getByText(testContent)).toBeInTheDocument();
  });

  it('renders ContentHeader component', () => {
    render(
      <MainContent>
        <div>Content</div>
      </MainContent>
    );
    expect(screen.getByTestId('content-header')).toBeInTheDocument();
  });

  it('has proper flex column layout structure', () => {
    const { container } = render(
      <MainContent>
        <div>Content</div>
      </MainContent>
    );
    const wrapper = container.firstChild as HTMLElement;
    expect(wrapper).toHaveClass('flex', 'flex-1', 'flex-col', 'overflow-hidden');
  });

  it('renders header and main content in correct order', () => {
    const { container } = render(
      <MainContent>
        <div>Main Content</div>
      </MainContent>
    );
    const wrapper = container.querySelector('.flex');
    const children = Array.from(wrapper?.children || []);

    expect(children[0]).toHaveAttribute('data-testid', 'content-header');
    expect(children[1]).toHaveClass('overflow-y-auto');
  });

  it('main content area has scrollable and overflow handling', () => {
    const { container } = render(
      <MainContent>
        <div>Content</div>
      </MainContent>
    );
    const main = container.querySelector('main');
    expect(main).toHaveClass('flex-1', 'overflow-y-auto', 'overflow-x-hidden');
  });

  it('has white background for main content area', () => {
    const { container } = render(
      <MainContent>
        <div>Content</div>
      </MainContent>
    );
    const main = container.querySelector('main');
    expect(main).toHaveClass('bg-white');
  });

  it('children are wrapped with proper padding', () => {
    const { container } = render(
      <MainContent>
        <div>Content</div>
      </MainContent>
    );
    const contentWrapper = container.querySelector('.px-6');
    expect(contentWrapper).toHaveClass('px-6', 'py-4');
    expect(contentWrapper).toHaveTextContent('Content');
  });

  it('takes up flexible width and height in parent layout', () => {
    const { container } = render(
      <MainContent>
        <div>Content</div>
      </MainContent>
    );
    const wrapper = container.firstChild as HTMLElement;
    expect(wrapper).toHaveClass('flex-1');
  });
});
