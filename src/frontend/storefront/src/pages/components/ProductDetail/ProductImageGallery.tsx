import { useState } from 'react';
import { DEFAULT_PRODUCT_IMAGE } from '../../../utils/constants';
import type { ProductImage } from '../../../types';
import styles from './ProductImageGallery.module.css';

interface ProductImageGalleryProps {
  images: ProductImage[];
  productName: string;
}

export default function ProductImageGallery({ images, productName }: ProductImageGalleryProps) {
  const [selectedImageIndex, setSelectedImageIndex] = useState(0);

  return (
    <div className={styles.imageSection}>
      <div className={styles.mainImage}>
        <img
          src={images[selectedImageIndex]?.url}
          alt={productName}
          onError={(e) => { e.currentTarget.src = DEFAULT_PRODUCT_IMAGE }}
        />
      </div>
      
      {images.length > 1 && (
        <div className={styles.thumbnailGrid}>
          {images.map((img, index) => (
            <button
              key={img.id}
              onClick={() => setSelectedImageIndex(index)}
              className={`${styles.thumbnail} ${selectedImageIndex === index ? styles.thumbnailActive : ''}`}
              aria-label={`View product image ${index + 1}`}
              type="button"
            >
              <img
                src={img.url}
                alt={img.altText || `Product image ${index + 1}`}
                onError={(e) => { e.currentTarget.src = DEFAULT_PRODUCT_IMAGE }}
              />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
