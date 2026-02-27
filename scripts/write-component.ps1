$content = @'
import { useState, useRef, useEffect } from 'react';
import { useTranslation } from '../../hooks/useTranslation';
import { languages, type Language } from '../../i18n';
import styles from './LanguageSwitcher.module.css';

/**
 * LanguageSwitcher Component
 * 
 * A toggle button for switching between languages.
 * Shows current language and allows switching to other available languages.
 * 
 * Features:
 * - Displays current language flag/name
 * - Dropdown to switch languages
 * - Persists selection to localStorage
 * - Updates Redux state for global access
 */
export const LanguageSwitcher: React.FC = () => {
  const { currentLanguage, changeLanguage } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLanguageChange = async (lang: Language) => {
    if (lang !== currentLanguage) {
      await changeLanguage(lang);
    }
    setIsOpen(false);
  };

  const currentLang = languages.find(l => l.code === currentLanguage) || languages[0];

  return (
    <div className={styles.container} ref={dropdownRef}>
      <button
        className={styles.trigger}
        onClick={() => setIsOpen(!isOpen)}
        aria-label="Select language"
        aria-expanded={isOpen}
      >
        <span className={styles.flag}>{currentLang.code.toUpperCase()}</span>
        <span className={styles.label}>{currentLang.nativeName}</span>
        <span className={`${styles.arrow} ${isOpen ? styles.arrowUp : ''}`}>▼</span>
      </button>

      {isOpen && (
        <ul className={styles.dropdown}>
          {languages.map((lang) => (
            <li key={lang.code}>
              <button
                className={`${styles.option} ${lang.code === currentLanguage ? styles.active : ''}`}
                onClick={() => handleLanguageChange(lang.code)}
              >
                <span className={styles.flag}>{lang.code.toUpperCase()}</span>
                <span className={styles.optionLabel}>{lang.nativeName}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default LanguageSwitcher;
'@

$path = "c:\Users\ivans\Desktop\Dev\E-commerce\src\frontend\storefront\src\components\LanguageSwitcher\LanguageSwitcher.tsx"
[System.IO.File]::WriteAllText($path, $content, [System.Text.UTF8Encoding]::new($true))
Write-Host "File written successfully"
