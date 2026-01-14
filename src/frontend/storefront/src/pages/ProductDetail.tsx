import { useParams } from 'react-router-dom';
import { useGetProductBySlugQuery } from '../store/api/productApi';

export default function ProductDetail() {
  const { slug = '' } = useParams();
  const { data: product, isLoading } = useGetProductBySlugQuery(slug);

  if (isLoading) return <div className="text-center py-8">Loading...</div>;
  if (!product) return <div className="text-center py-8">Product not found</div>;

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-6xl mx-auto px-4">
        <div className="bg-white rounded-lg shadow-lg p-8">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            {/* Images */}
            <div>
              <div className="aspect-square bg-gray-200 rounded-lg flex items-center justify-center mb-4">
                <img
                  src={product.images[0]?.url}
                  alt={product.name}
                  className="w-full h-full object-cover rounded-lg"
                  onError={(e) => { e.currentTarget.src = 'https://via.placeholder.com/500' }}
                />
              </div>
              <div className="grid grid-cols-4 gap-2">
                {product.images.slice(1).map((img) => (
                  <img
                    key={img.id}
                    src={img.url}
                    alt={img.altText || 'Product'}
                    className="w-full h-20 object-cover rounded cursor-pointer"
                    onError={(e) => { e.currentTarget.src = 'https://via.placeholder.com/100' }}
                  />
                ))}
              </div>
            </div>

            {/* Details */}
            <div>
              <h1 className="text-4xl font-bold mb-2">{product.name}</h1>
              <div className="flex items-center mb-4">
                <span className="text-yellow-500 text-xl">★ {product.averageRating}</span>
                <span className="text-gray-600 ml-2">({product.reviewCount} reviews)</span>
              </div>

              <div className="mb-6">
                <span className="text-3xl font-bold text-blue-600">${product.price.toFixed(2)}</span>
                {product.compareAtPrice && (
                  <span className="text-lg text-gray-500 line-through ml-4">${product.compareAtPrice.toFixed(2)}</span>
                )}
              </div>

              <p className="text-gray-700 mb-6">{product.description}</p>

              <div className="mb-6">
                <p className="text-sm text-gray-600">Stock: {product.stockQuantity} available</p>
                {product.stockQuantity <= product.lowStockThreshold && (
                  <p className="text-red-500 text-sm">Only {product.stockQuantity} left!</p>
                )}
              </div>

              <button className="w-full bg-blue-600 text-white py-3 rounded-lg font-bold hover:bg-blue-700 mb-4">
                Add to Cart
              </button>
              <button className="w-full border-2 border-gray-300 py-3 rounded-lg font-bold hover:border-gray-400">
                Add to Wishlist
              </button>
            </div>
          </div>

          {/* Reviews */}
          <div className="mt-12 pt-8 border-t">
            <h2 className="text-2xl font-bold mb-6">Reviews</h2>
            {product.reviews.length === 0 ? (
              <p className="text-gray-600">No reviews yet</p>
            ) : (
              <div className="space-y-4">
                {product.reviews.map((review) => (
                  <div key={review.id} className="border-b pb-4">
                    <div className="flex justify-between items-start mb-2">
                      <span className="font-bold">{review.userName}</span>
                      <span className="text-yellow-500">★ {review.rating}</span>
                    </div>
                    {review.title && <h4 className="font-semibold mb-1">{review.title}</h4>}
                    <p className="text-gray-700 mb-2">{review.comment}</p>
                    <span className="text-sm text-gray-500">{new Date(review.createdAt).toLocaleDateString()}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
