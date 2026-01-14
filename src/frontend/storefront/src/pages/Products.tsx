import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useGetProductsQuery } from '../store/api/productApi';

export default function Products() {
  const [page, setPage] = useState(1);
  const { data: result, isLoading } = useGetProductsQuery({ page, pageSize: 20 });

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-6xl mx-auto px-4">
        <h1 className="text-3xl font-bold mb-8">All Products</h1>

        {isLoading ? (
          <div className="text-center">Loading products...</div>
        ) : (
          <>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
              {result?.items.map((product) => (
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

            {/* Pagination */}
            <div className="flex justify-center gap-2">
              <button
                onClick={() => setPage(Math.max(1, page - 1))}
                disabled={page === 1}
                className="px-4 py-2 border rounded disabled:opacity-50"
              >
                Previous
              </button>
              <span className="px-4 py-2">Page {page}</span>
              <button
                onClick={() => setPage(page + 1)}
                disabled={!result || result.items.length < 20}
                className="px-4 py-2 border rounded disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
