import Card from '../../../components/ui/Card';
import CartItem from '../../../components/CartItem';
import styles from './CartItemList.module.css';

interface DisplayCartItem {
  id: string;
  name: string;
  slug: string;
  price: number;
  quantity: number;
  maxStock: number;
  image: string;
  compareAtPrice?: number;
  cartItemId?: string;
}

interface CartItemListProps {
  items: DisplayCartItem[];
  onUpdateQuantity: (id: string, quantity: number) => void;
  onRemove: (id: string) => void;
}

export default function CartItemList({ items, onUpdateQuantity, onRemove }: CartItemListProps) {
  return (
    <Card variant="elevated" padding="lg">
      <h2 className={styles.title}>
        Items ({items.length} {items.length === 1 ? 'product' : 'products'})
      </h2>
      <div className={styles.itemsList}>
        {items.map((item) => (
          <CartItem
            key={item.id}
            item={item}
            onUpdateQuantity={onUpdateQuantity}
            onRemove={onRemove}
          />
        ))}
      </div>
    </Card>
  );
}
