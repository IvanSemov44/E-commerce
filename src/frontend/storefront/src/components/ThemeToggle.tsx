import { useEffect, useState, useRef } from 'react';
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
  const [theme, setTheme] = useState<Theme>('system');
  const [mounted, setMounted] = useState(false);
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  // Theme options with icons
  const themeOptions: ThemeOption[] = [
    {
      value: 'light',
      label: 'Light',
      description: 'Light appearance',
      icon: (
        <svg fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z"
          />
        </svg>
      ),
    },
    {
      value: 'dark',
      label: 'Dark',
      description: 'Dark appearance',
      icon: (
        <svg fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z"
          />
        </svg>
      ),
    },
    {
      value: 'system',
      label: 'System',
      description: 'Follow system settings',
      icon: (
        <svg fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M9 17.25v1.007a3 3 0 01-.879 2.122L7.5 21h9l-.621-.621A3 3 0 0115 18.257V17.25m6-12V15a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 15V5.25m18 0A2.25 2.25 0 0018.75 3H5.25A2.25 2.25 0 003 5.25m18 0V12a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 12V5.25"
          />
        </svg>
      ),
    },
  ];

  // Get the actual theme based on system preference
  const getResolvedTheme = (themeValue: Theme): 'light' | 'dark' => {
    if (themeValue === 'system') {
      return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    return themeValue;
  };

  // Apply theme to document
  const applyTheme = (themeValue: Theme) => {
    const resolved = getResolvedTheme(themeValue);
    document.documentElement.setAttribute('data-theme', resolved);
  };

  // Initialize theme from localStorage or system preference
  useEffect(() => {
    const savedTheme = localStorage.getItem('theme') as Theme | null;
    const initialTheme = savedTheme || 'system';
    setTheme(initialTheme);
    applyTheme(initialTheme);
    setMounted(true);
  }, []);

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
  }, [theme]);

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
        aria-label={`Current theme: ${theme}. Click to change.`}
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
        <svg 
          className={`${styles.chevron} ${isOpen ? styles.chevronOpen : ''}`}
          fill="none" 
          viewBox="0 0 24 24" 
          strokeWidth={1.5} 
          stroke="currentColor"
        >
          <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
        </svg>
      </button>

      {isOpen && (
        <div 
          className={styles.dropdown}
          role="listbox"
          aria-label="Theme options"
        >
          <div className={styles.dropdownHeader}>
            <span>Appearance</span>
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
                  <svg fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                  </svg>
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
