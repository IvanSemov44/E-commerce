import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import PageHeader from './PageHeader';

describe('PageHeader', () => {
  it('renders title and optional subtitle', () => {
    render(<PageHeader title="Products" subtitle="Best picks" />);

    expect(screen.getByRole('heading', { name: 'Products' })).toBeInTheDocument();
    expect(screen.getByText('Best picks')).toBeInTheDocument();
  });

  it('renders badge and icon', () => {
    render(
      <PageHeader title="Orders" badge="New" icon={<span data-testid="title-icon">I</span>} />
    );

    expect(screen.getByText('New')).toBeInTheDocument();
    expect(screen.getByTestId('title-icon')).toBeInTheDocument();
  });

  it('hides accent line when showAccent is false', () => {
    const { container } = render(<PageHeader title="No Accent" showAccent={false} />);
    expect(container.querySelector('[class*="accentLine"]')).not.toBeInTheDocument();
  });

  it('renders meta items with icons', () => {
    render(
      <PageHeader
        title="Dashboard"
        metaItems={[
          { icon: <span data-testid="meta-1">i</span>, text: '10 items' },
          { text: 'Updated now' },
        ]}
      />
    );

    expect(screen.getByText('10 items')).toBeInTheDocument();
    expect(screen.getByText('Updated now')).toBeInTheDocument();
    expect(screen.getByTestId('meta-1')).toBeInTheDocument();
  });
});
