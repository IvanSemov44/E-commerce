import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import ProfileMessages from './ProfileMessages';
import styles from './ProfileMessages.module.css';

describe('ProfileMessages', () => {
  it('renders nothing when no messages provided', () => {
    const { container } = render(
      <ProfileMessages />
    );

    expect(container.firstChild).toBeNull();
  });

  it('renders success message when provided', () => {
    render(
      <ProfileMessages successMessage="Profile updated successfully" />
    );

    expect(screen.getByText('Profile updated successfully')).toBeInTheDocument();
  });

  it('renders error message when provided', () => {
    render(
      <ProfileMessages errorMessage="Failed to update profile" />
    );

    expect(screen.getByText('Failed to update profile')).toBeInTheDocument();
  });

  it('renders both messages when both provided', () => {
    render(
      <ProfileMessages
        successMessage="Profile updated"
        errorMessage="But some fields failed"
      />
    );

    expect(screen.getByText('Profile updated')).toBeInTheDocument();
    expect(screen.getByText('But some fields failed')).toBeInTheDocument();
  });

  it('applies success styling to success message', () => {
    render(
      <ProfileMessages successMessage="Profile updated successfully" />
    );

    const successElement = screen.getByText('Profile updated successfully');
    expect(successElement).toHaveClass(styles.success);
  });

  it('applies error styling to error message', () => {
    render(
      <ProfileMessages errorMessage="Failed to update profile" />
    );

    const errorElement = screen.getByText('Failed to update profile');
    expect(errorElement).toHaveClass(styles.error);
  });

  it('renders in container with proper structure', () => {
    const { container } = render(
      <ProfileMessages successMessage="Profile updated" />
    );

    const messageContainer = container.firstChild;
    expect(messageContainer).toHaveClass(styles.container);
  });
});
