import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/components/ui/Button';
import OrderTotalsDisplay from '@/shared/components/OrderTotalsDisplay/OrderTotalsDisplay';
import type { OrderSummaryProps } from './OrderSummary.types';

export function OrderSummary({ cartItems, totals, promoCode }: OrderSummaryProps) {
  const { t } = useTranslation();
  const { subtotal, discount, shipping, tax, total } = totals;
  const { code, validation, isValidating, onChange, onApply, onRemove } = promoCode;

  return (
    <div className="bg-gray-50 p-6 rounded-lg">
      <h2 className="text-lg font-semibold mb-4">{t('checkout.orderSummary')}</h2>

      {/* Items */}
      <div className="space-y-4 mb-4">
        {cartItems.map((item) => (
          <div key={item.id} className="flex gap-4">
            {item.image && (
              <img src={item.image} alt={item.name} className="w-16 h-16 object-cover rounded" />
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
            value={code}
            onChange={(e) => onChange(e.target.value)}
            placeholder={t('checkout.enterPromoCode')}
            className="flex-1 px-3 py-2 border rounded-lg text-sm"
          />
          {code ? (
            <Button variant="outline" size="sm" onClick={onRemove}>
              {t('checkout.remove')}
            </Button>
          ) : (
            <Button size="sm" onClick={onApply} disabled={isValidating || !code}>
              {isValidating ? t('checkout.applying') : t('checkout.apply')}
            </Button>
          )}
        </div>
        {validation && !validation.isValid && (
          <p className="text-red-500 text-sm mt-1">{validation.message}</p>
        )}
        {validation && validation.isValid && (
          <p className="text-green-600 text-sm mt-1">{validation.message}</p>
        )}
      </div>

      {/* Totals */}
      <OrderTotalsDisplay
        subtotal={subtotal}
        discount={discount}
        shipping={shipping}
        tax={tax}
        total={total}
      />
    </div>
  );
}
