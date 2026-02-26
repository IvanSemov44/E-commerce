import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ProfileHeader from '../../pages/components/Profile/ProfileHeader';

describe('ProfileHeader', () => {
  const defaultProps = {
    isEditMode: false,
    onEditClick: vi.fn(),
  };

  it('renders Profile Information title', () => {
    render(<ProfileHeader {...defaultProps} />);
    expect(screen.getByText('Profile Information')).toBeInTheDocument();
  });

  it('renders Edit Profile button when not in edit mode', () => {
    render(<ProfileHeader {...defaultProps} isEditMode={false} />);
    expect(screen.getByText('Edit Profile')).toBeInTheDocument();
  });

  it('does not render Edit Profile button when in edit mode', () => {
    render(<ProfileHeader {...defaultProps} isEditMode={true} />);
    expect(screen.queryByText('Edit Profile')).not.toBeInTheDocument();
  });

  it('calls onEditClick when Edit Profile button is clicked', () => {
    const onEditClick = vi.fn();
    render(<ProfileHeader {...defaultProps} onEditClick={onEditClick} />);
    
    fireEvent.click(screen.getByText('Edit Profile'));
    expect(onEditClick).toHaveBeenCalledTimes(1);
  });
});
