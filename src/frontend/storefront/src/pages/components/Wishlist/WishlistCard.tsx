import { Link } from 'react-router-dom';
import Button from '../../../components/ui/Button';
import styles from './WishlistCard.module.css';

interface Product {
  id: string;
  slug: string;
  name: string;
  shortDescription?: string;
  price: number;
  compareAtPrice?: number;
  images: Array<{ url: string }>;
  stockQuantity?: number;
  isAvailable?: boolean;
}

interface WishlistCardProps {
  product: Product;
  onRemove: (productId: string) => void;
}

// Icons
const TrashIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
  </svg>
);

const CartIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />
  </svg>
);

export default function WishlistCard({ product, onRemove }: WishlistCardProps) {
  const hasDiscount = product.compareAtPrice && product.compareAtPrice > product.price;
  const discountPercent = hasDiscount 
    ? Math.round((1 - product.price / product.compareAtPrice!) * 100) 
    : 0;
  
  const isInStock = product.isAvailable !== false && (product.stockQuantity === undefined || product.stockQuantity > 0);
  const isLowStock = product.stockQuantity !== undefined && product.stockQuantity > 0 && product.stockQuantity <= 5;

  return (
    <div className={styles.card}>
      <Link to={`/products/${product.slug}`} className={styles.link}>
        <div className={styles.imageContainer}>
          {product.images[0]?.url && (
            <img
              src={product.images[0].url}
              alt={product.name}
              className={styles.image}
            />
          )}
        </div>
        <div className={styles.content}>
          <h3 className={styles.name}>{product.name}</h3>
          {product.shortDescription && (
            <p className={styles.description}>{product.shortDescription}</p>
          )}
          
          {/* Stock Status */}
          <div className={`${styles.stockStatus} ${isInStock ? (isLowStock ? styles.lowStock : styles.inStock) : styles.outOfStock}`}>
            <span className={styles.stockDot}></span>
            <span>{isInStock ? (isLowStock ? `Only ${product.stockQuantity} left` : 'In Stock') : 'Out of Stock'}</span>
          </div>
        </div>
      </Link>

      <div className={styles.footer}>
        <div className={styles.priceSection}>
          <div className={styles.priceRow}>
            <span className={styles.price}>${product.price.toFixed(2)}</span>
            {hasDiscount && (
              <span className={styles.comparePrice}>
                ${product.compareAtPrice!.toFixed(2)}
              </span>
            )}
          </div>
          {hasDiscount && discountPercent > 0 && (
            <span className={styles.discountBadge}>-{discountPercent}%</span>
          )}
        </div>

        <div className={styles.actions}>
          <button 
            className={styles.removeButton}
            onClick={() => onRemove(product.id)}
            title="Remove from wishlist"
            aria-label="Remove from wishlist"
          >
            <TrashIcon />
          </button>
          {isInStock && (
            <button className={styles.addToCartButton}>
              <CartIcon />
              Add
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
