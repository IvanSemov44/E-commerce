import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import EmptyState from './EmptyState';

vi.mock('../ui/Card', () => ({
  default: ({
    children,
    variant,
    padding,
  }: {
    children: React.ReactNode;
    variant?: string;
    padding?: string;
  }) => (
    <div data-testid="card" data-variant={variant} data-padding={padding}>
      {children}
    </div>
  ),
}));

describe('EmptyState', () => {
  it('renders title', () => {
    render(<EmptyState title="No products" />);
    expect(screen.getByRole('heading', { name: 'No products' })).toBeInTheDocument();
  });

  it('renders description when provided', () => {
    render(<EmptyState title="No products" description="Try another filter" />);
    expect(screen.getByText('Try another filter')).toBeInTheDocument();
  });

  it('renders icon when provided', () => {
    render(<EmptyState title="No products" icon={<span data-testid="empty-icon">📦</span>} />);
    expect(screen.getByTestId('empty-icon')).toBeInTheDocument();
  });

  it('renders action when provided', () => {
    render(<EmptyState title="No products" action={<button>Reset filters</button>} />);
    expect(screen.getByRole('button', { name: 'Reset filters' })).toBeInTheDocument();
  });

  it('uses Card with bordered variant and large padding', () => {
    render(<EmptyState title="No products" />);
    const card = screen.getByTestId('card');
    expect(card).toHaveAttribute('data-variant', 'bordered');
    expect(card).toHaveAttribute('data-padding', 'lg');
  });

  it('hides optional content when not provided', () => {
    const { container } = render(<EmptyState title="No products" />);
    expect(container.querySelector('p')).not.toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
