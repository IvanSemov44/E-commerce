import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Card } from '../ui/Card';

describe('Card', () => {
  describe('Rendering', () => {
    it('renders children correctly', () => {
      render(
        <Card>
          <p>Card content</p>
        </Card>
      );

      expect(screen.getByText('Card content')).toBeInTheDocument();
    });

    it('renders as a div element', () => {
      const { container } = render(<Card>Content</Card>);

      expect(container.querySelector('div')).toBeInTheDocument();
    });
  });

  describe('Variants', () => {
    it('applies default variant class by default', () => {
      const { container } = render(<Card>Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/cardDefault/);
    });

    it('applies default variant when specified', () => {
      const { container } = render(<Card variant="default">Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/cardDefault/);
    });

    it('applies bordered variant class', () => {
      const { container } = render(<Card variant="bordered">Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/cardBordered/);
    });

    it('applies elevated variant class', () => {
      const { container } = render(<Card variant="elevated">Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/cardElevated/);
    });

    it('applies ghost variant class', () => {
      const { container } = render(<Card variant="ghost">Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/cardGhost/);
    });
  });

  describe('Padding', () => {
    it('applies medium padding by default', () => {
      const { container } = render(<Card>Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/paddingMedium/);
    });

    it('applies no padding when padding is none', () => {
      const { container } = render(<Card padding="none">Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/paddingNone/);
    });

    it('applies small padding when specified', () => {
      const { container } = render(<Card padding="sm">Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/paddingSmall/);
    });

    it('applies medium padding when specified', () => {
      const { container } = render(<Card padding="md">Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/paddingMedium/);
    });

    it('applies large padding when specified', () => {
      const { container } = render(<Card padding="lg">Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/paddingLarge/);
    });
  });

  describe('Custom ClassName', () => {
    it('applies custom className', () => {
      const { container } = render(<Card className="custom-class">Content</Card>);
      const card = container.querySelector('div');
      
      expect(card?.className).toContain('custom-class');
    });

    it('preserves variant and padding classes with custom className', () => {
      const { container } = render(
        <Card variant="elevated" padding="lg" className="custom-class">
          Content
        </Card>
      );
      const card = container.querySelector('div');
      
      expect(card?.className).toMatch(/cardElevated/);
      expect(card?.className).toMatch(/paddingLarge/);
      expect(card?.className).toContain('custom-class');
    });
  });

  describe('HTML Attributes', () => {
    it('spreads additional props to the div', () => {
      const { container } = render(
        <Card data-testid="test-card" id="my-card">
          Content
        </Card>
      );

      expect(container.querySelector('#my-card')).toBeInTheDocument();
    });

    it('forwards ref correctly', () => {
      const ref = { current: null as HTMLDivElement | null };

      render(<Card ref={ref}>Content</Card>);

      expect(ref.current).toBeInstanceOf(HTMLDivElement);
    });

    it('handles onClick events', () => {
      const handleClick = vi.fn();
      const ref = { current: null as HTMLDivElement | null };

      render(<Card ref={ref} onClick={handleClick}>Content</Card>);

      ref.current?.click();

      expect(handleClick).toHaveBeenCalledTimes(1);
    });
  });

  describe('Display Name', () => {
    it('has correct display name', () => {
      expect(Card.displayName).toBe('Card');
    });
  });
});
