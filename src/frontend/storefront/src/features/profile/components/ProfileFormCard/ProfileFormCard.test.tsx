import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import ProfileFormCard from './ProfileFormCard';

const mockSetIsEditMode = vi.fn();
const mockHandleCancel = vi.fn();
const mockAction = vi.fn();

const defaultHookValues = {
  profile: undefined,
  values: {
    firstName: 'Jane',
    lastName: 'Smith',
    email: 'jane@example.com',
    phone: '',
    avatarUrl: '',
  },
  fieldErrors: {},
  isEditMode: false,
  isLoading: false,
  isPending: false,
  error: undefined,
  handleChange: vi.fn(),
  handleBlur: vi.fn(),
  action: mockAction,
  setIsEditMode: mockSetIsEditMode,
  handleCancel: mockHandleCancel,
};

vi.mock('../../hooks/useProfileForm', () => ({
  useProfileForm: vi.fn(() => defaultHookValues),
}));

describe('ProfileFormCard', () => {
  it('renders form fields', () => {
    renderWithProviders(<ProfileFormCard />);

    expect(screen.getByDisplayValue('Jane')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Smith')).toBeInTheDocument();
  });

  it('renders in read mode by default — no Save/Cancel buttons', () => {
    renderWithProviders(<ProfileFormCard />);

    expect(screen.queryByRole('button', { name: /save/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /cancel/i })).not.toBeInTheDocument();
  });

  it('calls setIsEditMode when Edit button is clicked', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProfileFormCard />);

    await user.click(screen.getByRole('button', { name: /edit/i }));

    expect(mockSetIsEditMode).toHaveBeenCalledWith(true);
  });
});
