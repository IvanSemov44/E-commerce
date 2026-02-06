/**
 * Skeleton Component - Base skeleton loader with shimmer animation
 * Generic skeletal UI for loading states
 */

import styles from './Skeleton.module.css';

interface SkeletonProps {
  width?: string | number;
  height?: string | number;
  variant?: 'circle' | 'text' | 'rectangular';
  className?: string;
}

export default function Skeleton({
  width = '100%',
  height = '1rem',
  variant = 'rectangular',
  className = '',
}: SkeletonProps) {
  const style: React.CSSProperties = {
    width: typeof width === 'number' ? `${width}px` : width,
    height: typeof height === 'number' ? `${height}px` : height,
  };

  return (
    <div
      className={`${styles.skeleton} ${styles[variant]} ${className}`}
      style={style}
      aria-busy="true"
      aria-label="Loading"
    />
  );
}
