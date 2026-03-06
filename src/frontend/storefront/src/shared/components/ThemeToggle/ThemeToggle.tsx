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
  variant = 'dropdown'
}: ThemeToggleProps) {
  const { t } = useTranslation();
  
  // Initialize theme from localStorage with lazy initializer
  const [theme, setTheme] = useState<Theme>(() => {
    if (typeof window !== 'undefined') {
      return (localStorage.getItem('theme') as Theme) || 'system';
    }
    return 'system';
  });
  
  const [mounted, setMounted] = useState(false);
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
  const applyTheme = useCallback((themeValue: Theme) => {
    const resolved = getResolvedTheme(themeValue);
    document.documentElement.setAttribute('data-theme', resolved);
  }, [getResolvedTheme]);

  // Initialize theme from localStorage or system preference
  // Using state initializer to avoid setState in effect
  useEffect(() => {
    // This only runs once on mount with the already-initialized theme
    const timer = setTimeout(() => setMounted(true), 0);
    return () => clearTimeout(timer);
  }, []); // Empty deps - theme is already initialized via useState

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
    const currentTheme = themeOptions.find(t => t.value === theme);
    return currentTheme?.icon;
  };

  // Prevent hydration mismatch
  if (!mounted) {
    return (
      <div className={`${styles.wrapper} ${className}`}>
        <button 
          className={`${styles.toggle} ${styles[size]}`}
          disabled
        >
          <div className={styles.iconPlaceholder} />
        </button>
      </div>
    );
  }

  // Simple cycle variant (original behavior)
  if (variant === 'cycle') {
    const cycleTheme = () => {
      const themes: Theme[] = ['light', 'dark', 'system'];
      const currentIndex = themes.indexOf(theme);
      const nextTheme = themes[(currentIndex + 1) % themes.length];
      handleThemeChange(nextTheme);
    };

    return (
      <button
        onClick={cycleTheme}
        className={`${styles.toggle} ${styles[size]} ${className}`}
        aria-label={`${t('theme.currentTheme', { theme: t('theme.' + theme) })}`}
        title={`Theme: ${theme}`}
      >
        <div className={styles.iconWrapper}>
          {getCurrentIcon()}
        </div>
        <span className={styles.label}>{theme}</span>
      </button>
    );
  }

  // Dropdown variant (default - professional style)
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
        <div className={styles.iconWrapper}>
          {getCurrentIcon()}
        </div>
        <span className={styles.label}>{theme}</span>
        <ChevronDownIcon className={`${styles.chevron} ${isOpen ? styles.chevronOpen : ''}`} />
      </button>

      {isOpen && (
        <div 
          className={styles.dropdown}
          role="listbox"
          aria-label={t('theme.themeOptions')}
        >
          <div className={styles.dropdownHeader}>
            <span>{t('theme.title')}</span>
          </div>
          {themeOptions.map((option) => (
            <button
              key={option.value}
              onClick={() => handleThemeChange(option.value)}
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

export default ThemeToggle;
