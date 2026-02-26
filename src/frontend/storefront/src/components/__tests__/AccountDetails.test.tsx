import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import AccountDetails from '../../pages/components/Profile/AccountDetails';

describe('AccountDetails', () => {
  const defaultProps = {
    memberSince: '2023-01-15',
  };

  it('renders Account Details title', () => {
    render(<AccountDetails {...defaultProps} />);
    expect(screen.getByText('Account Details')).toBeInTheDocument();
  });

  it('renders Member Since label', () => {
    render(<AccountDetails {...defaultProps} />);
    expect(screen.getByText('Member Since:')).toBeInTheDocument();
  });

  it('renders formatted date', () => {
    render(<AccountDetails {...defaultProps} />);
    // Date is formatted based on locale
    expect(screen.getByText(/1\/15\/2023/)).toBeInTheDocument();
  });

  it('renders Account Status', () => {
    render(<AccountDetails {...defaultProps} />);
    expect(screen.getByText('Account Status:')).toBeInTheDocument();
  });

  it('renders Active status', () => {
    render(<AccountDetails {...defaultProps} />);
    expect(screen.getByText('Active')).toBeInTheDocument();
  });
});
