import { describe, it, expect, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import useForm from '../useForm';
import type { FormEvent } from 'react';

interface TestFormValues {
  email: string;
  password: string;
  name?: string;
}

// Helper to create mock form event
function createMockFormEvent(): FormEvent {
  return {
    preventDefault: vi.fn(),
  } as unknown as FormEvent;
}

describe('useForm', () => {
  const initialValues: TestFormValues = {
    email: '',
    password: '',
    name: '',
  };

  it('should initialize with initial values', () => {
    const onSubmit = vi.fn();
    const { result } = renderHook(() => useForm({ initialValues, onSubmit }));

    expect(result.current.values).toEqual(initialValues);
    expect(result.current.errors).toEqual({});
    expect(result.current.isSubmitting).toBe(false);
  });

  it('should update values on handleChange', () => {
    const onSubmit = vi.fn();
    const { result } = renderHook(() => useForm({ initialValues, onSubmit }));

    act(() => {
      result.current.handleChange({
        target: { name: 'email', value: 'test@example.com' },
      } as React.ChangeEvent<HTMLInputElement>);
    });

    expect(result.current.values.email).toBe('test@example.com');
  });

  it('should clear error when field is changed', () => {
    const onSubmit = vi.fn();
    const { result } = renderHook(() => useForm({ initialValues, onSubmit }));

    // Set an error first
    act(() => {
      result.current.setFieldError('email', 'Invalid email');
    });

    expect(result.current.errors.email).toBe('Invalid email');

    // Change the field
    act(() => {
      result.current.handleChange({
        target: { name: 'email', value: 'test@example.com' },
      } as React.ChangeEvent<HTMLInputElement>);
    });

    expect(result.current.errors.email).toBeUndefined();
  });

  it('should call onSubmit with values when validation passes', async () => {
    const onSubmit = vi.fn();
    const { result } = renderHook(() => useForm({ initialValues, onSubmit }));

    // Set values
    act(() => {
      result.current.setFieldValue('email', 'test@example.com');
      result.current.setFieldValue('password', 'password123');
    });

    // Submit
    await act(async () => {
      await result.current.handleSubmit(createMockFormEvent());
    });

    expect(onSubmit).toHaveBeenCalledWith({
      email: 'test@example.com',
      password: 'password123',
      name: '',
    });
  });

  it('should not call onSubmit when validation fails', async () => {
    const onSubmit = vi.fn();
    const validate = vi.fn((values: TestFormValues) => {
      if (!values.email) return { email: 'Email is required' };
      return {};
    });

    const { result } = renderHook(() => useForm({ initialValues, onSubmit, validate }));

    await act(async () => {
      await result.current.handleSubmit(createMockFormEvent());
    });

    expect(onSubmit).not.toHaveBeenCalled();
    expect(result.current.errors.email).toBe('Email is required');
  });

  it('should set isSubmitting during submission', async () => {
    const onSubmit = vi.fn().mockImplementation(() => {
      return new Promise((resolve) => setTimeout(resolve, 100));
    });

    const { result } = renderHook(() => useForm({ initialValues, onSubmit }));

    expect(result.current.isSubmitting).toBe(false);

    let promise: Promise<void>;
    act(() => {
      promise = result.current.handleSubmit(createMockFormEvent());
    });

    // Check isSubmitting is true during submission
    expect(result.current.isSubmitting).toBe(true);

    await act(async () => {
      await promise;
    });

    expect(result.current.isSubmitting).toBe(false);
  });

  it('should reset form to initial values', () => {
    const onSubmit = vi.fn();
    const { result } = renderHook(() => useForm({ initialValues, onSubmit }));

    // Change values
    act(() => {
      result.current.setFieldValue('email', 'test@example.com');
      result.current.setFieldError('email', 'Error');
    });

    // Reset
    act(() => {
      result.current.reset();
    });

    expect(result.current.values).toEqual(initialValues);
    expect(result.current.errors).toEqual({});
    expect(result.current.isSubmitting).toBe(false);
  });

  it('should set field value directly', () => {
    const onSubmit = vi.fn();
    const { result } = renderHook(() => useForm({ initialValues, onSubmit }));

    act(() => {
      result.current.setFieldValue('name', 'John Doe');
    });

    expect(result.current.values.name).toBe('John Doe');
  });

  it('should set field error directly', () => {
    const onSubmit = vi.fn();
    const { result } = renderHook(() => useForm({ initialValues, onSubmit }));

    act(() => {
      result.current.setFieldError('password', 'Password too short');
    });

    expect(result.current.errors.password).toBe('Password too short');
  });

  it('should update all values', () => {
    const onSubmit = vi.fn();
    const { result } = renderHook(() => useForm({ initialValues, onSubmit }));

    const newValues: TestFormValues = {
      email: 'new@example.com',
      password: 'newpassword',
      name: 'New Name',
    };

    act(() => {
      result.current.setValues(newValues);
    });

    expect(result.current.values).toEqual(newValues);
  });
});
