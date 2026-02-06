import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useRegisterMutation } from '../store/api/authApi';
import { useAppDispatch } from '../store/hooks';
import { loginSuccess } from '../store/slices/authSlice';
import useForm from '../hooks/useForm';
import { useToast } from '../hooks';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Card from '../components/ui/Card';
import styles from './Register.module.css';

export default function Register() {
  const [register, { isLoading }] = useRegisterMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { toast } = useToast();

  const form = useForm({
    initialValues: {
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      confirmPassword: '',
    },
    validate: (values) => {
      const errors: any = {};
      if (!values.firstName) errors.firstName = 'First name is required';
      if (!values.lastName) errors.lastName = 'Last name is required';
      if (!values.email) errors.email = 'Email is required';
      if (!values.password) errors.password = 'Password is required';
      if (!values.confirmPassword) errors.confirmPassword = 'Please confirm password';
      if (values.password && values.confirmPassword && values.password !== values.confirmPassword) {
        errors.confirmPassword = 'Passwords do not match';
      }
      return errors;
    },
    onSubmit: async (values) => {
      try {
        const { confirmPassword, ...registerData } = values;
        const response = await register(registerData).unwrap();
        if (response.success && response.user && response.token) {
          dispatch(loginSuccess({ user: response.user, token: response.token }));
          toast.success('Registration successful!');
          navigate('/');
        } else {
          toast.error(response.message || 'Registration failed');
        }
      } catch (err: any) {
        toast.error(err?.data?.message || 'An error occurred during registration');
      }
    },
  });

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Register</h1>

        <form onSubmit={form.handleSubmit} className={styles.form}>
          <div className={styles.nameFields}>
            <Input
              label="First Name"
              type="text"
              name="firstName"
              value={form.values.firstName}
              onChange={form.handleChange}
              error={form.errors.firstName}
              required
            />
            <Input
              label="Last Name"
              type="text"
              name="lastName"
              value={form.values.lastName}
              onChange={form.handleChange}
              error={form.errors.lastName}
              required
            />
          </div>

          <Input
            label="Email"
            type="email"
            name="email"
            value={form.values.email}
            onChange={form.handleChange}
            error={form.errors.email}
            required
          />

          <Input
            label="Password"
            type="password"
            name="password"
            value={form.values.password}
            onChange={form.handleChange}
            error={form.errors.password}
            required
          />

          <Input
            label="Confirm Password"
            type="password"
            name="confirmPassword"
            value={form.values.confirmPassword}
            onChange={form.handleChange}
            error={form.errors.confirmPassword}
            required
          />

          <Button
            type="submit"
            disabled={isLoading || form.isSubmitting}
            size="lg"
          >
            {isLoading || form.isSubmitting ? 'Registering...' : 'Register'}
          </Button>
        </form>

        <div className={styles.footer}>
          <p className={styles.footerText}>
            Already have an account?{' '}
            <Link to="/login" className={styles.footerLink}>
              Login here
            </Link>
          </p>
        </div>
      </Card>
    </div>
  );
}
