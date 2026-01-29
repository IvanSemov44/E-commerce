import { Link, useLocation } from 'react-router-dom';
import styles from './Sidebar.module.css';

const menuItems = [
  {
    icon: '📊',
    label: 'Dashboard',
    path: '/',
  },
  {
    icon: '📦',
    label: 'Products',
    path: '/products',
  },
  {
    icon: '🛒',
    label: 'Orders',
    path: '/orders',
  },
  {
    icon: '⭐',
    label: 'Reviews',
    path: '/reviews',
  },
  {
    icon: '👥',
    label: 'Customers',
    path: '/customers',
  },
  {
    icon: '🎟️',
    label: 'Promo Codes',
    path: '/promo-codes',
  },
  {
    icon: '📊',
    label: 'Inventory',
    path: '/inventory',
  },
  {
    icon: '⚙️',
    label: 'Settings',
    path: '/settings',
  },
];

export default function Sidebar() {
  const location = useLocation();

  return (
    <aside className={styles.sidebar}>
      <div className={styles.logo}>
        <div className={styles.logoBadge}>E</div>
        <span>Admin</span>
      </div>

      <nav className={styles.nav}>
        {menuItems.map((item) => (
          <Link
            key={item.path}
            to={item.path}
            className={`${styles.navItem} ${location.pathname === item.path ? styles.navItemActive : ''}`}
          >
            <span className={styles.navIcon}>{item.icon}</span>
            <span className={styles.navLabel}>{item.label}</span>
          </Link>
        ))}
      </nav>
    </aside>
  );
}
