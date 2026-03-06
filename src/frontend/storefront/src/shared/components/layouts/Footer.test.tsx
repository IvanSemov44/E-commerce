import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import Footer from './Footer';

const successMock = vi.fn();
const errorMock = vi.fn();

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock('@/shared/hooks/useToast', () => ({
  useToast: () => ({
    success: successMock,
    error: errorMock,
  }),
}));

vi.mock('@/shared/components/icons', () => ({
  FacebookIcon: () => <span>fb</span>,
  TwitterIcon: () => <span>tw</span>,
  InstagramIcon: () => <span>ig</span>,
  LinkedInIcon: () => <span>li</span>,
  YouTubeIcon: () => <span>yt</span>,
}));

function renderFooter() {
  return render(
    <MemoryRouter>
      <Footer />
    </MemoryRouter>
  );
}

describe('Footer', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders newsletter section and social links', () => {
    renderFooter();

    expect(screen.getByText('footer.newsletter')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('footer.yourEmail')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'footer.subscribe' })).toBeInTheDocument();

    expect(screen.getByRole('link', { name: 'Facebook' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Twitter' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Instagram' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'LinkedIn' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'YouTube' })).toBeInTheDocument();
  });

  it('shows validation error for invalid email', () => {
    renderFooter();

    fireEvent.click(screen.getByRole('button', { name: 'footer.subscribe' }));

    expect(errorMock).toHaveBeenCalledWith('footer.emailInvalid');
    expect(successMock).not.toHaveBeenCalled();
  });

  it('subscribes new email and stores it in localStorage', async () => {
    renderFooter();

    const input = screen.getByPlaceholderText('footer.yourEmail');
    fireEvent.change(input, { target: { value: 'john@example.com' } });
    fireEvent.click(screen.getByRole('button', { name: 'footer.subscribe' }));

    expect(screen.getByRole('button', { name: 'footer.subscribing' })).toBeDisabled();

    await waitFor(() => {
      expect(successMock).toHaveBeenCalledWith('footer.subscribeSuccess');
    }, { timeout: 2000 });

    expect(errorMock).not.toHaveBeenCalled();
    expect(localStorage.getItem('newsletter_subscribers')).toBe(JSON.stringify(['john@example.com']));
    expect(screen.getByPlaceholderText('footer.yourEmail')).toHaveValue('');
  });

  it('shows already subscribed error for duplicate email', async () => {
    localStorage.setItem('newsletter_subscribers', JSON.stringify(['john@example.com']));
    renderFooter();

    const input = screen.getByPlaceholderText('footer.yourEmail');
    fireEvent.change(input, { target: { value: 'john@example.com' } });
    fireEvent.submit(input.closest('form') as HTMLFormElement);

    await waitFor(() => {
      expect(errorMock).toHaveBeenCalledWith('footer.emailAlreadySubscribed');
    }, { timeout: 2000 });

    expect(successMock).not.toHaveBeenCalled();
  });

  it('renders common footer links', () => {
    renderFooter();

    expect(screen.getByRole('link', { name: 'footer.aboutUs' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'footer.helpCenter' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'footer.privacyPolicy' })).toBeInTheDocument();
    expect(screen.getByText('Legal')).toBeInTheDocument();
  });
});
