import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import OptimizedImage from '../OptimizedImage';

// Mock IntersectionObserver
const mockObserve = vi.fn();
const mockUnobserve = vi.fn();
const mockDisconnect = vi.fn();

beforeEach(() => {
  mockObserve.mockClear();
  mockUnobserve.mockClear();
  mockDisconnect.mockClear();
  
  window.IntersectionObserver = vi.fn().mockImplementation(() => ({
    observe: mockObserve,
    unobserve: mockUnobserve,
    disconnect: mockDisconnect,
  }));
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe('OptimizedImage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Rendering', () => {
    it('renders with required props', () => {
      render(<OptimizedImage src="/test-image.jpg" alt="Test image" />);

      const img = screen.getByRole('img');
      expect(img).toBeInTheDocument();
      expect(img).toHaveAttribute('alt', 'Test image');
    });

    it('applies custom className', () => {
      render(
        <OptimizedImage
          src="/test-image.jpg"
          alt="Test image"
          className="custom-class"
        />
      );

      const picture = document.querySelector('.custom-class');
      expect(picture).toBeInTheDocument();
    });

    it('sets width and height attributes when provided', () => {
      render(
        <OptimizedImage
          src="/test-image.jpg"
          alt="Test image"
          width={300}
          height={200}
        />
      );

      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('width', '300');
      expect(img).toHaveAttribute('height', '200');
    });

    it('applies aspect ratio style when width and height are provided', () => {
      const { container } = render(
        <OptimizedImage
          src="/test-image.jpg"
          alt="Test image"
          width={300}
          height={200}
        />
      );

      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper.style.aspectRatio).toBe('300 / 200');
    });
  });

  describe('Loading Behavior', () => {
    it('shows loading skeleton initially', () => {
      const { container } = render(
        <OptimizedImage src="/test-image.jpg" alt="Test image" />
      );

      expect(container.querySelector('.skeleton')).toBeInTheDocument();
    });

    it('hides skeleton after image loads', async () => {
      const { container } = render(
        <OptimizedImage src="/test-image.jpg" alt="Test image" />
      );

      const img = screen.getByRole('img');
      fireEvent.load(img);

      await waitFor(() => {
        expect(container.querySelector('.skeleton')).not.toBeInTheDocument();
      });
    });

    it('calls onLoad callback when image loads', async () => {
      const onLoad = vi.fn();

      render(
        <OptimizedImage
          src="/test-image.jpg"
          alt="Test image"
          onLoad={onLoad}
        />
      );

      const img = screen.getByRole('img');
      fireEvent.load(img);

      await waitFor(() => {
        expect(onLoad).toHaveBeenCalledTimes(1);
      });
    });

    it('sets loading attribute to lazy by default', () => {
      render(<OptimizedImage src="/test-image.jpg" alt="Test image" />);

      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('loading', 'lazy');
    });

    it('sets loading attribute to eager when specified', () => {
      render(
        <OptimizedImage
          src="/test-image.jpg"
          alt="Test image"
          loading="eager"
        />
      );

      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('loading', 'eager');
    });

    it('sets src immediately when loading is eager', () => {
      render(
        <OptimizedImage
          src="/test-image.jpg"
          alt="Test image"
          loading="eager"
        />
      );

      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('src', '/test-image.jpg');
    });

    it('does not set src initially when loading is lazy', () => {
      render(
        <OptimizedImage
          src="/test-image.jpg"
          alt="Test image"
          loading="lazy"
        />
      );

      const img = screen.getByRole('img');
      expect(img).not.toHaveAttribute('src', '/test-image.jpg');
    });
  });

  describe('Error Handling', () => {
    it('shows error state when image fails to load', async () => {
      const { container } = render(
        <OptimizedImage src="/missing-image.jpg" alt="Test image" />
      );

      const img = screen.getByRole('img');
      fireEvent.error(img);

      await waitFor(() => {
        expect(screen.getByText('Failed to load image')).toBeInTheDocument();
      });
    });

    it('calls onError callback when image fails to load', async () => {
      const onError = vi.fn();

      render(
        <OptimizedImage
          src="/missing-image.jpg"
          alt="Test image"
          onError={onError}
        />
      );

      const img = screen.getByRole('img');
      fireEvent.error(img);

      await waitFor(() => {
        expect(onError).toHaveBeenCalledTimes(1);
        expect(onError).toHaveBeenCalledWith(expect.any(Error));
      });
    });

    it('hides skeleton when error occurs', async () => {
      const { container } = render(
        <OptimizedImage src="/missing-image.jpg" alt="Test image" />
      );

      const img = screen.getByRole('img');
      fireEvent.error(img);

      await waitFor(() => {
        expect(container.querySelector('.skeleton')).not.toBeInTheDocument();
      });
    });
  });

  describe('Intersection Observer', () => {
    it('creates intersection observer for lazy loading', () => {
      render(
        <OptimizedImage
          src="/test-image.jpg"
          alt="Test image"
          loading="lazy"
        />
      );

      expect(window.IntersectionObserver).toHaveBeenCalled();
    });

    it('does not create intersection observer for eager loading', () => {
      vi.clearAllMocks();
      
      render(
        <OptimizedImage
          src="/test-image.jpg"
          alt="Test image"
          loading="eager"
        />
      );

      expect(window.IntersectionObserver).not.toHaveBeenCalled();
    });

    it('disconnects observer on unmount', () => {
      const { unmount } = render(
        <OptimizedImage
          src="/test-image.jpg"
          alt="Test image"
          loading="lazy"
        />
      );

      unmount();

      expect(mockDisconnect).toHaveBeenCalled();
    });
  });

  describe('Picture Element', () => {
    it('renders picture element with sources', () => {
      render(<OptimizedImage src="/test-image.jpg" alt="Test image" />);

      const picture = document.querySelector('picture');
      expect(picture).toBeInTheDocument();

      const sources = picture?.querySelectorAll('source');
      expect(sources).toHaveLength(2);
    });

    it('includes AVIF source', () => {
      render(<OptimizedImage src="/test-image.jpg" alt="Test image" />);

      const avifSource = document.querySelector('source[type="image/avif"]');
      expect(avifSource).toBeInTheDocument();
      expect(avifSource).toHaveAttribute('srcSet', '/test-image.avif');
    });

    it('includes WebP source', () => {
      render(<OptimizedImage src="/test-image.jpg" alt="Test image" />);

      const webpSource = document.querySelector('source[type="image/webp"]');
      expect(webpSource).toBeInTheDocument();
      expect(webpSource).toHaveAttribute('srcSet', '/test-image.webp');
    });
  });
});
