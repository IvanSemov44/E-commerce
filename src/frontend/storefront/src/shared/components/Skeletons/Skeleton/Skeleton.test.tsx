import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import Skeleton from './Skeleton';

describe('Skeleton', () => {
  it('renders skeleton element', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton).toBeInTheDocument();
  });

  it('applies rectangular variant by default', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/rectangular/);
  });

  it('applies pulse animation by default', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/pulse/);
  });

  it('applies circle variant', () => {
    const { container } = render(<Skeleton variant="circle" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/circle/);
  });

  it('applies text variant', () => {
    const { container } = render(<Skeleton variant="text" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/text/);
  });

  it('applies rounded variant', () => {
    const { container } = render(<Skeleton variant="rounded" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/rounded/);
  });

  it('applies wave animation', () => {
    const { container } = render(<Skeleton animation="wave" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/wave/);
  });

  it('applies none animation', () => {
    const { container } = render(<Skeleton animation="none" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/none/);
  });

  it('applies numeric width as pixels', () => {
    const { container } = render(<Skeleton width={100} />);
    const skeleton = container.querySelector('span[class*="skeleton"]') as HTMLElement;
    expect(skeleton.style.width).toBe('100px');
  });

  it('applies string width as-is', () => {
    const { container } = render(<Skeleton width="50%" />);
    const skeleton = container.querySelector('span[class*="skeleton"]') as HTMLElement;
    expect(skeleton.style.width).toBe('50%');
  });

  it('applies numeric height as pixels', () => {
    const { container } = render(<Skeleton height={50} />);
    const skeleton = container.querySelector('span[class*="skeleton"]') as HTMLElement;
    expect(skeleton.style.height).toBe('50px');
  });

  it('applies string height as-is', () => {
    const { container } = render(<Skeleton height="2rem" />);
    const skeleton = container.querySelector('span[class*="skeleton"]') as HTMLElement;
    expect(skeleton.style.height).toBe('2rem');
  });

  it('applies custom className', () => {
    const { container } = render(<Skeleton className="custom-class" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton).toHaveClass('custom-class');
  });

  it('combines variant, animation, and custom className', () => {
    const { container } = render(<Skeleton variant="circle" animation="wave" className="avatar" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/circle/);
    expect(skeleton?.className).toMatch(/wave/);
    expect(skeleton).toHaveClass('avatar');
  });

  it('has aria-busy attribute', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton).toHaveAttribute('aria-busy', 'true');
  });

  it('has aria-label attribute', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton).toHaveAttribute('aria-label', 'Loading');
  });

  it('renders with width and height combination', () => {
    const { container } = render(<Skeleton width={120} height={120} variant="circle" />);
    const skeleton = container.querySelector('span[class*="skeleton"]') as HTMLElement;
    expect(skeleton.style.width).toBe('120px');
    expect(skeleton.style.height).toBe('120px');
    expect(skeleton?.className).toMatch(/circle/);
  });

  it('supports various unit types', () => {
    const { container } = render(
      <div>
        <Skeleton width="100%" height="100%" />
        <Skeleton width="10rem" height="5rem" />
        <Skeleton width={200} height={100} />
      </div>
    );
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    const first = skeletons[0] as HTMLElement;
    const second = skeletons[1] as HTMLElement;
    const third = skeletons[2] as HTMLElement;

    expect(first?.style.width).toBe('100%');
    expect(second?.style.width).toBe('10rem');
    expect(third?.style.width).toBe('200px');
  });

  it('renders with default width and height when not provided', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('span[class*="skeleton"]') as HTMLElement;
    expect(skeleton.style.width).toBe('100%');
    expect(skeleton.style.height).toBe('1rem');
  });

  it('renders multiple skeletons independently', () => {
    const { container } = render(
      <div>
        <Skeleton variant="text" width="80%" />
        <Skeleton variant="text" width="60%" />
        <Skeleton variant="rounded" width={100} height={40} />
      </div>
    );
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    expect(skeletons).toHaveLength(3);
  });
});
