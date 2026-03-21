import { useTranslation } from 'react-i18next';
import styles from './PasswordStrengthIndicator.module.css';

const RULES = [
  { id: 'length', test: (p: string) => p.length >= 8, labelKey: 'auth.passwordMinLength' },
  { id: 'upper', test: (p: string) => /[A-Z]/.test(p), labelKey: 'auth.passwordUppercase' },
  { id: 'lower', test: (p: string) => /[a-z]/.test(p), labelKey: 'auth.passwordLowercase' },
  { id: 'digit', test: (p: string) => /[0-9]/.test(p), labelKey: 'auth.passwordDigit' },
] as const;

const STRENGTH = [
  { labelKey: 'auth.strengthWeak', scoreClass: styles.score1 },
  { labelKey: 'auth.strengthFair', scoreClass: styles.score2 },
  { labelKey: 'auth.strengthGood', scoreClass: styles.score3 },
  { labelKey: 'auth.strengthStrong', scoreClass: styles.score4 },
] as const;

interface Props {
  password: string;
}

export function PasswordStrengthIndicator({ password }: Props) {
  const { t } = useTranslation();

  if (!password) return null;

  const met = RULES.map((rule) => rule.test(password));
  const score = met.filter(Boolean).length; // 0–4
  const strength = score > 0 ? STRENGTH[score - 1] : null;

  return (
    <div className={styles.container} role="status" aria-label={t('auth.passwordStrength')}>
      {/* Segmented bar */}
      <div className={styles.barRow}>
        <div className={styles.bar} aria-hidden="true">
          {RULES.map((_, i) => (
            <div
              key={i}
              className={`${styles.segment} ${i < score && strength ? strength.scoreClass : ''}`}
            />
          ))}
        </div>
        {strength && (
          <span className={`${styles.label} ${strength.scoreClass}`}>{t(strength.labelKey)}</span>
        )}
      </div>

      {/* Rule checklist */}
      <ul className={styles.rules}>
        {RULES.map((rule, i) => (
          <li key={rule.id} className={`${styles.rule} ${met[i] ? styles.met : ''}`}>
            <svg className={styles.ruleIcon} viewBox="0 0 16 16" fill="none" aria-hidden="true">
              {met[i] ? (
                <path
                  d="M3 8l3.5 3.5L13 4"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              ) : (
                <circle cx="8" cy="8" r="5.5" stroke="currentColor" strokeWidth="1.5" />
              )}
            </svg>
            {t(rule.labelKey)}
          </li>
        ))}
      </ul>
    </div>
  );
}
