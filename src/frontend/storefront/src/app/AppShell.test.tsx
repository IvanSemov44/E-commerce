import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import AppShell from './AppShell';
vi.mock('@/app/AnnouncementBar', () => ({
  default: () => <div data-testid="announcement-bar">AnnouncementBar</div>,
}));
vi.mock('@/app/layouts', () => ({
  Header: () => <div data-testid="header">Header</div>,
  Footer: () => <div data-testid="footer">Footer</div>,
}));
vi.mock('@/app/skeletons', () => ({
  AppBootstrapLoading: () => <div data-testid="bootstrap-loading">Loading...</div>,
}));

const renderAppShell = (isInitializing = false) => {
  return render(
    <BrowserRouter>
      <AppShell isInitializing={isInitializing}>
        <div>Test Content</div>
      </AppShell>
    </BrowserRouter>
  );
};

describe('AppShell', () => {
  it('renders loading state when isInitializing is true', () => {
    renderAppShell(true);
    expect(screen.getByTestId('bootstrap-loading')).toBeInTheDocument();
  });

  it('renders shell components when not initializing', () => {
    renderAppShell(false);
    expect(screen.getByTestId('announcement-bar')).toBeInTheDocument();
    expect(screen.getByTestId('header')).toBeInTheDocument();
    expect(screen.getByTestId('footer')).toBeInTheDocument();
    expect(screen.getByText('Test Content')).toBeInTheDocument();
  });

  it('does not render children when initializing', () => {
    renderAppShell(true);
    expect(screen.queryByText('Test Content')).not.toBeInTheDocument();
  });
});
