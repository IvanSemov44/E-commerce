import { useTranslation } from 'react-i18next';
import { useGetProfileQuery } from '@/features/profile/api/profileApi';
import { Card } from '@/shared/components/ui/Card';
import PageHeader from '@/shared/components/PageHeader';
import QueryRenderer from '@/shared/components/QueryRenderer';
import { AccountDetails } from '@/features/profile/components/AccountDetails/AccountDetails';
import { ProfileSkeleton } from '@/features/profile/components/ProfileSkeleton/ProfileSkeleton';
import { ProfileFormCard } from '@/features/profile/components/ProfileFormCard/ProfileFormCard';
import styles from './ProfilePage.module.css';

export function ProfilePage() {
  const { t } = useTranslation();
  const { data: profile, isLoading, error } = useGetProfileQuery();

  return (
    <div className={styles.container}>
      <PageHeader title={t('profile.title')} />
      <QueryRenderer
        isLoading={isLoading}
        error={error}
        data={profile}
        loadingSkeleton={{
          custom: (
            <Card variant="elevated" padding="lg">
              <ProfileSkeleton />
            </Card>
          ),
        }}
        errorMessage={t('profile.failedToLoad')}
      >
        {(profile) => (
          <div className={styles.content}>
            <ProfileFormCard />
            <AccountDetails memberSince={profile.email} />
          </div>
        )}
      </QueryRenderer>
    </div>
  );
}
