import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import ProfileForm from './ProfileForm';

const mockValues = {
  firstName: 'John',
  lastName: 'Doe',
  email: 'john@example.com',
  phone: '+1-555-123-4567',
  avatarUrl: 'https://example.com/avatar.jpg',
};

const baseProps = {
  values: mockValues,
  fieldErrors: {},
  isEditMode: false,
  isPending: false,
  action: vi.fn(),
  onCancel: vi.fn(),
  onChange: vi.fn(),
  onBlur: vi.fn(),
};

describe('ProfileForm', () => {
  it('renders form fields with values', () => {
    render(<ProfileForm {...baseProps} />);

    expect(screen.getByDisplayValue('John')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Doe')).toBeInTheDocument();
    expect(screen.getByDisplayValue('john@example.com')).toBeInTheDocument();
  });

  it('disables editable fields in view mode', () => {
    render(<ProfileForm {...baseProps} isEditMode={false} />);

    expect(screen.getByDisplayValue('John')).toBeDisabled();
    expect(screen.getByDisplayValue('Doe')).toBeDisabled();
  });

  it('enables editable fields in edit mode', () => {
    render(<ProfileForm {...baseProps} isEditMode={true} />);

    expect(screen.getByDisplayValue('John')).not.toBeDisabled();
    expect(screen.getByDisplayValue('Doe')).not.toBeDisabled();
  });

  it('email field is always disabled', () => {
    render(<ProfileForm {...baseProps} isEditMode={true} />);

    expect(screen.getByDisplayValue('john@example.com')).toBeDisabled();
  });

  it('shows action buttons in edit mode', () => {
    render(<ProfileForm {...baseProps} isEditMode={true} />);

    expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /save changes/i })).toBeInTheDocument();
  });

  it('hides action buttons in view mode', () => {
    render(<ProfileForm {...baseProps} isEditMode={false} />);

    expect(screen.queryByRole('button', { name: /cancel/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /save changes/i })).not.toBeInTheDocument();
  });

  it('disables buttons and shows updating text while pending', () => {
    render(<ProfileForm {...baseProps} isEditMode={true} isPending={true} />);

    expect(screen.getByRole('button', { name: /updating/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled();
  });

  it('calls onChange when a field value changes', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<ProfileForm {...baseProps} isEditMode={true} onChange={onChange} />);

    await user.type(screen.getByDisplayValue('John'), 'x');

    expect(onChange).toHaveBeenCalled();
  });

  it('calls onCancel when cancel button clicked', async () => {
    const user = userEvent.setup();
    const onCancel = vi.fn();
    render(<ProfileForm {...baseProps} isEditMode={true} onCancel={onCancel} />);

    await user.click(screen.getByRole('button', { name: /cancel/i }));

    expect(onCancel).toHaveBeenCalled();
  });

  it('displays field errors', () => {
    render(
      <ProfileForm
        {...baseProps}
        isEditMode={true}
        fieldErrors={{ firstName: 'First Name is required' }}
      />
    );

    expect(screen.getByText('First Name is required')).toBeInTheDocument();
  });

  it('calls onBlur when a field loses focus', async () => {
    const user = userEvent.setup();
    const onBlur = vi.fn();
    render(<ProfileForm {...baseProps} isEditMode={true} onBlur={onBlur} />);

    await user.click(screen.getByDisplayValue('John'));
    await user.tab();

    expect(onBlur).toHaveBeenCalled();
  });
});
