import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Button } from '../Button';

describe('Button', () => {
  it('renders button with text children', () => {
    render(<Button>Click me</Button>);
    expect(screen.getByRole('button', { name: /click me/i })).toBeInTheDocument();
  });

  it('applies primary variant by default', () => {
    render(<Button>Submit</Button>);
    const button = screen.getByRole('button');
    expect(button.className).toMatch(/buttonPrimary/);
  });

  it('applies secondary variant', () => {
    render(<Button variant="secondary">Cancel</Button>);
    const button = screen.getByRole('button');
    expect(button.className).toMatch(/buttonSecondary/);
  });

  it('applies ghost variant', () => {
    render(<Button variant="ghost">Link</Button>);
    const button = screen.getByRole('button');
    expect(button.className).toMatch(/buttonGhost/);
  });

  it('applies destructive variant', () => {
    render(<Button variant="destructive">Delete</Button>);
    const button = screen.getByRole('button');
    expect(button.className).toMatch(/buttonDestructive/);
  });

  it('applies outline variant', () => {
    render(<Button variant="outline">Outlined</Button>);
    const button = screen.getByRole('button');
    expect(button.className).toMatch(/buttonOutline/);
  });

  it('applies small size', () => {
    render(<Button size="sm">Small</Button>);
    const button = screen.getByRole('button');
    expect(button.className).toMatch(/buttonSmall/);
  });

  it('applies medium size by default', () => {
    render(<Button>Medium</Button>);
    const button = screen.getByRole('button');
    expect(button.className).toMatch(/buttonMedium/);
  });

  it('applies large size', () => {
    render(<Button size="lg">Large</Button>);
    const button = screen.getByRole('button');
    expect(button.className).toMatch(/buttonLarge/);
  });

  it('disables button when disabled prop is true', () => {
    render(<Button disabled>Disabled</Button>);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
  });

  it('disables button when isLoading is true', () => {
    render(<Button isLoading>Submit</Button>);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
  });

  it('shows loading spinner when isLoading is true', () => {
    const { container } = render(<Button isLoading>Submit</Button>);
    const svg = container.querySelector('svg');
    expect(svg).toBeInTheDocument();
  });

  it('calls onClick handler when clicked', async () => {
    const handleClick = vi.fn();
    const user = userEvent.setup();
    render(<Button onClick={handleClick}>Click</Button>);

    await user.click(screen.getByRole('button'));
    expect(handleClick).toHaveBeenCalledOnce();
  });

  it('does not call onClick when disabled', async () => {
    const handleClick = vi.fn();
    render(
      <Button onClick={handleClick} disabled>
        Disabled
      </Button>
    );

    fireEvent.click(screen.getByRole('button'));
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('applies custom className', () => {
    render(<Button className="custom-class">Custom</Button>);
    const button = screen.getByRole('button');
    expect(button).toHaveClass('custom-class');
  });

  it('accepts type attribute', () => {
    render(
      <form>
        <Button type="submit">Submit</Button>
      </form>
    );
    const button = screen.getByRole('button');
    expect(button).toHaveAttribute('type', 'submit');
  });

  it('renders with aria-label', () => {
    render(<Button aria-label="Close dialog">×</Button>);
    const button = screen.getByRole('button', { name: /close dialog/i });
    expect(button).toBeInTheDocument();
  });

  it('combines variant, size, and custom classes', () => {
    render(
      <Button variant="secondary" size="lg" className="custom">
        Combined
      </Button>
    );
    const button = screen.getByRole('button');
    expect(button.className).toMatch(/buttonSecondary/);
    expect(button.className).toMatch(/buttonLarge/);
    expect(button).toHaveClass('custom');
  });
});
