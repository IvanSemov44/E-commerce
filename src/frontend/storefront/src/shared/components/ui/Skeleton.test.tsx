import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import Skeleton, { ProductCardSkeleton } from './Skeleton';

describe('Skeleton', () => {
  it('renders skeleton element', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton).toBeInTheDocument();
  });

  it('applies text variant by default', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/text/);
  });

  it('applies circular variant', () => {
    const { container } = render(<Skeleton variant="circular" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/circular/);
  });

  it('applies rectangular variant', () => {
    const { container } = render(<Skeleton variant="rectangular" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/rectangular/);
  });

  it('applies rounded variant', () => {
    const { container } = render(<Skeleton variant="rounded" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/rounded/);
  });

  it('applies pulse animation by default', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/pulse/);
  });

  it('applies wave animation', () => {
    const { container } = render(<Skeleton animation="wave" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/wave/);
  });

  it('applies no animation', () => {
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
    const { container } = render(<Skeleton height="25px" />);
    const skeleton = container.querySelector('span[class*="skeleton"]') as HTMLElement;
    expect(skeleton.style.height).toBe('25px');
  });

  it('applies custom className', () => {
    const { container } = render(<Skeleton className="custom-class" />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton).toHaveClass('custom-class');
  });

  it('combines variant, animation, and custom className', () => {
    const { container } = render(
      <Skeleton variant="circular" animation="wave" className="avatar" />
    );
    const skeleton = container.querySelector('span[class*="skeleton"]');
    expect(skeleton?.className).toMatch(/circular/);
    expect(skeleton?.className).toMatch(/wave/);
    expect(skeleton).toHaveClass('avatar');
  });

  it('renders with width and height', () => {
    const { container } = render(<Skeleton width={120} height={120} variant="circular" />);
    const skeleton = container.querySelector('span[class*="skeleton"]') as HTMLElement;
    expect(skeleton.style.width).toBe('120px');
    expect(skeleton.style.height).toBe('120px');
    expect(skeleton?.className).toMatch(/circular/);
  });

  it('renders ProductCardSkeleton', () => {
    const { container } = render(<ProductCardSkeleton />);
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('ProductCardSkeleton has correct structure', () => {
    const { container } = render(<ProductCardSkeleton />);
    const productCard = container.querySelector('[class*="productCard"]');
    const productImage = container.querySelector('[class*="productImage"]');
    const productContent = container.querySelector('[class*="productContent"]');
    
    expect(productCard).toBeInTheDocument();
    expect(productImage).toBeInTheDocument();
    expect(productContent).toBeInTheDocument();
  });

  it('handles multiple skeleton variants in composition', () => {
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

  it('renders empty className when not provided', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.querySelector('span[class*="skeleton"]');
    // Element should have skeleton classes but not extra custom classes
    expect(skeleton?.className).toMatch(/skeleton/);
  });

  it('supports width and height with various units', () => {
    const { container } = render(
      <div>
        <Skeleton width="100%" height="100%" />
        <Skeleton width="10rem" height="5rem" />
        <Skeleton width={200} height={100} />
      </div>
    );
    const skeletons = container.querySelectorAll('span[class*="skeleton"]');
    const shell1 = skeletons[0] as HTMLElement;
    const shell2 = skeletons[1] as HTMLElement;
    const shell3 = skeletons[2] as HTMLElement;
    
    expect(shell1?.style.width).toBe('100%');
    expect(shell2?.style.width).toBe('10rem');
    expect(shell3?.style.width).toBe('200px');
  });
});
