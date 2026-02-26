import Card from '../../../components/ui/Card';
import styles from './OrderItemsList.module.css';

interface OrderItem {
  productName: string;
  productImageUrl?: string;
  quantity: number;
  unitPrice?: number;
  totalPrice?: number;
}

interface OrderItemsListProps {
  items: OrderItem[];
}

// Icons
const PackageIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.titleIcon}>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
  </svg>
);

export default function OrderItemsList({ items }: OrderItemsListProps) {
  return (
    <Card variant="elevated" padding="lg">
      <h2 className={styles.title}>
        <PackageIcon />
        Order Items
      </h2>

      <div className={styles.itemsList}>
        {items.map((item, index) => (
          <div
            key={index}
            className={`${styles.item} ${
              index < items.length - 1 ? styles.itemWithBorder : ''
            }`}
          >
            {item.productImageUrl && (
              <img
                src={item.productImageUrl}
                alt={item.productName}
                className={styles.itemImage}
              />
            )}

            <div className={styles.itemInfo}>
              <p className={styles.itemName}>{item.productName}</p>
              <div className={styles.quantityBadge}>
                <span className={styles.quantityLabel}>Qty</span>
                <span className={styles.quantityValue}>{item.quantity}</span>
              </div>
            </div>

            <div className={styles.itemPrice}>
              <p className={styles.priceLabel}>Price</p>
              <p className={styles.priceValue}>
                ${item.unitPrice?.toFixed(2) || '0.00'}
              </p>
            </div>

            <div className={styles.itemTotal}>
              <p className={styles.priceLabel}>Total</p>
              <p className={styles.totalValue}>
                ${item.totalPrice?.toFixed(2) || '0.00'}
              </p>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}
