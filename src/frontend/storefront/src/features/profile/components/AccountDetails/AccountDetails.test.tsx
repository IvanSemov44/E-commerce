import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { AccountDetails } from './AccountDetails';
import styles from './AccountDetails.module.css';

describe('AccountDetails', () => {
  it('renders account details section', () => {
    render(<AccountDetails memberSince="2023-01-15T10:30:00Z" />);

    expect(screen.getByRole('heading', { level: 3 })).toBeInTheDocument();
  });

  it('displays member since date in locale format', () => {
    render(<AccountDetails memberSince="2023-01-15T10:30:00Z" />);

    expect(screen.getByText(/member since/i)).toBeInTheDocument();
    expect(screen.getByText(/1\/15\/2023/)).toBeInTheDocument();
  });

  it('displays account status as active', () => {
    render(<AccountDetails memberSince="2023-01-15T10:30:00Z" />);

    expect(screen.getByText(/account status/i)).toBeInTheDocument();
    expect(screen.getByText(/active/i)).toBeInTheDocument();
  });

  it('handles invalid date strings', () => {
    render(<AccountDetails memberSince="invalid-date" />);

    expect(screen.getByText(/invalid-date/)).toBeInTheDocument();
  });

  it('renders with proper styling for active status', () => {
    render(<AccountDetails memberSince="2023-01-15T10:30:00Z" />);

    const activeSpan = screen.getByText(/active/i);
    expect(activeSpan).toHaveClass(styles.active);
  });
});
