import type { ReactNode } from 'react';
import Skeleton from '../Skeleton/Skeleton';
import styles from './SkeletonCard.module.css';

interface SkeletonCardLine {
  width: string | number;
  height: number;
}

interface SkeletonCardProps {
  imageHeight: number;
  lines?: SkeletonCardLine[];
  className?: string;
  children?: ReactNode;
}

const DEFAULT_LINES: SkeletonCardLine[] = [
  { width: '80%', height: 16 },
  { width: '40%', height: 14 },
];

export default function SkeletonCard({
  imageHeight,
  lines = DEFAULT_LINES,
  className,
  children,
}: SkeletonCardProps) {
  return (
    <div className={`${styles.card}${className ? ` ${className}` : ''}`}>
      <Skeleton width="100%" height={imageHeight} variant="rounded" animation="wave" />
      {lines.map((line, i) => (
        <Skeleton
          key={i}
          width={line.width}
          height={line.height}
          variant="rounded"
          animation="wave"
        />
      ))}
      {children}
    </div>
  );
}
