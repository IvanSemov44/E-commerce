import { Link } from 'react-router-dom';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { selectCartItems, selectCartSubtotal, updateQuantity, removeItem } from '../store/slices/cartSlice';
import Button from '../components/ui/Button';
import Card from '../components/ui/Card';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import CartItem from '@/components/CartItem';

export default function Cart() {
  const dispatch = useAppDispatch();
  const cartItems = useAppSelector(selectCartItems);
  const subtotal = useAppSelector(selectCartSubtotal);

  // Calculate additional charges
  const shipping = subtotal > 100 ? 0 : subtotal > 0 ? 10 : 0;
  const tax = subtotal * 0.08;
  const total = subtotal + shipping + tax;

  const handleUpdateQuantity = (id: string, quantity: number) => {
    if (quantity <= 0) {
      dispatch(removeItem(id));
    } else {
      dispatch(updateQuantity({ id, quantity }));
    }
  };

  const handleRemove = (id: string) => {
    dispatch(removeItem(id));
  };

  return (
    <div className="min-h-screen bg-slate-50 py-8">
      <div className="max-w-6xl mx-auto px-4">
        <PageHeader title="Shopping Cart" />

        {cartItems.length === 0 ? (
          <EmptyState
            icon={
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z"
                />
              </svg>
            }
            title="Your cart is empty"
            action={
              <Link to="/products">
                <Button size="lg">Continue Shopping</Button>
              </Link>
            }
          />
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Cart Items */}
            <div className="lg:col-span-2">
              <Card variant="elevated" padding="lg">
                <h2 className="text-2xl font-bold mb-6 text-slate-900">
                  Items ({cartItems.length} {cartItems.length === 1 ? 'product' : 'products'})
                </h2>
                <div className="space-y-4">
                  {cartItems.map((item) => (
                    <CartItem
                      key={item.id}
                      item={item}
                      onUpdateQuantity={handleUpdateQuantity}
                      onRemove={handleRemove}
                    />
                  ))}
                </div>
              </Card>
            </div>

            {/* Order Summary */}
            <div className="h-fit sticky top-4">
              <Card variant="elevated" padding="lg">
                <h2 className="text-2xl font-bold mb-6 text-slate-900">Order Summary</h2>
                <div className="space-y-3 mb-6 pb-6 border-b border-slate-200">
                  <div className="flex justify-between text-slate-600">
                    <span>Subtotal:</span>
                    <span className="font-semibold text-slate-900">${subtotal.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-slate-600">
                    <span>Shipping:</span>
                    <span className="font-semibold text-slate-900">
                      {shipping === 0 ? 'FREE' : `$${shipping.toFixed(2)}`}
                    </span>
                  </div>
                  {subtotal > 50 && subtotal < 100 && (
                    <div className="text-sm text-emerald-600 font-medium">
                      Add ${(100 - subtotal).toFixed(2)} more for free shipping!
                    </div>
                  )}
                  <div className="flex justify-between text-slate-600">
                    <span>Tax (8%):</span>
                    <span className="font-semibold text-slate-900">${tax.toFixed(2)}</span>
                  </div>
                </div>
                <div className="flex justify-between text-lg font-bold mb-6 text-slate-900">
                  <span>Total:</span>
                  <span className="text-emerald-600">${total.toFixed(2)}</span>
                </div>
                <Link to="/checkout" className="block mb-3">
                  <Button size="lg" className="w-full">
                    Proceed to Checkout
                  </Button>
                </Link>
                <Link to="/products" className="block">
                  <Button variant="secondary" size="lg" className="w-full">
                    Continue Shopping
                  </Button>
                </Link>
              </Card>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
