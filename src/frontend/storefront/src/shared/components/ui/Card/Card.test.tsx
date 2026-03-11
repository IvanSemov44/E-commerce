import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Card } from '../Card';

describe('Card', () => {
  it('renders card with children', () => {
    render(<Card>Card content</Card>);
    expect(screen.getByText('Card content')).toBeInTheDocument();
  });

  it('applies default variant', () => {
    const { container } = render(<Card>Content</Card>);
    const card = container.querySelector('div[class*="card"]');
    expect(card?.className).toMatch(/cardDefault/);
  });

  it('applies bordered variant', () => {
    const { container } = render(<Card variant="bordered">Bordered</Card>);
    const card = container.querySelector('div[class*="card"]');
    expect(card?.className).toMatch(/cardBordered/);
  });

  it('applies elevated variant', () => {
    const { container } = render(<Card variant="elevated">Elevated</Card>);
    const card = container.querySelector('div[class*="card"]');
    expect(card?.className).toMatch(/cardElevated/);
  });

  it('applies ghost variant', () => {
    const { container } = render(<Card variant="ghost">Ghost</Card>);
    const card = container.querySelector('div[class*="card"]');
    expect(card?.className).toMatch(/cardGhost/);
  });

  it('applies default padding', () => {
    const { container } = render(<Card>Content</Card>);
    const card = container.querySelector('div[class*="card"]');
    expect(card?.className).toMatch(/paddingMedium/);
  });

  it('applies small padding', () => {
    const { container } = render(<Card padding="sm">Content</Card>);
    const card = container.querySelector('div[class*="card"]');
    expect(card?.className).toMatch(/paddingSmall/);
  });

  it('applies large padding', () => {
    const { container } = render(<Card padding="lg">Content</Card>);
    const card = container.querySelector('div[class*="card"]');
    expect(card?.className).toMatch(/paddingLarge/);
  });

  it('applies no padding', () => {
    const { container } = render(<Card padding="none">Content</Card>);
    const card = container.querySelector('div[class*="card"]');
    expect(card?.className).toMatch(/paddingNone/);
  });

  it('applies custom className', () => {
    const { container } = render(<Card className="custom-class">Content</Card>);
    const card = container.querySelector('div[class*="card"]');
    expect(card).toHaveClass('custom-class');
  });

  it('combines variant and padding classes', () => {
    const { container } = render(
      <Card variant="bordered" padding="lg">
        Content
      </Card>
    );
    const card = container.querySelector('div[class*="card"]');
    expect(card?.className).toMatch(/cardBordered/);
    expect(card?.className).toMatch(/paddingLarge/);
  });

  it('renders nested elements', () => {
    render(
      <Card>
        <h2>Title</h2>
        <p>Description</p>
      </Card>
    );
    expect(screen.getByRole('heading', { name: /title/i })).toBeInTheDocument();
    expect(screen.getByText('Description')).toBeInTheDocument();
  });

  it('accepts data-testid attribute', () => {
    render(<Card data-testid="card-element">Content</Card>);
    expect(screen.getByTestId('card-element')).toBeInTheDocument();
  });

  it('supports all variants with custom className', () => {
    const { container } = render(
      <Card variant="elevated" padding="sm" className="absolute top-0">
        Content
      </Card>
    );
    const card = container.querySelector('div[class*="card"]');
    expect(card?.className).toMatch(/cardElevated/);
    expect(card?.className).toMatch(/paddingSmall/);
    expect(card).toHaveClass('absolute', 'top-0');
  });

  it('renders as div element', () => {
    const { container } = render(<Card>Content</Card>);
    const card = container.querySelector('div[class*="card"]');
    expect(card?.tagName).toBe('DIV');
  });

  it('accepts click event', () => {
    const handleClick = vi.fn();
    const { container } = render(<Card onClick={handleClick}>Clickable</Card>);
    const card = container.querySelector('div[class*="card"]') as HTMLElement;
    card?.click();
    expect(handleClick).toHaveBeenCalled();
  });
});
