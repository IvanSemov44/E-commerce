import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useLoginMutation } from '../store/api/authApi';
import { useAppDispatch } from '../store/hooks';
import { loginSuccess, type AdminUser } from '../store/slices/authSlice';
import { useToast } from '../hooks';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import styles from './LoginPage.module.css';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [login, { isLoading }] = useLoginMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { toast } = useToast();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email || !password) {
      toast.error('Please fill in all fields');
      return;
    }

    try {
      const response = await login({ email, password }).unwrap();

      // Tokens are now stored in httpOnly cookies by the backend
      const user = response.user;

      if (response.success && user) {
        dispatch(loginSuccess(user as AdminUser));
        toast.success('Login successful!');
        navigate('/');
      } else {
        toast.error(response.message || 'Login failed');
      }
    } catch (err: any) {
      toast.error(err?.data?.message || 'An error occurred during login');
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
