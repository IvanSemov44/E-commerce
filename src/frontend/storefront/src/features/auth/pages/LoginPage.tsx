import { Link, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useLoginMutation } from '../api/authApi';
import { useAppDispatch } from '@/shared/lib/store';
import { loginSuccess } from '../slices/authSlice';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import useForm from '@/shared/hooks/useForm';
import { useToast, useApiErrorHandler } from '@/shared/hooks';
import { Button, Input, Card } from '@/shared/components/ui';
import { zodValidate } from '@/shared/lib/utils/zodValidate';
import { createLoginSchema } from '../schemas/authSchemas';
import styles from './Login.module.css';

export default function Login() {
  const { t } = useTranslation();
  const [login, { isLoading }] = useLoginMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { handleError } = useApiErrorHandler();

  const form = useForm({
    initialValues: { email: '', password: '' },
    validate: zodValidate(createLoginSchema(t)),
    onSubmit: async (values) => {
      try {
        const response = await login(values).unwrap();
        if (response.success && response.user) {
          dispatch(loginSuccess(response.user));
          toast.success(t('auth.loginSuccess'));
          navigate(ROUTE_PATHS.home);
        } else {
          toast.error(response.message || t('auth.loginError'));
        }
      } catch (err) {
        handleError(err, t('auth.loginError'));
      }
    },
  });

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 className={styles.title}>{t('auth.login')}</h1>

        <form onSubmit={form.handleSubmit} className={styles.form}>
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

          <div className={styles.forgotPassword}>
            <Link to={ROUTE_PATHS.forgotPassword} className={styles.footerLink}>
              {t('auth.forgotPassword')}
            </Link>
          </div>

          <Button type="submit" disabled={isLoading || form.isSubmitting} size="lg">
            {isLoading || form.isSubmitting ? t('auth.loggingIn') : t('auth.login')}
          </Button>
        </form>

        <div className={styles.footer}>
          <p className={styles.footerText}>
            {t('auth.dontHaveAccount')}{' '}
            <Link to={ROUTE_PATHS.register} className={styles.footerLink}>
              {t('auth.loginHere')}
            </Link>
          </p>
        </div>
      </Card>
    </div>
  );
}
