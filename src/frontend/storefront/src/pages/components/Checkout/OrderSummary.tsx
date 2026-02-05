import CartItem from '../../../components/CartItem';
import type { CartItem as CartItemType } from '../../../store/slices/cartSlice';
import PromoCodeSection from './PromoCodeSection';
import styles from './OrderSummary.module.css';

interface PromoCodeValidation {
  isValid: boolean;
  discountAmount: number;
  message?: string;
}

interface OrderSummaryProps {
  cartItems: CartItemType[];
  subtotal: number;
  discount: number;
  shipping: number;
  tax: number;
  total: number;
  promoCode: string;
  onPromoCodeChange: (code: string) => void;
  promoCodeValidation: PromoCodeValidation | null;
  validatingPromoCode: boolean;
  onApplyPromoCode: () => void;
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
  return (
    <>
      <h2 className={styles.summaryTitle}>Order Summary</h2>

      {/* Items */}
      <div className={styles.itemsList}>
        {cartItems.map((item) => (
          <CartItem
            key={item.id}
            item={item}
            onUpdateQuantity={() => {}}
            onRemove={() => {}}
            readOnly={true}
          />
        ))}
      </div>

      {/* Promo Code */}
      <PromoCodeSection
        promoCode={promoCode}
        onPromoCodeChange={onPromoCodeChange}
        promoCodeValidation={promoCodeValidation}
        validatingPromoCode={validatingPromoCode}
        onApply={onApplyPromoCode}
        onRemove={onRemovePromoCode}
      />

      {/* Totals */}
      <div className={styles.totalsSection}>
        <div className={styles.totalLine}>
          <span>Subtotal:</span>
          <span className={styles.totalValue}>${subtotal.toFixed(2)}</span>
        </div>
        {discount > 0 && (
          <div className={styles.totalLine} style={{ color: '#16a34a' }}>
            <span>Discount ({promoCode}):</span>
            <span className={styles.totalValue}>-${discount.toFixed(2)}</span>
          </div>
        )}
        <div className={styles.totalLine}>
          <span>Shipping:</span>
          <span className={styles.totalValue}>
            {shipping === 0 ? 'FREE' : `$${shipping.toFixed(2)}`}
          </span>
        </div>
        <div className={styles.totalLine}>
          <span>Tax:</span>
          <span className={styles.totalValue}>${tax.toFixed(2)}</span>
        </div>
      </div>
      <div className={styles.grandTotal}>
        <span>Total:</span>
        <span className={styles.grandTotalAmount}>${total.toFixed(2)}</span>
      </div>
    </>
  );
}
