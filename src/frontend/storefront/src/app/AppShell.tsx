import type { ReactNode } from 'react';
import AnnouncementBar from '@/shared/components/AnnouncementBar';
import { Header, Footer } from '@/shared/components/layouts';
import LoadingFallback from '@/shared/components/LoadingFallback';

interface AppShellProps {
  children: ReactNode;
  isInitializing: boolean;
}

export default function AppShell({ children, isInitializing }: AppShellProps) {
  return (
    <div>
      <AnnouncementBar />
      <Header />
      <main>{isInitializing ? <LoadingFallback /> : children}</main>
      <Footer />
    </div>
  );
}
