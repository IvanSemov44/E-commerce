import { Link } from 'react-router-dom';
import { useAppSelector } from '../store/hooks';
import { useAddToWishlistMutation, useRemoveFromWishlistMutation, useCheckInWishlistQuery } from '../store/api/wishlistApi';
import Card from './ui/Card';

interface ProductCardProps {
  id: string;
  name: string;
  slug: string;
  price: number;
  compareAtPrice?: number;
  imageUrl: string;
  rating?: number;
  reviewCount?: number;
}

export default function ProductCard({
  id,
  name,
  slug,
  price,
  compareAtPrice,
  imageUrl,
  rating = 0,
  reviewCount = 0
}: ProductCardProps) {
  const DEFAULT_PRODUCT_IMAGE = 'https://placehold.co/400x400/f1f5f9/64748b?text=Product';
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const { data: isInWishlist, refetch: refetchWishlist } = useCheckInWishlistQuery(id, {
    skip: !isAuthenticated,
  });
  const [addToWishlist] = useAddToWishlistMutation();
  const [removeFromWishlist] = useRemoveFromWishlistMutation();

  const handleWishlistClick = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();

    if (!isAuthenticated) {
      alert('Please sign in to add items to your wishlist');
      return;
    }

    try {
      if (isInWishlist) {
        await removeFromWishlist(id).unwrap();
      } else {
        await addToWishlist(id).unwrap();
      }
      refetchWishlist();
    } catch {
      // Error handled by mutation state
    }
  };

  return (
    <Link to={`/products/${slug}`}>
      <Card
        variant="default"
        padding="sm"
        style={{ position: 'relative' }}
      >
        <div style={{ position: 'relative' }}>
          <img
            src={imageUrl || DEFAULT_PRODUCT_IMAGE}
            alt={name}
            onError={(e) => {
              e.currentTarget.src = DEFAULT_PRODUCT_IMAGE;
            }}
          />
          {isAuthenticated && (
            <button
              onClick={handleWishlistClick}
              style={{
                position: 'absolute',
                top: '0.5rem',
                right: '0.5rem',
                background: 'white',
                border: 'none',
                borderRadius: '50%',
                width: '2rem',
                height: '2rem',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                cursor: 'pointer',
                fontSize: '1.25rem',
                boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
              }}
              title={isInWishlist ? 'Remove from wishlist' : 'Add to wishlist'}
            >
              {isInWishlist ? '♥' : '♡'}
            </button>
          )}
        </div>
        <div>
          <h3>
            {name}
          </h3>
          <div>
            <p className="price-primary">${price.toFixed(2)}</p>
            {compareAtPrice && compareAtPrice > price && (
              <p className="price-compare">${compareAtPrice.toFixed(2)}</p>
            )}
          </div>
          {rating > 0 && (
            <div>
              <div>
                {[...Array(5)].map((_, i) => (
                  <svg
                    key={i}
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                ))}
              </div>
              <span>({reviewCount})</span>
            </div>
          )}
        </div>
      </Card>
    </Link>
  );
}
