import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useRegisterMutation } from '../api/authApi';
import { useAppDispatch } from '@/shared/lib/store';
import { loginSuccess } from '../slices/authSlice';
import useForm from '@/shared/hooks/useForm';
import { useToast } from '@/shared/hooks/useToast';
import { Button, Input, Card } from '@/shared/components/ui';
import styles from './Register.module.css';

export default function Register() {
  const { t } = useTranslation();
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
      const errors: Record<string, string> = {};
      if (!values.firstName) errors.firstName = t('profile.firstName') + ' ' + t('common.required').toLowerCase();
      if (!values.lastName) errors.lastName = t('profile.lastName') + ' ' + t('common.required').toLowerCase();
      if (!values.email) errors.email = t('auth.emailRequired');
      if (!values.password) errors.password = t('auth.passwordRequired');
      if (!values.confirmPassword) errors.confirmPassword = t('auth.confirmPassword') + ' ' + t('common.required').toLowerCase();
      if (values.password && values.confirmPassword && values.password !== values.confirmPassword) {
        errors.confirmPassword = t('auth.passwordsDoNotMatch');
      }
      return errors;
    },
    onSubmit: async (values) => {
      try {
        // eslint-disable-next-line @typescript-eslint/no-unused-vars
        const { confirmPassword, ...registerData } = values;
        const response = await register(registerData).unwrap();
        if (response.success && response.user) {
          dispatch(loginSuccess(response.user));
          toast.success(t('auth.registrationSuccess'));
          navigate('/');
        } else {
          toast.error(response.message || t('auth.registrationFailed'));
        }
      } catch (err: unknown) {
        const error = err as { data?: { message?: string } };
        toast.error(error?.data?.message || t('auth.registrationError'));
      }
    },
  });

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>{t('auth.register')}</h1>

        <form onSubmit={form.handleSubmit} className={styles.form}>
          <div className={styles.nameFields}>
            <Input
              label={t('profile.firstName')}
              type="text"
              name="firstName"
              value={form.values.firstName}
              onChange={form.handleChange}
              error={form.errors.firstName}
              required
            />
            <Input
              label={t('profile.lastName')}
              type="text"
              name="lastName"
              value={form.values.lastName}
              onChange={form.handleChange}
              error={form.errors.lastName}
              required
            />
          </div>

          <Input
            label={t('auth.email')}
            type="email"
            name="email"
            value={form.values.email}
            onChange={form.handleChange}
            error={form.errors.email}
            required
          />

          <Input
            label={t('auth.password')}
            type="password"
            name="password"
            value={form.values.password}
            onChange={form.handleChange}
            error={form.errors.password}
            required
          />

          <Input
            label={t('auth.confirmPassword')}
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
            {isLoading || form.isSubmitting ? t('auth.registering') : t('auth.register')}
          </Button>
        </form>

        <div className={styles.footer}>
          <p className={styles.footerText}>
            {t('auth.alreadyHaveAccount')}{' '}
            <Link to="/login" className={styles.footerLink}>
              {t('auth.loginHere')}
            </Link>
          </p>
        </div>
      </Card>
    </div>
  );
}
