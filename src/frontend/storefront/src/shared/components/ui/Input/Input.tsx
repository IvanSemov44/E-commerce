import * as React from 'react';
import { useId } from 'react';
import styles from './Input.module.css';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  variant?: 'default' | 'subtle' | 'error';
  label?: string;
  error?: string;
  icon?: React.ReactNode;
}

// Helper function to get variant class
function getInputVariantClass(variant: 'default' | 'subtle' | 'error' = 'default'): string {
  const variantMap: Record<string, string> = {
    default: styles.inputDefault,
    subtle: styles.inputSubtle,
    error: styles.inputError,
  };
  return variantMap[variant];
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, variant = 'default', label, error, icon, id, ...props }, ref) => {
    const generatedId = useId();
    const inputId = id || `input-${generatedId}`;
    const variantToUse = error ? 'error' : variant;
    const variantClass = getInputVariantClass(variantToUse);
    const inputClasses = [styles.input, variantClass, icon ? styles.inputWithIcon : '']
      .filter(Boolean)
      .join(' ');
    const combinedClassName = className ? `${inputClasses} ${className}` : inputClasses;

    return (
      <div className={styles.wrapper}>
        {label && (
          <label htmlFor={inputId} className={styles.label}>
            {label}
          </label>
        )}
        <div className={styles.inputContainer}>
          {icon && <div className={styles.icon}>{icon}</div>}
          <input id={inputId} ref={ref} className={combinedClassName} {...props} />
        </div>
        {error && <p className={styles.error}>{error}</p>}
      </div>
    );
  }
);

Input.displayName = 'Input';
