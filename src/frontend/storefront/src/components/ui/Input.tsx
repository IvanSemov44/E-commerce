import * as React from 'react';
import styles from './Input.module.css';

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
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

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, variant = 'default', label, error, icon, ...props }, ref) => {
    const variantToUse = error ? 'error' : variant;
    const variantClass = getInputVariantClass(variantToUse);
    const inputClasses = [styles.input, variantClass, icon ? styles.inputWithIcon : ''].filter(Boolean).join(' ');
    const combinedClassName = className ? `${inputClasses} ${className}` : inputClasses;

    return (
      <div className={styles.wrapper}>
        {label && <label className={styles.label}>{label}</label>}
        <div className={styles.inputContainer}>
          {icon && <div className={styles.icon}>{icon}</div>}
          <input ref={ref} className={combinedClassName} {...props} />
        </div>
        {error && <p className={styles.error}>{error}</p>}
      </div>
    );
  }
);

Input.displayName = 'Input';

export { Input };
export default Input;
