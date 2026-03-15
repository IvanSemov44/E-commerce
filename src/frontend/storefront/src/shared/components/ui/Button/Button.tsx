import * as React from 'react';
import { SpinnerIcon } from '@/shared/components/icons';
import styles from './Button.module.css';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost' | 'destructive' | 'outline';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

// Helper function to combine variant and size classes
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

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    { className, variant = 'primary', size = 'md', isLoading, children, disabled, ...props },
    ref
  ) => {
    const buttonClasses = getButtonClasses(variant, size);
    const combinedClassName = className ? `${buttonClasses} ${className}` : buttonClasses;
    const isDisabled = disabled || isLoading;

    return (
      <button className={combinedClassName} ref={ref} disabled={isDisabled} {...props}>
        {isLoading && <SpinnerIcon className={styles.spinner} fill="none" aria-hidden="true" />}
        {children}
      </button>
    );
  }
);

Button.displayName = 'Button';
