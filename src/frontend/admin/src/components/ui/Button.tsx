import * as React from 'react';
import styles from './Button.module.css';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost' | 'destructive' | 'outline';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

function getButtonClasses(
  variant: 'primary' | 'secondary' | 'ghost' | 'destructive' | 'outline' = 'primary',
  size: 'sm' | 'md' | 'lg' = 'md'
): string {
  const variantMap: Record<string, string> = {
    primary: styles.buttonPrimary,
    secondary: styles.buttonSecondary,
    ghost: styles.buttonGhost,
    destructive: styles.buttonDestructive,
    outline: styles.buttonOutline,
  };

  const sizeMap: Record<string, string> = {
    sm: styles.buttonSmall,
    md: styles.buttonMedium,
    lg: styles.buttonLarge,
  };

  return `${styles.button} ${variantMap[variant]} ${sizeMap[size]}`;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'primary', size = 'md', isLoading, children, disabled, ...props }, ref) => {
    const buttonClasses = getButtonClasses(variant, size);
    const combinedClassName = className ? `${buttonClasses} ${className}` : buttonClasses;
    const isDisabled = disabled || isLoading;

    return (
      <button
        className={combinedClassName}
        ref={ref}
        disabled={isDisabled}
        style={isDisabled ? { opacity: 0.5, cursor: 'not-allowed', pointerEvents: 'none' } : undefined}
        {...props}
      >
        {isLoading && (
          <svg
            className={styles.spinner}
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle className={styles.spinnerCircle} cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path
              className={styles.spinnerPath}
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
        )}
        {children}
      </button>
    );
  }
);

Button.displayName = 'Button';

export { Button };
export default Button;
