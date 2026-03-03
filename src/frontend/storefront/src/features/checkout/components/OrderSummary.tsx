// OrderSummary component - migrated from pages/components/Checkout/OrderSummary.tsx

import { useTranslation } from 'react-i18next';
import Button from '../../../shared/components/ui/Button';
import type { CartItem } from '../../cart/slices/cartSlice';

interface PromoCodeValidation {
  isValid: boolean;
  discountAmount: number;
  message?: string;
}

interface OrderSummaryProps {
  cartItems: CartItem[];
  subtotal: number;
  discount: number;
  shipping: number;
  tax: number;
  total: number;
  promoCode: string;
  onPromoCodeChange: (code: string) => void;
  promoCodeValidation: PromoCodeValidation | null;
  validatingPromoCode: boolean;
  onApplyPromoCode: () => Promise<void>;
  onRemovePromoCode: () => void;
}

export default function OrderSummary({
  cartItems,
  subtotal,
  discount,
  shipping,
  tax,
  total,
  promoCode,
  onPromoCodeChange,
  promoCodeValidation,
  validatingPromoCode,
  onApplyPromoCode,
  onRemovePromoCode,
}: OrderSummaryProps) {
  const { t } = useTranslation();

  return (
    <div className="bg-gray-50 p-6 rounded-lg">
      <h2 className="text-lg font-semibold mb-4">{t('checkout.orderSummary')}</h2>
      
      {/* Items */}
      <div className="space-y-4 mb-4">
        {cartItems.map((item) => (
          <div key={item.id} className="flex gap-4">
            {item.image && (
              <img
                src={item.image}
                alt={item.name}
                className="w-16 h-16 object-cover rounded"
              />
            )}
            <div className="flex-1">
              <p className="font-medium">{item.name}</p>
              <p className="text-sm text-gray-500">
                {item.quantity} x ${item.price.toFixed(2)}
              </p>
            </div>
            <p className="font-medium">${(item.price * item.quantity).toFixed(2)}</p>
          </div>
        ))}
      </div>

      {/* Promo Code */}
      <div className="mb-4 pb-4 border-b">
        <label htmlFor="promoCode" className="block text-sm font-medium mb-1">
          {t('checkout.promoCode')}
        </label>
        <div className="flex gap-2">
          <input
            type="text"
            id="promoCode"
            value={promoCode}
            onChange={(e) => onPromoCodeChange(e.target.value)}
            placeholder={t('checkout.enterPromoCode')}
            className="flex-1 px-3 py-2 border rounded-lg text-sm"
          />
          {promoCode ? (
            <Button
              variant="outline"
              size="sm"
              onClick={onRemovePromoCode}
            >
              {t('checkout.remove')}
            </Button>
          ) : (
            <Button
              size="sm"
              onClick={onApplyPromoCode}
              disabled={validatingPromoCode || !promoCode}
            >
              {validatingPromoCode ? t('checkout.applying') : t('checkout.apply')}
            </Button>
          )}
        </div>
        {promoCodeValidation && !promoCodeValidation.isValid && (
          <p className="text-red-500 text-sm mt-1">{promoCodeValidation.message}</p>
        )}
        {promoCodeValidation && promoCodeValidation.isValid && (
          <p className="text-green-600 text-sm mt-1">{promoCodeValidation.message}</p>
        )}
      </div>

      {/* Totals */}
      <div className="border-t pt-4 space-y-2">
        <div className="flex justify-between">
          <span>{t('checkout.subtotal')}</span>
          <span>${subtotal.toFixed(2)}</span>
        </div>
        {discount > 0 && (
          <div className="flex justify-between text-green-600">
            <span>{t('checkout.discount')}</span>
            <span>-${discount.toFixed(2)}</span>
          </div>
        )}
        <div className="flex justify-between">
          <span>{t('checkout.shipping')}</span>
          <span>{shipping === 0 ? t('checkout.free') : `$${shipping.toFixed(2)}`}</span>
        </div>
        <div className="flex justify-between">
          <span>{t('checkout.tax')}</span>
          <span>${tax.toFixed(2)}</span>
        </div>
        <div className="flex justify-between font-semibold text-lg pt-2 border-t">
          <span>{t('checkout.total')}</span>
          <span>${total.toFixed(2)}</span>
        </div>
      </div>
    </div>
  );
}
