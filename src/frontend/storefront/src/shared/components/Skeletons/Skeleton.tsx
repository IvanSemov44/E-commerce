/**
 * Skeleton Component - Base skeleton loader with configurable animation
 * Generic skeletal UI for loading states
 */

import styles from './Skeleton.module.css';

interface SkeletonProps {
  width?: string | number;
  height?: string | number;
  variant?: 'circle' | 'text' | 'rectangular' | 'rounded';
  animation?: 'pulse' | 'wave' | 'none';
  className?: string;
}

export default function Skeleton({
  width = '100%',
  height = '1rem',
  variant = 'rectangular',
  animation = 'pulse',
  className = '',
}: SkeletonProps) {
  const style: React.CSSProperties = {
    width: typeof width === 'number' ? `${width}px` : width,
    height: typeof height === 'number' ? `${height}px` : height,
  };

  return (
    <span
      className={`${styles.skeleton} ${styles[variant]} ${styles[animation]} ${className}`}
      style={style}
      aria-busy="true"
      aria-label="Loading"
    />
  );
}
