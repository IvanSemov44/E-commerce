import { useState, useRef, useEffect } from 'react';
import { useTranslation } from '@/shared/hooks/useTranslation';
import { languages, type Language } from '@/shared/i18n';
import styles from './LanguageSwitcher.module.css';

interface LanguageSwitcherProps {
  /** Additional CSS class names */
  className?: string;
  /** Size variant */
  size?: 'sm' | 'md' | 'lg';
}

/**
 * LanguageSwitcher Component
 * 
 * A professional language selector matching ThemeToggle style with:
 * - Dropdown menu with clear options
 * - Language flag icons
 * - Persistent language preference via localStorage
 * - Size variants (sm, md, lg)
 */
export function LanguageSwitcher({ 
  className = '', 
  size = 'md'
}: LanguageSwitcherProps) {
  const { currentLanguage, changeLanguage } = useTranslation();
  const [mounted, setMounted] = useState(false);
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  // Initialize mounted state
  useEffect(() => {
    setMounted(true);
  }, []);

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

  const handleLanguageChange = async (lang: Language) => {
    if (lang !== currentLanguage) {
      await changeLanguage(lang);
    }
    setIsOpen(false);
    buttonRef.current?.focus();
  };

  const toggleDropdown = () => {
    setIsOpen(!isOpen);
  };

  const currentLang = languages.find(l => l.code === currentLanguage) || languages[0];

  // Get current language flag icon
  const getCurrentIcon = () => {
    const lang = languages.find(l => l.code === currentLanguage);
    return lang?.code.toUpperCase() || 'EN';
  };

  // Prevent hydration mismatch
  if (!mounted) {
    return (
      <div className={`${styles.wrapper} ${className}`}>
        <button 
          className={`${styles.toggle} ${styles[size]}`}
          disabled
        >
          <span className={styles.iconPlaceholder}>EN</span>
        </button>
      </div>
    );
  }

  return (
    <div className={`${styles.wrapper} ${className}`} ref={dropdownRef}>
      <button
        ref={buttonRef}
        onClick={toggleDropdown}
        className={`${styles.toggle} ${styles[size]}`}
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        aria-label={`Language: ${currentLang.nativeName}. Click to change.`}
      >
        <span className={styles.iconWrapper}>
          {getCurrentIcon()}
        </span>
        <span className={styles.label}>{currentLang.nativeName}</span>
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
          aria-label="Language options"
        >
          <div className={styles.dropdownHeader}>
            <span>Language</span>
          </div>
          {languages.map((lang) => (
            <button
              key={lang.code}
              onClick={() => handleLanguageChange(lang.code)}
              className={`${styles.option} ${currentLanguage === lang.code ? styles.optionActive : ''}`}
              role="option"
              aria-selected={currentLanguage === lang.code}
            >
              <span className={styles.optionIcon}>
                {lang.code.toUpperCase()}
              </span>
              <span className={styles.optionContent}>
                <span className={styles.optionLabel}>{lang.nativeName}</span>
                <span className={styles.optionDescription}>{lang.name}</span>
              </span>
              {currentLanguage === lang.code && (
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

export default LanguageSwitcher;
