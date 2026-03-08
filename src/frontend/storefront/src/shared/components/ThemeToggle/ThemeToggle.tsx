import { useEffect, useState, useRef, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  SunIcon,
  MoonIcon,
  ComputerIcon,
  ChevronDownIcon,
  CheckIcon,
} from '@/shared/components/icons';
import styles from './ThemeToggle.module.css';

type Theme = 'light' | 'dark' | 'system';

interface ThemeToggleProps {
  /** Additional CSS class names */
  className?: string;
  /** Size variant */
  size?: 'sm' | 'md' | 'lg';
  /** Use dropdown mode (default) or simple cycle button */
  variant?: 'dropdown' | 'cycle';
}

interface ThemeOption {
  value: Theme;
  label: string;
  description: string;
  icon: React.ReactNode;
}

interface ThemeCycleButtonProps {
  className: string;
  size: 'sm' | 'md' | 'lg';
  label: string;
  icon: React.ReactNode;
  ariaLabel: string;
  onClick: () => void;
}

function ThemeCycleButton({
  className,
  size,
  label,
  icon,
  ariaLabel,
  onClick,
}: ThemeCycleButtonProps) {
  return (
    <button
      onClick={onClick}
      className={`${styles.toggle} ${styles[size]} ${className}`}
      aria-label={ariaLabel}
      title={`Theme: ${label}`}
    >
      <div className={styles.iconWrapper}>{icon}</div>
      <span className={styles.label}>{label}</span>
    </button>
  );
}

interface ThemeDropdownProps {
  className: string;
  size: 'sm' | 'md' | 'lg';
  dropdownRef: React.RefObject<HTMLDivElement | null>;
  buttonRef: React.RefObject<HTMLButtonElement | null>;
  isOpen: boolean;
  theme: Theme;
  currentIcon?: React.ReactNode;
  themeOptions: ThemeOption[];
  toggleDropdown: () => void;
  onThemeChange: (theme: Theme) => void;
  optionsLabel: string;
  title: string;
}

function ThemeDropdown({
  className,
  size,
  dropdownRef,
  buttonRef,
  isOpen,
  theme,
  currentIcon,
  themeOptions,
  toggleDropdown,
  onThemeChange,
  optionsLabel,
  title,
}: ThemeDropdownProps) {
  return (
    <div className={`${styles.wrapper} ${className}`} ref={dropdownRef}>
      <button
        ref={buttonRef}
        onClick={toggleDropdown}
        className={`${styles.toggle} ${styles[size]}`}
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        aria-label={`Theme: ${theme}. Click to change.`}
      >
        <div className={styles.iconWrapper}>{currentIcon}</div>
        <span className={styles.label}>{theme}</span>
        <ChevronDownIcon className={`${styles.chevron} ${isOpen ? styles.chevronOpen : ''}`} />
      </button>

      {isOpen && (
        <div className={styles.dropdown} role="listbox" aria-label={optionsLabel}>
          <div className={styles.dropdownHeader}>
            <span>{title}</span>
          </div>
          {themeOptions.map((option) => (
            <button
              key={option.value}
              onClick={() => onThemeChange(option.value)}
              className={`${styles.option} ${theme === option.value ? styles.optionActive : ''}`}
              role="option"
              aria-selected={theme === option.value}
            >
              <span className={styles.optionIcon}>{option.icon}</span>
              <span className={styles.optionContent}>
                <span className={styles.optionLabel}>{option.label}</span>
                <span className={styles.optionDescription}>{option.description}</span>
              </span>
              {theme === option.value && (
                <span className={styles.checkIcon}>
                  <CheckIcon />
                </span>
              )}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

/**
 * ThemeToggle Component
 *
 * A professional, accessible theme selector with:
 * - Dropdown menu with clear options (like GitHub/VS Code)
 * - Light/Dark/System mode support
 * - Smooth transitions between themes
 * - Persistent theme preference via localStorage
 * - System preference detection
 * - Keyboard navigation support
 */
export function ThemeToggle({
  className = '',
  size = 'md',
  variant = 'dropdown',
}: ThemeToggleProps) {
  const { t } = useTranslation();

  // Initialize theme from localStorage with lazy initializer
  const [theme, setTheme] = useState<Theme>(() => {
    if (typeof window !== 'undefined') {
      return (localStorage.getItem('theme') as Theme) || 'system';
    }
    return 'system';
  });

  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  // Theme options with icons
  const themeOptions: ThemeOption[] = [
    {
      value: 'light',
      label: t('theme.light'),
      description: t('theme.lightDescription'),
      icon: <SunIcon />,
    },
    {
      value: 'dark',
      label: t('theme.dark'),
      description: t('theme.darkDescription'),
      icon: <MoonIcon />,
    },
    {
      value: 'system',
      label: t('theme.system'),
      description: t('theme.systemDescription'),
      icon: <ComputerIcon />,
    },
  ];

  // Get the actual theme based on system preference
  const getResolvedTheme = useCallback((themeValue: Theme): 'light' | 'dark' => {
    if (themeValue === 'system') {
      return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    return themeValue;
  }, []);

  // Apply theme to document
  const applyTheme = useCallback(
    (themeValue: Theme) => {
      const resolved = getResolvedTheme(themeValue);
      document.documentElement.setAttribute('data-theme', resolved);
    },
    [getResolvedTheme]
  );

  // Apply theme when it changes
  useEffect(() => {
    applyTheme(theme);
  }, [theme, applyTheme]);

  // Listen for system preference changes
  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    const handleChange = () => {
      if (theme === 'system') {
        applyTheme('system');
      }
    };

    mediaQuery.addEventListener('change', handleChange);
    return () => mediaQuery.removeEventListener('change', handleChange);
  }, [theme, applyTheme]);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen]);

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (!isOpen) return;

      if (event.key === 'Escape') {
        setIsOpen(false);
        buttonRef.current?.focus();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleKeyDown);
    }
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen]);

  const handleThemeChange = (newTheme: Theme) => {
    setTheme(newTheme);
    localStorage.setItem('theme', newTheme);
    applyTheme(newTheme);
    setIsOpen(false);
    buttonRef.current?.focus();
  };

  const toggleDropdown = () => {
    setIsOpen(!isOpen);
  };

  const getCurrentIcon = () => {
    const currentTheme = themeOptions.find((t) => t.value === theme);
    return currentTheme?.icon;
  };

  // Simple cycle variant (original behavior)
  if (variant === 'cycle') {
    const cycleTheme = () => {
      const themes: Theme[] = ['light', 'dark', 'system'];
      const currentIndex = themes.indexOf(theme);
      const nextTheme = themes[(currentIndex + 1) % themes.length];
      handleThemeChange(nextTheme);
    };

    return (
      <ThemeCycleButton
        className={className}
        size={size}
        label={theme}
        icon={getCurrentIcon()}
        ariaLabel={`${t('theme.currentTheme', { theme: t('theme.' + theme) })}`}
        onClick={cycleTheme}
      />
    );
  }

  // Dropdown variant (default - professional style)
  return (
    <ThemeDropdown
      className={className}
      size={size}
      dropdownRef={dropdownRef}
      buttonRef={buttonRef}
      isOpen={isOpen}
      theme={theme}
      currentIcon={getCurrentIcon()}
      themeOptions={themeOptions}
      toggleDropdown={toggleDropdown}
      onThemeChange={handleThemeChange}
      optionsLabel={t('theme.themeOptions')}
      title={t('theme.title')}
    />
  );
}

export default ThemeToggle;
