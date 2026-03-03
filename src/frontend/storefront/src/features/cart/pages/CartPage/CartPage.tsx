import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useCart } from '../../hooks/useCart';
import Button from '../../../../shared/components/ui/Button';
import PageHeader from '../../../../shared/components/PageHeader';
import EmptyState from '../../../../shared/components/EmptyState';
import { CartSkeleton } from '../../../../shared/components/Skeletons';
import { CartItemList, CartSummary } from '../../components';
import { FREE_SHIPPING_THRESHOLD } from '@/shared/lib/utils/constants';
import styles from './CartPage.module.css';

export default function CartPage() {
  const { t } = useTranslation();
  const { displayItems, totals, isLoading, handleUpdateQuantity, handleRemove } = useCart();

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <PageHeader title={t('cart.title')} />

        {isLoading ? (
          <CartSkeleton />
        ) : displayItems.length === 0 && !isLoading ? (
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
            title={t('cart.emptyCart')}
            action={
              <Link to="/products">
                <Button size="lg">{t('cart.continueShopping')}</Button>
              </Link>
            }
          />
        ) : (
          <div className={styles.grid}>
            <CartItemList
              items={displayItems}
              onUpdateQuantity={handleUpdateQuantity}
              onRemove={handleRemove}
            />
            <CartSummary
              subtotal={totals.subtotal}
              shipping={totals.shipping}
              tax={totals.tax}
              total={totals.total}
              freeShippingThreshold={FREE_SHIPPING_THRESHOLD}
            />
          </div>
        )}
      </div>
    </div>
  );
}
