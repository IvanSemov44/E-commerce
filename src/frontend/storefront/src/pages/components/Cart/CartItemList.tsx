import { useTranslation } from 'react-i18next';
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
  const { t } = useTranslation();
  const itemText = items.length === 1 ? t('cart.item_one') : t('cart.item_other');
  
  return (
    <Card variant="elevated" padding="lg">
      <h2 className={styles.title}>
        {t('cart.itemsCount', { count: items.length, itemText })}
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
