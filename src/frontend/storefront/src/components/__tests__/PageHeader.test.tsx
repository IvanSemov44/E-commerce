import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import PageHeader from '../PageHeader';

describe('PageHeader', () => {
  describe('Rendering', () => {
    it('renders title correctly', () => {
      render(<PageHeader title="Test Title" />);

      expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
      expect(screen.getByText('Test Title')).toBeInTheDocument();
    });

    it('renders subtitle when provided', () => {
      render(<PageHeader title="Test Title" subtitle="Test subtitle" />);

      expect(screen.getByText('Test subtitle')).toBeInTheDocument();
    });

    it('does not render subtitle when not provided', () => {
      render(<PageHeader title="Test Title" />);

      expect(screen.queryByText('Test subtitle')).not.toBeInTheDocument();
    });
  });

  describe('Accent Element', () => {
    it('renders accent element by default', () => {
      const { container } = render(<PageHeader title="Test Title" />);

      // The accent element is an empty div
      const wrapper = container.querySelector('div');
      expect(wrapper?.children.length).toBeGreaterThan(1);
    });

    it('renders accent element when showAccent is true', () => {
      const { container } = render(
        <PageHeader title="Test Title" showAccent={true} />
      );

      const wrapper = container.querySelector('div');
      expect(wrapper?.children.length).toBeGreaterThan(1);
    });

    it('does not render accent element when showAccent is false', () => {
      render(<PageHeader title="Test Title" showAccent={false} />);

      // Only the h1 should be rendered
      const heading = screen.getByRole('heading', { level: 1 });
      expect(heading.parentElement?.children.length).toBe(1);
    });
  });

  describe('Structure', () => {
    it('renders h1 element', () => {
      render(<PageHeader title="Test Title" />);

      const heading = screen.getByRole('heading', { level: 1 });
      expect(heading.tagName).toBe('H1');
    });

    it('renders paragraph for subtitle', () => {
      render(<PageHeader title="Test Title" subtitle="Test subtitle" />);

      const subtitle = screen.getByText('Test subtitle');
      expect(subtitle.tagName).toBe('P');
    });
  });
});
