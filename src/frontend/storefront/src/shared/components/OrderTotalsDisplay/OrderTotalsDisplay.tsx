import { formatPrice } from '@/shared/lib/utils/priceFormatter';

interface OrderTotalsDisplayProps {
  subtotal: number;
  shipping: number;
  tax: number;
  total: number;
  discount?: number;
  freeShippingLabel?: string;
  className?: string;
}

export default function OrderTotalsDisplay({
  subtotal,
  shipping,
  tax,
  total,
  discount = 0,
  freeShippingLabel = 'Free',
  className = '',
}: OrderTotalsDisplayProps) {
  return (
    <div className={`space-y-2 ${className}`}>
      <div className="flex justify-between text-sm">
        <span className="text-gray-600">Subtotal:</span>
        <span className="font-medium">{formatPrice(subtotal)}</span>
      </div>

      {discount > 0 && (
        <div className="flex justify-between text-sm text-green-600">
          <span>Discount:</span>
          <span className="font-medium">-{formatPrice(discount)}</span>
        </div>
      )}

      <div className="flex justify-between text-sm">
        <span className="text-gray-600">Shipping:</span>
        <span className="font-medium">
          {shipping === 0 ? freeShippingLabel : formatPrice(shipping)}
        </span>
      </div>

      <div className="flex justify-between text-sm">
        <span className="text-gray-600">Tax:</span>
        <span className="font-medium">{formatPrice(tax)}</span>
      </div>

      <div className="border-t pt-2 flex justify-between">
        <span className="font-bold">Total:</span>
        <span className="font-bold text-lg">{formatPrice(total)}</span>
      </div>
    </div>
  );
}
