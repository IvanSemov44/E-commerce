import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { ProfileHeader } from './ProfileHeader';

describe('ProfileHeader', () => {
  it('renders profile title', () => {
    render(<ProfileHeader isEditMode={false} onEditClick={vi.fn()} />);

    expect(screen.getByRole('heading', { level: 2 })).toBeInTheDocument();
    expect(screen.getByText(/profile/i)).toBeInTheDocument();
  });

  it('renders subtitle', () => {
    render(<ProfileHeader isEditMode={false} onEditClick={vi.fn()} />);

    expect(screen.getByText(/manage your account information/i)).toBeInTheDocument();
  });

  it('shows edit button in view mode', () => {
    render(<ProfileHeader isEditMode={false} onEditClick={vi.fn()} />);

    expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument();
  });

  it('hides edit button in edit mode', () => {
    render(<ProfileHeader isEditMode={true} onEditClick={vi.fn()} />);

    expect(screen.queryByRole('button', { name: /edit/i })).not.toBeInTheDocument();
  });

  it('calls onEditClick when edit button clicked', async () => {
    const user = userEvent.setup();
    const onEditClick = vi.fn();

    render(<ProfileHeader isEditMode={false} onEditClick={onEditClick} />);

    const editButton = screen.getByRole('button', { name: /edit/i });
    await user.click(editButton);

    expect(onEditClick).toHaveBeenCalled();
  });
});
