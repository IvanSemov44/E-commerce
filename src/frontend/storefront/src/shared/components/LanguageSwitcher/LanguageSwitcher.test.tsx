import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { LanguageSwitcher } from './LanguageSwitcher';

const changeLanguageMock = vi.fn().mockResolvedValue(undefined);

vi.mock('@/shared/hooks/useTranslation', () => ({
  useTranslation: () => ({
    currentLanguage: 'en',
    changeLanguage: changeLanguageMock,
  }),
}));

vi.mock('@/shared/i18n', () => ({
  languages: [
    { code: 'en', name: 'English', nativeName: 'English', dir: 'ltr' },
    { code: 'bg', name: 'Bulgarian', nativeName: 'Български', dir: 'ltr' },
  ],
}));

vi.mock('@/shared/components/icons', () => ({
  ChevronDownIcon: ({ className }: { className?: string }) => <span data-testid="chevron" className={className}>v</span>,
  CheckIcon: () => <span data-testid="check">✓</span>,
}));

async function renderMounted() {
  render(<LanguageSwitcher />);
  await waitFor(() => {
    const button = screen.getByRole('button');
    expect(button).not.toBeDisabled();
  });
}

describe('LanguageSwitcher', () => {
  beforeEach(() => {
    changeLanguageMock.mockClear();
  });

  it('renders disabled placeholder before mount completes', () => {
    render(<LanguageSwitcher />);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
    expect(button).toHaveTextContent('EN');
  });

  it('renders current language after mount', () => {
    render(<LanguageSwitcher />);
    const button = screen.getByRole('button');
    expect(button).toBeInTheDocument();
  });

  it('shows accessible language button after mount', async () => {
    await renderMounted();
    const button = screen.getByRole('button', { name: /language: english/i });
    expect(button).not.toBeDisabled();
    expect(button).toHaveTextContent('English');
  });

  it('opens dropdown and shows language options', async () => {
    await renderMounted();

    fireEvent.click(screen.getByRole('button', { name: /language: english/i }));

    expect(screen.getByRole('listbox', { name: /language options/i })).toBeInTheDocument();
    expect(screen.getAllByRole('option')).toHaveLength(2);
  });

  it('calls changeLanguage for a different language and closes dropdown', async () => {
    await renderMounted();

    fireEvent.click(screen.getByRole('button', { name: /language: english/i }));
    fireEvent.click(screen.getByRole('option', { name: /български/i }));

    await waitFor(() => {
      expect(changeLanguageMock).toHaveBeenCalledWith('bg');
      expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    });
  });

  it('does not call changeLanguage for current language', async () => {
    await renderMounted();

    fireEvent.click(screen.getByRole('button', { name: /language: english/i }));
    fireEvent.click(screen.getByRole('option', { name: /english/i }));

    expect(changeLanguageMock).not.toHaveBeenCalled();
  });

  it('closes dropdown on Escape key', async () => {
    await renderMounted();

    fireEvent.click(screen.getByRole('button', { name: /language: english/i }));
    expect(screen.getByRole('listbox')).toBeInTheDocument();

    fireEvent.keyDown(document, { key: 'Escape' });

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('closes dropdown when clicking outside', async () => {
    await renderMounted();

    fireEvent.click(screen.getByRole('button', { name: /language: english/i }));
    expect(screen.getByRole('listbox')).toBeInTheDocument();

    fireEvent.mouseDown(document.body);

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });
});
