import React from 'react';
import { Link } from 'react-router-dom';
import { DEFAULT_PRODUCT_IMAGE } from '../utils/constants';
import type { CartItem as CartItemType } from '../store/slices/cartSlice';
import styles from './CartItem.module.css';

interface CartItemProps {
  item: CartItemType;
  onUpdateQuantity: (id: string, quantity: number) => void;
  onRemove: (id: string) => void;
  readOnly?: boolean;
}

const CartItem = React.memo(function CartItem({ item, onUpdateQuantity, onRemove, readOnly = false }: CartItemProps) {
  // Use default image if item.image is empty or undefined
  const imageSrc = item.image || DEFAULT_PRODUCT_IMAGE;

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
          ${item.price.toFixed(2)}
          {item.compareAtPrice && (
            <span className={styles.strikethrough}>
              ${item.compareAtPrice.toFixed(2)}
            </span>
          )}
        </div>

        {!readOnly && (
          <div className={styles.quantityContainer}>
            <button
              onClick={() => onUpdateQuantity(item.id, item.quantity - 1)}
              className={styles.button}
            >
              −
            </button>
            <span className={styles.quantityDisplay}>{item.quantity}</span>
            <button
              onClick={() => onUpdateQuantity(item.id, item.quantity + 1)}
              disabled={item.quantity >= item.maxStock}
              className={styles.button}
            >
              +
            </button>
            {item.quantity >= item.maxStock && (
              <span className={styles.maxStockWarning}>Max stock reached</span>
            )}
          </div>
        )}

        {readOnly && <div className={styles.readOnlyQuantity}>Quantity: {item.quantity}</div>}
      </div>

      {/* Subtotal & Remove */}
      <div className={styles.rightSection}>
        <div className={styles.subtotal}>${(item.price * item.quantity).toFixed(2)}</div>
        {!readOnly && (
          <button
            onClick={() => onRemove(item.id)}
            className={styles.removeButton}
          >
            Remove
          </button>
        )}
      </div>
    </div>
  );
});

export default CartItem;

