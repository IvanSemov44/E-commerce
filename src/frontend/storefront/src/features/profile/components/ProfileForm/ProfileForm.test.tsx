import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import ProfileForm from './ProfileForm';

describe('ProfileForm', () => {
  const mockFormData = {
    firstName: 'John',
    lastName: 'Doe',
    email: 'john@example.com',
    phone: '+1-555-123-4567',
    avatarUrl: 'https://example.com/avatar.jpg',
  };

  it('renders form fields with values', () => {
    render(
      <ProfileForm
        formData={mockFormData}
        isEditMode={false}
        isUpdating={false}
        onFormDataChange={vi.fn()}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByDisplayValue('John')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Doe')).toBeInTheDocument();
    expect(screen.getByDisplayValue('john@example.com')).toBeInTheDocument();
  });

  it('disables fields in view mode', () => {
    render(
      <ProfileForm
        formData={mockFormData}
        isEditMode={false}
        isUpdating={false}
        onFormDataChange={vi.fn()}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByDisplayValue('John')).toBeDisabled();
    expect(screen.getByDisplayValue('Doe')).toBeDisabled();
  });

  it('enables fields in edit mode', () => {
    render(
      <ProfileForm
        formData={mockFormData}
        isEditMode={true}
        isUpdating={false}
        onFormDataChange={vi.fn()}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByDisplayValue('John')).not.toBeDisabled();
    expect(screen.getByDisplayValue('Doe')).not.toBeDisabled();
  });

  it('shows action buttons when in edit mode', () => {
    render(
      <ProfileForm
        formData={mockFormData}
        isEditMode={true}
        isUpdating={false}
        onFormDataChange={vi.fn()}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /save changes/i })).toBeInTheDocument();
  });

  it('hides action buttons in view mode', () => {
    render(
      <ProfileForm
        formData={mockFormData}
        isEditMode={false}
        isUpdating={false}
        onFormDataChange={vi.fn()}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    expect(screen.queryByRole('button', { name: /cancel/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /save changes/i })).not.toBeInTheDocument();
  });

  it('calls onFormDataChange on field input', async () => {
    const user = userEvent.setup();
    const onFormDataChange = vi.fn();

    render(
      <ProfileForm
        formData={mockFormData}
        isEditMode={true}
        isUpdating={false}
        onFormDataChange={onFormDataChange}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    const firstNameInput = screen.getByDisplayValue('John');
    await user.clear(firstNameInput);
    await user.type(firstNameInput, 'Jane');

    expect(onFormDataChange).toHaveBeenCalled();
  });

  it('calls onSubmit when save button clicked', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();

    render(
      <ProfileForm
        formData={mockFormData}
        isEditMode={true}
        isUpdating={false}
        onFormDataChange={vi.fn()}
        onSubmit={onSubmit}
        onCancel={vi.fn()}
      />
    );

    const saveButton = screen.getByRole('button', { name: /save changes/i });
    await user.click(saveButton);

    expect(onSubmit).toHaveBeenCalled();
  });

  it('calls onCancel when cancel button clicked', async () => {
    const user = userEvent.setup();
    const onCancel = vi.fn();

    render(
      <ProfileForm
        formData={mockFormData}
        isEditMode={true}
        isUpdating={false}
        onFormDataChange={vi.fn()}
        onSubmit={vi.fn()}
        onCancel={onCancel}
      />
    );

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    await user.click(cancelButton);

    expect(onCancel).toHaveBeenCalled();
  });

  it('disables buttons while updating', () => {
    render(
      <ProfileForm
        formData={mockFormData}
        isEditMode={true}
        isUpdating={true}
        onFormDataChange={vi.fn()}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: /saving/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled();
  });

  it('validates avatar URL on blur', async () => {
    const user = userEvent.setup();
    const onAvatarError = vi.fn();

    render(
      <ProfileForm
        formData={{ ...mockFormData, avatarUrl: 'invalid-url' }}
        isEditMode={true}
        isUpdating={false}
        onFormDataChange={vi.fn()}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
        onAvatarError={onAvatarError}
      />
    );

    const avatarInput = screen.getByDisplayValue('invalid-url');
    avatarInput.focus();
    await user.tab();

    expect(onAvatarError).toHaveBeenCalled();
  });
});
