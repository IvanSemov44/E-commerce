import { useState, useEffect } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useResetPasswordMutation } from '../store/api/authApi';
import { useToast } from '../hooks';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Card from '../components/ui/Card';
import styles from './Login.module.css';

export default function ResetPassword() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [success, setSuccess] = useState(false);
  const [resetPassword, { isLoading }] = useResetPasswordMutation();
  const { toast } = useToast();

  const email = searchParams.get('email') || '';
  const token = searchParams.get('token') || '';

  useEffect(() => {
    if (!email || !token) {
      toast.error('Invalid password reset link. Please request a new one.');
    }
  }, [email, token, toast]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (password !== confirmPassword) {
      toast.error('Passwords do not match');
      return;
    }

    if (password.length < 6) {
      toast.error('Password must be at least 6 characters');
      return;
    }

    try {
      await resetPassword({ email, token, newPassword: password }).unwrap();
      setSuccess(true);
      toast.success('Password reset successful! Redirecting to login...');
      // Redirect to login after 2 seconds
      setTimeout(() => {
        navigate('/login');
      }, 2000);
    } catch (err: any) {
      toast.error(err?.data?.message || 'Failed to reset password. The link may have expired.');
    }
  };

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Reset Password</h1>

        {success ? (
          <div className={styles.centered}>
            <div className={styles.successBox}>
              <p className={styles.successTitle}>Password Reset Successful!</p>
              <p>Your password has been reset. Redirecting to login...</p>
            </div>
            <Link to="/login" className={styles.footerLink}>
              Go to Login
            </Link>
          </div>
        ) : (
          <>
            {email && token ? (
              <>
                <p className={styles.description}>
                  Enter your new password for <strong>{email}</strong>
                </p>

                <form onSubmit={handleSubmit} className={styles.form}>
                  <Input
                    label="New Password"
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    placeholder="Enter new password"
                  />

                  <Input
                    label="Confirm Password"
                    type="password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                    placeholder="Confirm new password"
                  />

                  <Button
                    type="submit"
                    disabled={isLoading}
                    size="lg"
                  >
                    {isLoading ? 'Resetting...' : 'Reset Password'}
                  </Button>
                </form>
              </>
            ) : (
              <div className={styles.centered}>
                <p className={styles.description}>
                  This password reset link is invalid or has expired.
                </p>
                <Link to="/forgot-password" className={styles.footerLink}>
                  Request a new reset link
                </Link>
              </div>
            )}

            <div className={styles.footer}>
              <p className={styles.footerText}>
                Remember your password?{' '}
                <Link to="/login" className={styles.footerLink}>
                  Back to Login
                </Link>
              </p>
            </div>
          </>
        )}
      </Card>
    </div>
  );
}
