import { describe, it, expect, vi } from 'vitest'
import { renderHook, act, waitFor } from '@testing-library/react'
import useForm from './useForm'

interface TestFormValues {
  username: string
  email: string
  password: string
  rememberMe: boolean
}

describe('useForm', () => {
  const initialValues: TestFormValues = {
    username: '',
    email: '',
    password: '',
    rememberMe: false,
  }

  it('initializes with default values', () => {
    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        onSubmit: vi.fn(),
      })
    )

    expect(result.current.values).toEqual(initialValues)
    expect(result.current.errors).toEqual({})
    expect(result.current.isSubmitting).toBe(false)
  })

  it('handles input changes', () => {
    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        onSubmit: vi.fn(),
      })
    )

    act(() => {
      result.current.handleChange({
        target: { name: 'username', value: 'testuser', type: 'text' },
      } as React.ChangeEvent<HTMLInputElement>)
    })

    expect(result.current.values.username).toBe('testuser')
  })

  it('handles checkbox changes', () => {
    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        onSubmit: vi.fn(),
      })
    )

    act(() => {
      result.current.handleChange({
        target: { name: 'rememberMe', checked: true, type: 'checkbox' },
      } as React.ChangeEvent<HTMLInputElement>)
    })

    expect(result.current.values.rememberMe).toBe(true)
  })

  it('handles multiple input changes', () => {
    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        onSubmit: vi.fn(),
      })
    )

    act(() => {
      result.current.handleChange({
        target: { name: 'username', value: 'testuser', type: 'text' },
      } as React.ChangeEvent<HTMLInputElement>)
    })

    act(() => {
      result.current.handleChange({
        target: { name: 'email', value: 'test@example.com', type: 'text' },
      } as React.ChangeEvent<HTMLInputElement>)
    })

    expect(result.current.values.username).toBe('testuser')
    expect(result.current.values.email).toBe('test@example.com')
  })

  it('clears error when field is modified', () => {
    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        onSubmit: vi.fn(),
      })
    )

    // Set an error manually
    act(() => {
      result.current.setFieldError('username', 'Username is required')
    })

    expect(result.current.errors.username).toBe('Username is required')

    // Change the field
    act(() => {
      result.current.handleChange({
        target: { name: 'username', value: 'testuser', type: 'text' },
      } as React.ChangeEvent<HTMLInputElement>)
    })

    expect(result.current.errors.username).toBeUndefined()
  })

  it('validates form on submit', async () => {
    const handleSubmit = vi.fn()
    const validate = vi.fn((values: TestFormValues) => {
      const errors: Partial<Record<keyof TestFormValues, string>> = {}
      if (!values.username) errors.username = 'Username is required'
      return errors
    })

    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        validate,
        onSubmit: handleSubmit,
      })
    )

    await act(async () => {
      await result.current.handleSubmit({
        preventDefault: vi.fn(),
      } as unknown as React.FormEvent)
    })

    expect(validate).toHaveBeenCalled()
    expect(result.current.errors.username).toBe('Username is required')
    expect(handleSubmit).not.toHaveBeenCalled()
  })

  it('calls onSubmit with values when validation passes', async () => {
    const handleSubmit = vi.fn()
    const validate = vi.fn(() => ({}))

    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        validate,
        onSubmit: handleSubmit,
      })
    )

    // Set some values
    act(() => {
      result.current.setFieldValue('username', 'testuser')
    })

    await act(async () => {
      await result.current.handleSubmit({
        preventDefault: vi.fn(),
      } as unknown as React.FormEvent)
    })

    expect(handleSubmit).toHaveBeenCalledWith({
      ...initialValues,
      username: 'testuser',
    })
  })

  it('sets isSubmitting during form submission', async () => {
    let resolveSubmit: () => void
    const handleSubmit = vi.fn(
      () => new Promise<void>((resolve) => {
        resolveSubmit = resolve
      })
    )

    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        onSubmit: handleSubmit,
      })
    )

    // Start submission — do not await so isSubmitting stays true
    act(() => {
      void result.current.handleSubmit({
        preventDefault: vi.fn(),
      } as unknown as React.FormEvent)
    })

    // Should be submitting while promise is pending
    await waitFor(() => expect(result.current.isSubmitting).toBe(true))

    // Resolve the promise and wait for isSubmitting to clear
    await act(async () => {
      resolveSubmit!()
    })

    // Should no longer be submitting
    expect(result.current.isSubmitting).toBe(false)
  })

  it('resets form to initial values', () => {
    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        onSubmit: vi.fn(),
      })
    )

    act(() => {
      result.current.handleChange({
        target: { name: 'username', value: 'testuser', type: 'text' },
      } as React.ChangeEvent<HTMLInputElement>)
    })

    act(() => {
      result.current.setFieldError('email', 'Invalid email')
    })

    expect(result.current.values.username).toBe('testuser')
    expect(result.current.errors.email).toBe('Invalid email')

    act(() => {
      result.current.reset()
    })

    expect(result.current.values).toEqual(initialValues)
    expect(result.current.errors).toEqual({})
    expect(result.current.isSubmitting).toBe(false)
  })

  it('sets field value directly', () => {
    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        onSubmit: vi.fn(),
      })
    )

    act(() => {
      result.current.setFieldValue('username', 'directvalue')
    })

    expect(result.current.values.username).toBe('directvalue')
  })

  it('sets field error directly', () => {
    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        onSubmit: vi.fn(),
      })
    )

    act(() => {
      result.current.setFieldError('email', 'Invalid email format')
    })

    expect(result.current.errors.email).toBe('Invalid email format')
  })

  it('updates all values at once', () => {
    const { result } = renderHook(() =>
      useForm<TestFormValues>({
        initialValues,
        onSubmit: vi.fn(),
      })
    )

    const newValues: TestFormValues = {
      username: 'newuser',
      email: 'new@example.com',
      password: 'newpassword',
      rememberMe: true,
    }

    act(() => {
      result.current.setValues(newValues)
    })

    expect(result.current.values).toEqual(newValues)
  })
})