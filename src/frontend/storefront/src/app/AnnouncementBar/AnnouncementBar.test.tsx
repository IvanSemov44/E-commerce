import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { AnnouncementBar } from './AnnouncementBar';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

describe('AnnouncementBar', () => {
  it('renders translated defaults', () => {
    render(
      <MemoryRouter>
        <AnnouncementBar />
      </MemoryRouter>
    );

    expect(screen.getByText('announcement.freeShipping')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'announcement.shopNow' })).toHaveAttribute(
      'href',
      '/products'
    );
  });

  it('renders custom message and link text', () => {
    render(
      <MemoryRouter>
        <AnnouncementBar message="Sale today" link="/deals" linkText="View deals" />
      </MemoryRouter>
    );

    expect(screen.getByText('Sale today')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'View deals' })).toHaveAttribute('href', '/deals');
  });

  it('hides link when link is empty', () => {
    render(
      <MemoryRouter>
        <AnnouncementBar message="Hello" link="" />
      </MemoryRouter>
    );

    expect(screen.queryByRole('link')).not.toBeInTheDocument();
  });

  it('dismisses when dismiss button clicked', () => {
    render(
      <MemoryRouter>
        <AnnouncementBar dismissible />
      </MemoryRouter>
    );

    fireEvent.click(screen.getByRole('button', { name: 'announcement.dismiss' }));
    expect(screen.queryByText('announcement.freeShipping')).not.toBeInTheDocument();
  });
});
