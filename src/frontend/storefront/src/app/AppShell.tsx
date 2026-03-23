import type { ReactNode } from 'react';
import { AnnouncementBar } from '@/app/AnnouncementBar';
import { Header, Footer } from '@/app/layouts';
import { AppBootstrapLoading } from '@/app/skeletons';

interface AppShellProps {
  children: ReactNode;
  isInitializing: boolean;
}

export function AppShell({ children, isInitializing }: AppShellProps) {
  if (isInitializing) {
    return <AppBootstrapLoading />;
  }

  return (
    <div>
      <AnnouncementBar />
      <Header />
      <main>{children}</main>
      <Footer />
    </div>
  );
}
