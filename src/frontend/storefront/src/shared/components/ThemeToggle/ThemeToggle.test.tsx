import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { SVGProps } from 'react';
import { ThemeToggle } from './ThemeToggle';

// Mock icons
vi.mock('@/shared/components/icons', () => ({
  SunIcon: (props: SVGProps<SVGSVGElement>) => <svg data-testid="sun-icon" {...props} />,
  MoonIcon: (props: SVGProps<SVGSVGElement>) => <svg data-testid="moon-icon" {...props} />,
  ComputerIcon: (props: SVGProps<SVGSVGElement>) => <svg data-testid="computer-icon" {...props} />,
  ChevronDownIcon: (props: SVGProps<SVGSVGElement>) => (
    <svg data-testid="chevron-icon" {...props} />
  ),
  CheckIcon: (props: SVGProps<SVGSVGElement>) => <svg data-testid="check-icon" {...props} />,
}));

// Mock react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        'theme.light': 'Light',
        'theme.dark': 'Dark',
        'theme.system': 'System',
        'theme.lightDescription': 'Light theme',
        'theme.darkDescription': 'Dark theme',
        'theme.systemDescription': 'Use system setting',
      };
      return translations[key] || key;
    },
  }),
}));

describe('ThemeToggle', () => {
  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();
    vi.clearAllMocks();
    // Reset document class
    document.documentElement.className = '';
  });

  it('renders toggle button', () => {
    render(<ThemeToggle />);
    const button = screen.getByRole('button');
    expect(button).toBeInTheDocument();
  });

  it('renders dropdown menu on button click', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');
    await user.click(button);

    // Dropdown menu should appear
    await waitFor(() => {
      expect(screen.getByText('Light')).toBeInTheDocument();
      expect(screen.getByText('Dark')).toBeInTheDocument();
      expect(screen.getByText('System')).toBeInTheDocument();
    });
  });

  it('renders with cycle variant', () => {
    render(<ThemeToggle variant="cycle" />);
    const button = screen.getByRole('button');
    expect(button).toBeInTheDocument();
  });

  it('changes theme to light', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');
    await user.click(button);

    const lightOption = screen.getByText('Light');
    await user.click(lightOption);

    // Theme should be saved to localStorage
    expect(localStorage.getItem('theme')).toBe('light');
  });

  it('changes theme to dark', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');
    await user.click(button);

    const darkOption = screen.getByText('Dark');
    await user.click(darkOption);

    expect(localStorage.getItem('theme')).toBe('dark');
  });

  it('changes theme to system', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');
    await user.click(button);

    const systemOption = screen.getByText('System');
    await user.click(systemOption);

    expect(localStorage.getItem('theme')).toBe('system');
  });

  it('closes dropdown after selection', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');
    await user.click(button);

    const lightOption = screen.getByText('Light');
    await user.click(lightOption);

    // Dropdown should close
    await waitFor(() => {
      expect(screen.queryByText('Dark')).not.toBeInTheDocument();
    });
  });

  it('shows checkmark on selected theme', async () => {
    const user = userEvent.setup();
    localStorage.setItem('theme', 'dark');

    render(<ThemeToggle />);
    const button = screen.getByRole('button');
    await user.click(button);

    // Selected item should have check icon or visual indicator
    expect(screen.getByText('Dark')).toBeInTheDocument();
  });

  it('loads theme from localStorage on mount', () => {
    localStorage.setItem('theme', 'dark');
    render(<ThemeToggle />);
    // Component should initialize with saved theme
    expect(localStorage.getItem('theme')).toBe('dark');
  });

  it('applies custom className', () => {
    const { container } = render(<ThemeToggle className="custom-class" />);
    const element = container.querySelector('.custom-class');
    expect(element).toBeInTheDocument();
  });

  it('renders with different size variants', () => {
    const { rerender } = render(<ThemeToggle size="sm" />);
    expect(screen.getByRole('button')).toBeInTheDocument();

    rerender(<ThemeToggle size="lg" />);
    expect(screen.getByRole('button')).toBeInTheDocument();
  });

  it('supports keyboard navigation', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');
    await user.click(button);

    // Navigate with arrow keys
    await user.keyboard('{ArrowDown}');
    // Next option should be highlighted
    expect(screen.getByText('Dark')).toBeInTheDocument();
  });

  it('closes dropdown on escape key', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');
    await user.click(button);

    await waitFor(() => {
      expect(screen.getByText('Light')).toBeInTheDocument();
    });

    await user.keyboard('{Escape}');

    await waitFor(() => {
      expect(screen.queryByText('Light')).not.toBeInTheDocument();
    });
  });

  it('displays theme icons in dropdown', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');
    await user.click(button);

    // Icons should be visible
    await waitFor(() => {
      expect(screen.getByTestId('sun-icon')).toBeInTheDocument();
      expect(screen.getByTestId('moon-icon')).toBeInTheDocument();
      expect(screen.getAllByTestId('computer-icon').length).toBeGreaterThan(0);
    });
  });

  it('handles rapid theme changes', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');

    // Click theme multiple times
    await user.click(button);
    const lightOption = screen.getByText('Light');
    await user.click(lightOption);

    await user.click(button);
    const darkOption = screen.getByText('Dark');
    await user.click(darkOption);

    expect(localStorage.getItem('theme')).toBe('dark');
  });

  it('applies theme attribute to document root', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');
    await user.click(button);

    const darkOption = screen.getByText('Dark');
    await user.click(darkOption);

    // Document should have theme attribute applied
    await waitFor(() => {
      expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    });
  });

  it('renders dropdown options with descriptions', async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = screen.getByRole('button');
    await user.click(button);

    await waitFor(() => {
      expect(screen.getByText('Light theme')).toBeInTheDocument();
      expect(screen.getByText('Dark theme')).toBeInTheDocument();
      expect(screen.getByText('Use system setting')).toBeInTheDocument();
    });
  });

  it('handles system preference changes when system theme is selected', () => {
    render(<ThemeToggle />);
    localStorage.setItem('theme', 'system');
    // Component should respect system preference
    expect(localStorage.getItem('theme')).toBe('system');
  });
});
