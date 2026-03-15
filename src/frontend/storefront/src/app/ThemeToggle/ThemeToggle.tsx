import { useEffect, useState, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import {
  SunIcon,
  MoonIcon,
  ComputerIcon,
  ChevronDownIcon,
  CheckIcon,
} from '@/shared/components/icons';
import { useTheme } from '@/app/providers/ThemeProvider';
import styles from './ThemeToggle.module.css';

type Theme = 'light' | 'dark' | 'system';

interface ThemeToggleProps {
  size?: 'sm' | 'md';
}

interface ThemeOption {
  value: Theme;
  label: string;
  description: string;
  icon: React.ReactNode;
}

/**
 * ThemeToggle - Dropdown theme selector
 * Supports light/dark/system modes with accessible dropdown UI
 */
export function ThemeToggle({ size = 'md' }: ThemeToggleProps) {
  const { t } = useTranslation();
  const { theme, setTheme } = useTheme();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

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

  const currentTheme = themeOptions.find((t) => t.value === theme);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [isOpen]);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (isOpen && event.key === 'Escape') {
        setIsOpen(false);
        buttonRef.current?.focus();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleKeyDown);
      return () => document.removeEventListener('keydown', handleKeyDown);
    }
  }, [isOpen]);

  const handleThemeChange = (newTheme: Theme) => {
    setTheme(newTheme);
    setIsOpen(false);
    buttonRef.current?.focus();
  };

  return (
    <div className={styles.wrapper} ref={dropdownRef}>
      <button
        ref={buttonRef}
        onClick={() => setIsOpen(!isOpen)}
        className={`${styles.toggle} ${styles[size]}`}
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        aria-label={`Theme: ${theme}. Click to change.`}
      >
        <div className={styles.iconWrapper}>{currentTheme?.icon}</div>
        <span className={styles.label}>{theme}</span>
        <ChevronDownIcon className={`${styles.chevron} ${isOpen ? styles.chevronOpen : ''}`} />
      </button>

      {isOpen && (
        <div className={styles.dropdown} role="listbox" aria-label={t('theme.themeOptions')}>
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
