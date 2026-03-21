import React from 'react';
import { Link } from 'react-router';
import { DEFAULT_PRODUCT_IMAGE } from '@/shared/lib/utils/constants';
import { formatPrice } from '@/shared/lib/utils/priceFormatter';
import { useCartOperations } from '@/features/cart/hooks';
import { useToast } from '@/shared/hooks';
import { QuantityControl } from '@/shared/components/ui';
import type { CartItem as CartItemType } from '@/features/cart/types';
import styles from './CartItem.module.css';

interface CartItemProps {
  item: CartItemType;
  readOnly?: boolean;
}

export const CartItem = React.memo(function CartItem({ item, readOnly = false }: CartItemProps) {
  const { update, remove } = useCartOperations();
  const { success, error: showError } = useToast();

  const imageSrc = item.image || DEFAULT_PRODUCT_IMAGE;

  async function handleUpdateQuantity(quantity: number) {
    try {
      await update(item.id, quantity);
      success('Cart updated');
    } catch {
      showError('Failed to update cart');
    }
  }

  async function handleRemove() {
    try {
      await remove(item.id);
      success('Item removed from cart');
    } catch {
      showError('Failed to remove item');
    }
  }

  return (
    <div className={styles.container}>
      {/* Image */}
      <Link to={`/products/${item.slug}`} className={styles.imageLink}>
        <img
          src={imageSrc}
          alt={item.name}
          className={styles.image}
          onError={(e) => {
            e.currentTarget.src = DEFAULT_PRODUCT_IMAGE;
          }}
        />
      </Link>

      {/* Details */}
      <div className={styles.details}>
        <Link to={`/products/${item.slug}`} className={styles.nameLink}>
          {item.name}
        </Link>
        <div className={styles.price}>
          {formatPrice(item.price)}
          {item.compareAtPrice && (
            <span className={styles.strikethrough}>{formatPrice(item.compareAtPrice)}</span>
          )}
        </div>

        {!readOnly && (
          <QuantityControl
            value={item.quantity}
            max={item.maxStock}
            onChange={(qty) => void handleUpdateQuantity(qty)}
          />
        )}

        {readOnly && <div className={styles.readOnlyQuantity}>Quantity: {item.quantity}</div>}
      </div>

      {/* Subtotal & Remove */}
      <div className={styles.rightSection}>
        <div className={styles.subtotal}>{formatPrice(item.price * item.quantity)}</div>
        {!readOnly && (
          <button onClick={() => void handleRemove()} className={styles.removeButton}>
            Remove
          </button>
        )}
      </div>
    </div>
  );
});
