import styles from './PageHeader.module.css';

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  showAccent?: boolean;
  icon?: React.ReactNode;
  badge?: string;
  variant?: 'default' | 'leftAligned' | 'compact' | 'hero';
  metaItems?: Array<{
    icon?: React.ReactNode;
    text: string;
  }>;
}

export default function PageHeader({
  title,
  subtitle,
  showAccent = true,
  icon,
  badge,
  variant = 'default',
  metaItems,
}: PageHeaderProps) {
  const containerClasses = [
    styles.container,
    variant === 'leftAligned' && styles.leftAligned,
    variant === 'compact' && styles.compact,
    variant === 'hero' && styles.hero,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={containerClasses}>
      {badge && (
        <div className={styles.badge}>
          <span className={styles.badgeIcon}>✨</span>
          {badge}
        </div>
      )}

      <h1 className={icon ? styles.titleWithIcon : styles.title}>
        {icon && <span className={styles.titleIcon}>{icon}</span>}
        {title}
      </h1>

      {showAccent && <div className={styles.accentLine}></div>}

      {subtitle && <p className={styles.subtitle}>{subtitle}</p>}

      {metaItems && metaItems.length > 0 && (
        <div className={styles.metaInfo}>
          {metaItems.map((item, index) => (
            <div key={index} className={styles.metaItem}>
              {item.icon && <span className={styles.metaIcon}>{item.icon}</span>}
              <span>{item.text}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
