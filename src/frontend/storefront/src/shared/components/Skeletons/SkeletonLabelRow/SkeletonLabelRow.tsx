import Skeleton from '../Skeleton/Skeleton';
import styles from './SkeletonLabelRow.module.css';

interface SkeletonLabelRowItem {
  width: string | number;
  height: number;
}

interface SkeletonLabelRowProps {
  items: SkeletonLabelRowItem[];
  between?: boolean;
  wrap?: boolean;
  className?: string;
}

export default function SkeletonLabelRow({
  items,
  between = true,
  wrap = false,
  className,
}: SkeletonLabelRowProps) {
  const classNames = [
    styles.row,
    between ? styles.between : '',
    wrap ? styles.wrap : '',
    className ?? '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={classNames}>
      {items.map((item, i) => (
        <Skeleton
          key={i}
          width={item.width}
          height={item.height}
          variant="rounded"
          animation="wave"
        />
      ))}
    </div>
  );
}
