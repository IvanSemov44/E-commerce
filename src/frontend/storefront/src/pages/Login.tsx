import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useLoginMutation } from '../store/api/authApi';
import { useAppDispatch } from '../store/hooks';
import { loginSuccess } from '../store/slices/authSlice';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Card from '../components/ui/Card';
import ErrorAlert from '../components/ErrorAlert';
import styles from './Login.module.css';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [login, { isLoading }] = useLoginMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      const response = await login({ email, password }).unwrap();
      if (response.success && response.user && response.token) {
        dispatch(loginSuccess({ user: response.user, token: response.token }));
        navigate('/');
      } else {
        setError(response.message || 'Login failed');
      }
    } catch (err: any) {
      setError(err?.data?.message || 'An error occurred during login');
    }
  };

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Login</h1>

        {error && (
          <div className={styles.errorAlert}>
            <ErrorAlert message={error} onDismiss={() => setError('')} />
          </div>
        )}

        <form onSubmit={handleSubmit} className={styles.form}>
          <Input
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />

          <Input
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />

          <div style={{ textAlign: 'right', marginTop: '-8px' }}>
            <Link to="/forgot-password" className={styles.footerLink} style={{ fontSize: '14px' }}>
              Forgot password?
            </Link>
          </div>

          <Button
            type="submit"
            disabled={isLoading}
            size="lg"
          >
            {isLoading ? 'Logging in...' : 'Login'}
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
