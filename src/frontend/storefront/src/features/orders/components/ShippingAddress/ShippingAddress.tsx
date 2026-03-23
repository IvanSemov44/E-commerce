import type { Address } from '@/shared/types';

interface ShippingAddressProps {
  address: Address;
}

export function ShippingAddress({ address }: ShippingAddressProps) {
  return (
    <div className="bg-gray-50 p-4 rounded-lg">
      <h3 className="font-medium mb-2">Shipping Address</h3>
      <p>
        {address.firstName} {address.lastName}
      </p>
      <p>{address.streetLine1}</p>
      {address.streetLine2 && <p>{address.streetLine2}</p>}
      <p>
        {address.city}, {address.state} {address.postalCode}
      </p>
      <p>{address.country}</p>
      {address.phone && <p className="mt-2 text-sm text-gray-600">{address.phone}</p>}
    </div>
  );
}
