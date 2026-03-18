import { Link, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useRegisterMutation } from '../api/authApi';
import { useAppDispatch } from '@/shared/lib/store';
import { loginSuccess } from '../slices/authSlice';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { useForm } from '@/shared/hooks/useForm';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { Button, Input, Card } from '@/shared/components/ui';
import { zodValidate } from '@/shared/lib/utils/zodValidate';
import { createRegisterSchema } from '../schemas/authSchemas';
import styles from './Register.module.css';

export default function Register() {
  const { t } = useTranslation();
  const [register, { isLoading }] = useRegisterMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const form = useForm({
    initialValues: {
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      confirmPassword: '',
    },
    validate: zodValidate(createRegisterSchema(t)),
    onSubmit: async (values) => {
      try {
        // eslint-disable-next-line @typescript-eslint/no-unused-vars
        const { confirmPassword, ...registerData } = values;
        const response = await register(registerData).unwrap();
        if (response.success && response.user) {
          dispatch(loginSuccess(response.user));
          toast.success(t('auth.registrationSuccess'));
          navigate(ROUTE_PATHS.home);
        } else {
          toast.error(response.message || t('auth.registrationFailed'));
        }
      } catch (err) {
        handleError(err, t('auth.registrationError'));
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

          <Button type="submit" disabled={isLoading || form.isSubmitting} size="lg">
            {isLoading || form.isSubmitting ? t('auth.registering') : t('auth.register')}
          </Button>
        </form>

        <div className={styles.footer}>
          <p className={styles.footerText}>
            {t('auth.alreadyHaveAccount')}{' '}
            <Link to={ROUTE_PATHS.login} className={styles.footerLink}>
              {t('auth.loginHere')}
            </Link>
          </p>
        </div>
      </Card>
    </div>
  );
}
