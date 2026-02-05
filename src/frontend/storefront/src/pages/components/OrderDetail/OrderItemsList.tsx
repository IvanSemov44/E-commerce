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

export default function OrderItemsList({ items }: OrderItemsListProps) {
  return (
    <Card variant="elevated" padding="lg">
      <h2 className={styles.title}>Order Items</h2>

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

            <div>
              <p className={styles.itemName}>{item.productName}</p>
              <p className={styles.itemQuantity}>Quantity: {item.quantity}</p>
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
