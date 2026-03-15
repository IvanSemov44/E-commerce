import * as React from 'react';
import styles from './Card.module.css';

interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'bordered' | 'elevated' | 'ghost';
  padding?: 'none' | 'sm' | 'md' | 'lg';
}

// Helper function to get variant and padding classes
function getCardClasses(
  variant: 'default' | 'bordered' | 'elevated' | 'ghost' = 'default',
  padding: 'none' | 'sm' | 'md' | 'lg' = 'md'
): string {
  const variantMap: Record<string, string> = {
    default: styles.cardDefault,
    bordered: styles.cardBordered,
    elevated: styles.cardElevated,
    ghost: styles.cardGhost,
  };

  const paddingMap: Record<string, string> = {
    none: styles.paddingNone,
    sm: styles.paddingSmall,
    md: styles.paddingMedium,
    lg: styles.paddingLarge,
  };

  return `${styles.card} ${variantMap[variant]} ${paddingMap[padding]}`;
}

export const Card = React.forwardRef<HTMLDivElement, CardProps>(
  ({ className, variant = 'default', padding = 'md', ...props }, ref) => {
    const cardClasses = getCardClasses(variant, padding);
    const combinedClassName = className ? `${cardClasses} ${className}` : cardClasses;

    return <div ref={ref} className={combinedClassName} {...props} />;
  }
);

Card.displayName = 'Card';
