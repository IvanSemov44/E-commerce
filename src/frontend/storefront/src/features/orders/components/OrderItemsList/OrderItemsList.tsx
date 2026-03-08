import type { OrderItemsListProps } from './OrderItemsList.types';

export default function OrderItemsList({ items }: OrderItemsListProps) {
  return (
    <div>
      {items.map((item) => (
        <div key={item.id} className="flex py-4 border-b">
          {item.productImageUrl && (
            <img
              src={item.productImageUrl}
              alt={item.productName}
              className="w-16 h-16 object-cover rounded"
            />
          )}
          <div className="ml-4">
            <p className="font-medium">{item.productName}</p>
            <p className="text-sm text-gray-500">
              Qty: {item.quantity} x ${item.unitPrice.toFixed(2)}
            </p>
          </div>
        </div>
      ))}
    </div>
  );
}
