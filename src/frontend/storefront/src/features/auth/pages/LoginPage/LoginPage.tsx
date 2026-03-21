import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button, Input, Card } from '@/shared/components/ui';
import { PasswordToggleButton } from '@/features/auth/components/PasswordToggleButton/PasswordToggleButton';
import { useLoginForm } from './useLoginForm';
import styles from './LoginPage.module.css';

export function LoginPage() {
  const { t } = useTranslation();
  const { values, fieldErrors, password, handleChange, handleBlur, action, isPending } =
    useLoginForm();

  return (
    <div className={styles.container}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <h1 id="login-title" className={styles.title}>
          {t('auth.login')}
        </h1>

        <form action={action} noValidate aria-labelledby="login-title" className={styles.form}>
          <Input
            label={t('auth.email')}
            type="email"
            name="email"
            value={values.email}
            autoComplete="email"
            autoCapitalize="none"
            autoCorrect="off"
            onChange={handleChange}
            onBlur={handleBlur}
            error={fieldErrors.email}
            disabled={isPending}
            required
          />

          <Input
            label={t('auth.password')}
            type={password.inputType}
            name="password"
            value={values.password}
            autoComplete="current-password"
            autoCapitalize="none"
            spellCheck={false}
            onChange={handleChange}
            onBlur={handleBlur}
            error={fieldErrors.password}
            disabled={isPending}
            required
            trailingElement={
              <PasswordToggleButton
                show={password.show}
                ariaLabel={password.ariaLabel}
                onClick={password.toggle}
              />
            }
          />

          <div className={styles.forgotPassword}>
            <Link to={ROUTE_PATHS.forgotPassword} className={styles.footerLink}>
              {t('auth.forgotPassword')}
            </Link>
          </div>

          <Button type="submit" disabled={isPending} size="lg">
            {isPending ? t('auth.loggingIn') : t('auth.login')}
          </Button>
        </form>

        <div className={styles.footer}>
          <p className={styles.footerText}>
            {t('auth.dontHaveAccount')}{' '}
            <Link to={ROUTE_PATHS.register} className={styles.footerLink}>
              {t('auth.registerHere')}
            </Link>
          </p>
        </div>
      </Card>
    </div>
  );
}
