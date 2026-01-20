import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useLoginMutation } from '../store/api/authApi';
import { useAppDispatch } from '../store/hooks';
import { loginSuccess, type AdminUser } from '../store/slices/authSlice';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import styles from './LoginPage.module.css';

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

    if (!email || !password) {
      setError('Please fill in all fields');
      return;
    }

    try {
      const response = await login({ email, password }).unwrap();
      if (response.success && response.user && response.token) {
        dispatch(loginSuccess({ user: response.user as AdminUser, token: response.token }));
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
      <div className={styles.content}>
        <div className={styles.logo}>
          <div className={styles.logoBadge}>E</div>
          <h1>Admin Portal</h1>
        </div>

        <Card variant="elevated" className={styles.card}>
          <CardHeader>
            <CardTitle>Administrator Login</CardTitle>
          </CardHeader>
          <CardContent className={styles.cardContent}>
            {error && (
              <div className={styles.error}>
                <div className={styles.errorIcon}>⚠️</div>
                <p>{error}</p>
              </div>
            )}

            <form onSubmit={handleSubmit} className={styles.form}>
              <Input
                label="Email Address"
                type="email"
                placeholder="admin@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />

              <Input
                label="Password"
                type="password"
                placeholder="Enter your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />

              <Button
                type="submit"
                disabled={isLoading}
                isLoading={isLoading}
                size="lg"
                className={styles.submitButton}
              >
                {isLoading ? 'Logging in...' : 'Login'}
              </Button>
            </form>

            <div className={styles.footer}>
              <p className={styles.footerText}>
                Admin access only. If you don't have credentials, contact your administrator.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
