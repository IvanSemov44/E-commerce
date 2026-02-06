import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForgotPasswordMutation } from '../store/api/authApi';
import { useToast } from '../hooks';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Card from '../components/ui/Card';
import styles from './Login.module.css';

export default function ForgotPassword() {
  const [email, setEmail] = useState('');
  const [success, setSuccess] = useState(false);
  const [forgotPassword, { isLoading }] = useForgotPasswordMutation();
  const { toast } = useToast();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      await forgotPassword({ email }).unwrap();
      setSuccess(true);
      toast.success('Password reset link sent! Check your email.');
    } catch (err: any) {
      toast.error(err?.data?.message || 'An error occurred. Please try again.');
    }
  };

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Forgot Password</h1>

        {success ? (
          <div className={styles.centered}>
            <div className={styles.successBox}>
              <p className={styles.successTitle}>Check your email!</p>
              <p>If an account exists with that email, we've sent you a password reset link.</p>
            </div>
            <Link to="/login" className={styles.footerLink}>
              Back to Login
            </Link>
          </div>
        ) : (
          <>
            <p className={styles.description}>
              Enter your email address and we'll send you a link to reset your password.
            </p>

            <form onSubmit={handleSubmit} className={styles.form}>
              <Input
                label="Email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                placeholder="Enter your email address"
              />

              <Button
                type="submit"
                disabled={isLoading}
                size="lg"
              >
                {isLoading ? 'Sending...' : 'Send Reset Link'}
              </Button>
            </form>

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
