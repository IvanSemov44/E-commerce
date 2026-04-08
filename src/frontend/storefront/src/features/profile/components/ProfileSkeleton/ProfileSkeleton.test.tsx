import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { ProfileSkeleton } from './ProfileSkeleton';

describe('ProfileSkeleton', () => {
  it('renders profile container', () => {
    const { container } = render(<ProfileSkeleton />);
    const profileContainer = container.querySelector('[class*="profileContainer"]');
    expect(profileContainer).toBeInTheDocument();
  });

  it('renders profile header section', () => {
    const { container } = render(<ProfileSkeleton />);
    const profileHeader = container.querySelector('[class*="profileHeader"]');
    expect(profileHeader).toBeInTheDocument();
  });

  it('renders header info section', () => {
    const { container } = render(<ProfileSkeleton />);
    const headerInfo = container.querySelector('[class*="headerInfo"]');
    expect(headerInfo).toBeInTheDocument();
  });

  it('renders profile form section', () => {
    const { container } = render(<ProfileSkeleton />);
    const profileForm = container.querySelector('[class*="profileForm"]');
    expect(profileForm).toBeInTheDocument();
  });

  it('renders multiple form groups', () => {
    const { container } = render(<ProfileSkeleton />);
    const formGroups = container.querySelectorAll('[class*="formGroup"]');
    expect(formGroups.length).toBeGreaterThanOrEqual(4);
  });

  it('renders circle variant for avatar', () => {
    const { container } = render(<ProfileSkeleton />);
    const firstSkeleton = container.querySelector('span[class*="skeleton"]');
    expect(firstSkeleton?.className).toMatch(/circle/);
  });
});
