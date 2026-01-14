import { Link } from 'react-router-dom';
import { useGetFeaturedProductsQuery } from '../store/api/productApi';

export default function Home() {
  const { data: featured, isLoading } = useGetFeaturedProductsQuery(10);

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Hero Section */}
      <section className="bg-blue-600 text-white py-20">
        <div className="max-w-6xl mx-auto px-4 text-center">
          <h1 className="text-5xl font-bold mb-4">Welcome to E-Commerce</h1>
          <p className="text-xl mb-8">Discover amazing products at great prices</p>
          <Link to="/products" className="bg-white text-blue-600 px-8 py-3 rounded font-bold hover:bg-gray-100">
            Shop Now
          </Link>
        </div>
      </section>

      {/* Featured Products */}
      <section className="py-16 max-w-6xl mx-auto px-4">
        <h2 className="text-3xl font-bold mb-8">Featured Products</h2>
        {isLoading ? (
          <div className="text-center">Loading products...</div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {featured?.map((product) => (
              <Link key={product.id} to={`/products/${product.slug}`} className="bg-white rounded-lg shadow hover:shadow-lg transition">
                <div className="aspect-square bg-gray-200 rounded-t-lg flex items-center justify-center">
                  <img
                    src={product.images[0]?.url}
                    alt={product.name}
                    className="w-full h-full object-cover"
                    onError={(e) => { e.currentTarget.src = 'https://via.placeholder.com/300' }}
                  />
                </div>
                <div className="p-4">
                  <h3 className="font-bold text-lg mb-2">{product.name}</h3>
                  <p className="text-gray-600 mb-4">${product.price.toFixed(2)}</p>
                  <div className="flex items-center justify-between">
                    <span className="text-yellow-500">★ {product.averageRating}</span>
                    <span className="text-gray-500 text-sm">({product.reviewCount})</span>
                  </div>
                </div>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
