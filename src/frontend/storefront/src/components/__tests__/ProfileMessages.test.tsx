import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import ProfileMessages from '../../pages/components/Profile/ProfileMessages';

describe('ProfileMessages', () => {
  const defaultProps = {
    successMessage: '',
    errorMessage: '',
  };

  it('renders nothing when no messages are present', () => {
    const { container } = render(<ProfileMessages {...defaultProps} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders success message when provided', () => {
    render(<ProfileMessages {...defaultProps} successMessage="Profile updated successfully!" />);
    expect(screen.getByText('Profile updated successfully!')).toBeInTheDocument();
  });

  it('renders error message when provided', () => {
    render(<ProfileMessages {...defaultProps} errorMessage="Failed to update profile" />);
    expect(screen.getByText('Failed to update profile')).toBeInTheDocument();
  });

  it('renders both success and error messages when provided', () => {
    render(
      <ProfileMessages
        successMessage="Profile updated!"
        errorMessage="But something else happened"
      />
    );
    expect(screen.getByText('Profile updated!')).toBeInTheDocument();
    expect(screen.getByText('But something else happened')).toBeInTheDocument();
  });

  it('renders empty string as falsy', () => {
    const { container } = render(<ProfileMessages {...defaultProps} successMessage="" errorMessage="" />);
    expect(container.firstChild).toBeNull();
  });
});
