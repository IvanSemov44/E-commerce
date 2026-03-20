import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/components/ui/Card';
import { CartItem } from '../CartItem/CartItem';
import styles from './CartItemList.module.css';

export interface DisplayCartItem {
  id: string;
  name: string;
  slug: string;
  price: number;
  quantity: number;
  maxStock: number;
  image: string;
  compareAtPrice?: number;
}

export interface CartItemListProps {
  items: DisplayCartItem[];
}

export function CartItemList({ items }: CartItemListProps) {
  const { t } = useTranslation();
  const itemText = items.length === 1 ? t('cart.item_one') : t('cart.item_other');

  return (
    <Card variant="elevated" padding="lg">
      <h2 className={styles.title}>{t('cart.itemsCount', { count: items.length, itemText })}</h2>
      <div className={styles.itemsList}>
        {items.map((item) => (
          <CartItem key={item.id} item={item} />
        ))}
      </div>
    </Card>
  );
}
