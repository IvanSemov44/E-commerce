import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router';
import HeaderUserMenu from './HeaderUserMenu';

const defaultProps = {
  user: null,
  isOpen: false,
  onToggle: vi.fn(),
  onClose: vi.fn(),
  onLogout: vi.fn(),
};

describe('HeaderUserMenu', () => {
  it('renders user avatar', () => {
    const user = { firstName: 'John', email: 'john@test.com', role: 'Customer' };
    render(
      <BrowserRouter>
        <HeaderUserMenu {...defaultProps} user={user} />
      </BrowserRouter>
    );
    expect(screen.getAllByText(/J/)).toHaveLength(2);
  });

  it('shows dropdown when open', () => {
    const user = { firstName: 'John', email: 'john@test.com', role: 'Customer' };
    render(
      <BrowserRouter>
        <HeaderUserMenu {...defaultProps} user={user} isOpen={true} />
      </BrowserRouter>
    );
    expect(screen.getByText(/Account/)).toBeInTheDocument();
  });
});
