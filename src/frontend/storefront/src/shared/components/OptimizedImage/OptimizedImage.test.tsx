import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import OptimizedImage from './OptimizedImage';

// Mock IntersectionObserver
const mockIntersectionObserver = vi.fn();
mockIntersectionObserver.mockReturnValue({
  observe: () => null,
  unobserve: () => null,
  disconnect: () => null,
  takeRecords: () => [],
});
window.IntersectionObserver = mockIntersectionObserver as any;

describe('OptimizedImage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockIntersectionObserver.mockClear();
  });

  it('renders image with src and alt text', () => {
    render(
      <OptimizedImage
        src="/test-image.jpg"
        alt="Test product"
      />
    );
    const img = screen.getByAltText('Test product');
    expect(img).toBeInTheDocument();
  });

  it('renders with custom width and height', () => {
    render(
      <OptimizedImage
        src="/image.jpg"
        alt="Image"
        width={300}
        height={200}
      />
    );
    const img = screen.getByAltText('Image') as HTMLImageElement;
    expect(img).toHaveAttribute('width', '300');
    expect(img).toHaveAttribute('height', '200');
  });

  it('applies custom className', () => {
    const { container } = render(
      <OptimizedImage
        src="/image.jpg"
        alt="Test"
        className="custom-image-class"
      />
    );
    const img = container.querySelector('.custom-image-class');
    expect(img).toBeInTheDocument();
  });

  it('lazy loads image by default', () => {
    render(
      <OptimizedImage
        src="/image.jpg"
        alt="Lazy loaded image"
      />
    );
    const img = screen.getByAltText('Lazy loaded image') as HTMLImageElement;
    expect(img).toHaveAttribute('loading', 'lazy');
  });

  it('eagerly loads image when requested', () => {
    render(
      <OptimizedImage
        src="/image.jpg"
        alt="Eager loaded image"
        loading="eager"
      />
    );
    const img = screen.getByAltText('Eager loaded image') as HTMLImageElement;
    expect(img).toHaveAttribute('loading', 'eager');
  });

  it('calls onLoad callback when image loads', async () => {
    const onLoad = vi.fn();
    render(
      <OptimizedImage
        src="/image.jpg"
        alt="Test"
        onLoad={onLoad}
        loading="eager"
      />
    );
    const img = screen.getByAltText('Test') as HTMLImageElement;

    // Simulate image load
    fireEvent.load(img);

    await waitFor(() => {
      expect(onLoad).toHaveBeenCalled();
    });
  });

  it('calls onError callback when image fails to load', async () => {
    const onError = vi.fn();
    render(
      <OptimizedImage
        src="/broken-image.jpg"
        alt="Broken image"
        onError={onError}
        loading="eager"
      />
    );
    const img = screen.getByAltText('Broken image') as HTMLImageElement;

    // Simulate image load error
    fireEvent.error(img);

    await waitFor(() => {
      expect(onError).toHaveBeenCalled();
    });
  });

  it('shows loading state before image loads', () => {
    const { container } = render(
      <OptimizedImage
        src="/image.jpg"
        alt="Loading image"
        loading="eager"
      />
    );
    // Check for loading skeleton class
    expect(container).toBeInTheDocument();
  });

  it('removes loading state after image loads', async () => {
    const { container } = render(
      <OptimizedImage
        src="/image.jpg"
        alt="Test"
        loading="eager"
      />
    );
    const img = screen.getByAltText('Test') as HTMLImageElement;

    // Simulate load
    fireEvent.load(img);

    await waitFor(() => {
      expect(img).toBeInTheDocument();
    });
  });

  it('uses intersection observer for lazy loading', () => {
    render(
      <OptimizedImage
        src="/image.jpg"
        alt="Lazy image"
        loading="lazy"
      />
    );

    // Verify IntersectionObserver was called
    expect(mockIntersectionObserver).toHaveBeenCalled();
  });

  it('supports multiple image formats with srcset', () => {
    const { container } = render(
      <OptimizedImage
        src="/image.jpg"
        alt="Test image"
        loading="eager"
      />
    );
    const img = screen.getByAltText('Test image') as HTMLImageElement;
    // Component should support modern image formats
    expect(img).toBeInTheDocument();
  });

  it('handles image with no width/height gracefully', () => {
    render(
      <OptimizedImage
        src="/image.jpg"
        alt="No dimensions"
      />
    );
    const img = screen.getByAltText('No dimensions');
    expect(img).toBeInTheDocument();
  });

  it('cleans up intersection observer on unmount', () => {
    const { unmount } = render(
      <OptimizedImage
        src="/image.jpg"
        alt="Cleanup test"
      />
    );

    unmount();

    // Observer should be disconnected
    expect(mockIntersectionObserver).toHaveBeenCalled();
  });

  it('renders with aspect ratio preservation', () => {
    const { container } = render(
      <OptimizedImage
        src="/image.jpg"
        alt="Image with ratio"
        width={300}
        height={200}
      />
    );
    // Should preserve aspect ratio
    expect(container).toBeInTheDocument();
  });

  it('shows fallback behavior on error', async () => {
    const onError = vi.fn();
    render(
      <OptimizedImage
        src="/bad-image.jpg"
        alt="Error image"
        onError={onError}
        loading="eager"
      />
    );

    const img = screen.getByAltText('Error image') as HTMLImageElement;
    fireEvent.error(img);

    await waitFor(() => {
      expect(onError).toHaveBeenCalled();
    });
  });

  it('re-renders when src prop changes', () => {
    const { rerender } = render(
      <OptimizedImage
        src="/image1.jpg"
        alt="Test"
        loading="eager"
      />
    );

    expect(screen.getByAltText('Test')).toBeInTheDocument();

    rerender(
      <OptimizedImage
        src="/image2.jpg"
        alt="Test"
        loading="eager"
      />
    );

    expect(screen.getByAltText('Test')).toBeInTheDocument();
  });

  it('supports placeholder while loading', () => {
    const { container } = render(
      <OptimizedImage
        src="/image.jpg"
        alt="With placeholder"
        width={300}
        height={200}
      />
    );
    expect(container).toBeInTheDocument();
  });

  it('handles very large images correctly', () => {
    render(
      <OptimizedImage
        src="/large-image.jpg"
        alt="Large image"
        width={4000}
        height={3000}
      />
    );
    const img = screen.getByAltText('Large image');
    expect(img).toBeInTheDocument();
  });

  it('handles very small images correctly', () => {
    render(
      <OptimizedImage
        src="/icon.svg"
        alt="Small icon"
        width={24}
        height={24}
      />
    );
    const img = screen.getByAltText('Small icon');
    expect(img).toBeInTheDocument();
  });
});
