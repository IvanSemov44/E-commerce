import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router';
import { HeaderUserMenu } from './HeaderUserMenu';

type MockUser = { firstName: string; email: string } | null;

let mockUser: MockUser = null;

vi.mock('@/shared/lib/store', () => ({
  useAppSelector: () => mockUser,
}));

vi.mock('@/shared/components/icons', () => ({
  ChevronDownIcon: () => null,
  UserIcon: () => null,
  LogoutIcon: () => null,
}));

const defaultProps = {
  isOpen: false,
  onToggle: vi.fn(),
  onClose: vi.fn(),
  onLogout: vi.fn(),
};

describe('HeaderUserMenu', () => {
  beforeEach(() => {
    mockUser = null;
  });

  it('renders user avatar initial', () => {
    mockUser = { firstName: 'John', email: 'john@test.com' };
    render(
      <BrowserRouter>
        <HeaderUserMenu {...defaultProps} />
      </BrowserRouter>
    );
    // avatar shows first letter, name span also contains 'J'
    expect(screen.getAllByText(/J/).length).toBeGreaterThanOrEqual(1);
  });

  it('shows dropdown with account label when open', () => {
    mockUser = { firstName: 'John', email: 'john@test.com' };
    render(
      <BrowserRouter>
        <HeaderUserMenu {...defaultProps} isOpen={true} />
      </BrowserRouter>
    );
    expect(screen.getByText(/Account/)).toBeInTheDocument();
  });

  it('shows user email in open dropdown', () => {
    mockUser = { firstName: 'John', email: 'john@test.com' };
    render(
      <BrowserRouter>
        <HeaderUserMenu {...defaultProps} isOpen={true} />
      </BrowserRouter>
    );
    expect(screen.getByText('john@test.com')).toBeInTheDocument();
  });

  it('does not render dropdown when closed', () => {
    mockUser = { firstName: 'John', email: 'john@test.com' };
    render(
      <BrowserRouter>
        <HeaderUserMenu {...defaultProps} isOpen={false} />
      </BrowserRouter>
    );
    expect(screen.queryByText('john@test.com')).not.toBeInTheDocument();
  });

  it('falls back to U avatar when no user', () => {
    mockUser = null;
    render(
      <BrowserRouter>
        <HeaderUserMenu {...defaultProps} />
      </BrowserRouter>
    );
    expect(screen.getByText('U')).toBeInTheDocument();
  });
});
