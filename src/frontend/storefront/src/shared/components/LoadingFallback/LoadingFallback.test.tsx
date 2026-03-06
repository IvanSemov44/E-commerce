import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import LoadingFallback from './LoadingFallback';

describe('LoadingFallback', () => {
  it('renders default message', () => {
    render(<LoadingFallback />);
    expect(screen.getByText('Loading page...')).toBeInTheDocument();
  });

  it('renders custom message', () => {
    render(<LoadingFallback message="Loading products..." />);
    expect(screen.getByText('Loading products...')).toBeInTheDocument();
  });

  it('renders spinner as aria-hidden', () => {
    const { container } = render(<LoadingFallback />);
    const spinner = container.querySelector('[aria-hidden="true"]');
    expect(spinner).toBeInTheDocument();
  });

  it('renders exactly one message paragraph', () => {
    const { container } = render(<LoadingFallback />);
    const messageParagraphs = container.querySelectorAll('p');
    expect(messageParagraphs).toHaveLength(1);
  });
});
