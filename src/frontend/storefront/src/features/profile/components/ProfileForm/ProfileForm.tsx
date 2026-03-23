import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/components/ui/Button';
import { Input } from '@/shared/components/ui/Input';
import type { ProfileFormProps } from './ProfileForm.types';
import styles from './ProfileForm.module.css';

export function ProfileForm({
  values,
  fieldErrors,
  isEditMode,
  isPending,
  action,
  onCancel,
  onChange,
  onBlur,
}: ProfileFormProps) {
  const { t } = useTranslation();

  return (
    <form action={action} className={styles.form} noValidate>
      <div className={styles.grid}>
        <div className={styles.field}>
          <Input
            label={t('profile.firstName')}
            name="firstName"
            value={values.firstName}
            onChange={onChange}
            onBlur={onBlur}
            error={fieldErrors.firstName}
            disabled={!isEditMode || isPending}
            required
          />
        </div>
        <div className={styles.field}>
          <Input
            label={t('profile.lastName')}
            name="lastName"
            value={values.lastName}
            onChange={onChange}
            onBlur={onBlur}
            error={fieldErrors.lastName}
            disabled={!isEditMode || isPending}
            required
          />
        </div>
        <div className={styles.field}>
          <Input label={t('profile.email')} name="email" value={values.email} disabled />
        </div>
        <div className={styles.field}>
          <Input
            label={t('profile.phone')}
            name="phone"
            value={values.phone ?? ''}
            onChange={onChange}
            onBlur={onBlur}
            error={fieldErrors.phone}
            disabled={!isEditMode || isPending}
          />
        </div>
        <div className={styles.field}>
          <Input
            label={t('profile.avatarUrl')}
            name="avatarUrl"
            value={values.avatarUrl ?? ''}
            onChange={onChange}
            onBlur={onBlur}
            error={fieldErrors.avatarUrl}
            disabled={!isEditMode || isPending}
          />
        </div>
      </div>

      {isEditMode && (
        <div className={styles.actions}>
          <Button type="button" variant="ghost" onClick={onCancel} disabled={isPending}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" disabled={isPending}>
            {isPending ? t('common.updating') : t('profile.saveChanges')}
          </Button>
        </div>
      )}
    </form>
  );
}
