import type { OrderTotalsProps } from './OrderTotals.types';

export default function OrderTotals({
  subtotal,
  discountAmount = 0,
  shippingAmount,
  taxAmount,
  totalAmount,
}: OrderTotalsProps) {
  return (
    <div className="bg-gray-50 p-6 rounded-lg space-y-2 mb-6">
      <div className="flex justify-between text-sm">
        <span className="text-gray-600">Subtotal:</span>
        <span className="font-medium">${subtotal.toFixed(2)}</span>
      </div>
      
      {discountAmount > 0 && (
        <div className="flex justify-between text-sm text-green-600">
          <span>Discount:</span>
          <span className="font-medium">-${discountAmount.toFixed(2)}</span>
        </div>
      )}
      
      <div className="flex justify-between text-sm">
        <span className="text-gray-600">Shipping:</span>
        <span className="font-medium">${shippingAmount.toFixed(2)}</span>
      </div>
      
      <div className="flex justify-between text-sm">
        <span className="text-gray-600">Tax:</span>
        <span className="font-medium">${taxAmount.toFixed(2)}</span>
      </div>
      
      <div className="border-t pt-2 flex justify-between">
        <span className="font-bold">Total:</span>
        <span className="font-bold text-lg">${totalAmount.toFixed(2)}</span>
      </div>
    </div>
  );
}
