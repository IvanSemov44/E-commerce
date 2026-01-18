import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useRegisterMutation } from '../store/api/authApi';
import { useAppDispatch } from '../store/hooks';
import { loginSuccess } from '../store/slices/authSlice';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Card from '../components/ui/Card';
import ErrorAlert from '../components/ErrorAlert';
import styles from './Register.module.css';

export default function Register() {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [register, { isLoading }] = useRegisterMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (password !== confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    try {
      const response = await register({ email, password, firstName, lastName }).unwrap();
      if (response.success && response.user && response.token) {
        dispatch(loginSuccess({ user: response.user, token: response.token }));
        navigate('/');
      } else {
        setError(response.message || 'Registration failed');
      }
    } catch (err: any) {
      setError(err?.data?.message || 'An error occurred during registration');
    }
  };

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>Register</h1>

        {error && (
          <div className={styles.errorAlert}>
            <ErrorAlert message={error} onDismiss={() => setError('')} />
          </div>
        )}

        <form onSubmit={handleSubmit} className={styles.form}>
          <div className={styles.nameFields}>
            <Input
              label="First Name"
              type="text"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              required
            />
            <Input
              label="Last Name"
              type="text"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              required
            />
          </div>

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

          <Input
            label="Confirm Password"
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            required
          />

          <Button
            type="submit"
            disabled={isLoading}
            size="lg"
          >
            {isLoading ? 'Registering...' : 'Register'}
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
