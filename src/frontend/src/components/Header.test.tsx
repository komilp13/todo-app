import { render, screen } from '@/test-utils';
import Header from './Header';

describe('Header Component', () => {
  it('renders with correct title', () => {
    render(<Header title="Test Title" />);
    const heading = screen.getByRole('heading', { level: 1 });
    expect(heading).toBeInTheDocument();
    expect(heading).toHaveTextContent('Test Title');
  });

  it('applies correct styling classes', () => {
    const { container } = render(<Header title="Styled Header" />);
    const header = container.querySelector('header');
    expect(header).toHaveClass('bg-blue-600', 'text-white', 'p-4');
  });
});
