import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useLoginMutation } from '../store/api/authApi';
import { useAppDispatch } from '../store/hooks';
import { loginSuccess } from '../store/slices/authSlice';
import useForm from '../hooks/useForm';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Card from '../components/ui/Card';
import ErrorAlert from '../components/ErrorAlert';
import styles from './Login.module.css';

export default function Login() {
  const [error, setError] = useState('');
  const [login, { isLoading }] = useLoginMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const form = useForm({
    initialValues: { email: '', password: '' },
    validate: (values) => {
      const errors: any = {};
      if (!values.email) errors.email = 'Email is required';
      if (!values.password) errors.password = 'Password is required';
      return errors;
    },
    onSubmit: async (values) => {
      setError('');
      try {
        const response = await login(values).unwrap();
        if (response.success && response.user && response.token) {
          dispatch(loginSuccess({ user: response.user, token: response.token }));
          navigate('/');
        } else {
          setError(response.message || 'Login failed');
        }
      } catch (err: any) {
        setError(err?.data?.message || 'An error occurred during login');
      }
    },
  });

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Login</h1>

        {error && (
          <div className={styles.errorAlert}>
            <ErrorAlert message={error} onDismiss={() => setError('')} />
          </div>
        )}

        <form onSubmit={form.handleSubmit} className={styles.form}>
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

          <div style={{ textAlign: 'right', marginTop: '-8px' }}>
            <Link to="/forgot-password" className={styles.footerLink} style={{ fontSize: '14px' }}>
              Forgot password?
            </Link>
          </div>

          <Button
            type="submit"
            disabled={isLoading || form.isSubmitting}
            size="lg"
          >
            {isLoading || form.isSubmitting ? 'Logging in...' : 'Login'}
          </Button>
        </form>

        <div className={styles.footer}>
          <p className={styles.footerText}>
            Don't have an account?{' '}
            <Link to="/register" className={styles.footerLink}>
              Register here
            </Link>
          </p>
        </div>
      </Card>
    </div>
  );
}
