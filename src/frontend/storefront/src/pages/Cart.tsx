import { Link } from 'react-router-dom';

export default function Cart() {
  // TODO: Implement cart functionality with Redux slice
  const cartItems = [];

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-6xl mx-auto px-4">
        <h1 className="text-3xl font-bold mb-8">Shopping Cart</h1>

        {cartItems.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-8 text-center">
            <p className="text-gray-600 mb-4">Your cart is empty</p>
            <Link to="/products" className="text-blue-600 hover:underline">
              Continue shopping
            </Link>
          </div>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Cart Items */}
            <div className="lg:col-span-2 bg-white rounded-lg shadow p-6">
              {/* Items will go here */}
            </div>

            {/* Summary */}
            <div className="bg-white rounded-lg shadow p-6 h-fit">
              <h2 className="text-xl font-bold mb-4">Order Summary</h2>
              <div className="space-y-2 mb-4 pb-4 border-b">
                <div className="flex justify-between">
                  <span>Subtotal:</span>
                  <span>$0.00</span>
                </div>
                <div className="flex justify-between">
                  <span>Shipping:</span>
                  <span>$0.00</span>
                </div>
                <div className="flex justify-between">
                  <span>Tax:</span>
                  <span>$0.00</span>
                </div>
              </div>
              <div className="flex justify-between text-lg font-bold mb-4">
                <span>Total:</span>
                <span>$0.00</span>
              </div>
              <Link to="/checkout" className="w-full bg-blue-600 text-white py-3 rounded font-bold hover:bg-blue-700 block text-center">
                Proceed to Checkout
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
