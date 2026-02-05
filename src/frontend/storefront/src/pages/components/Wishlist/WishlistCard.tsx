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
}

interface WishlistCardProps {
  product: Product;
  onRemove: (productId: string) => void;
}

export default function WishlistCard({ product, onRemove }: WishlistCardProps) {
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
        </div>
      </Link>

      <div className={styles.footer}>
        <div className={styles.priceSection}>
          <span className={styles.price}>${product.price.toFixed(2)}</span>
          {product.compareAtPrice && product.compareAtPrice > product.price && (
            <span className={styles.comparePrice}>
              ${product.compareAtPrice.toFixed(2)}
            </span>
          )}
        </div>

        <div className={styles.actions}>
          <Button
            size="sm"
            variant="secondary"
            onClick={() => onRemove(product.id)}
          >
            Remove
          </Button>
        </div>
      </div>
    </div>
  );
}
