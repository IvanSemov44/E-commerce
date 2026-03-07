import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ShoppingCartIcon } from '@/shared/components/icons';
import { useCart } from '../../hooks/useCart';
import Button from '@/shared/components/ui/Button';
import PageHeader from '@/shared/components/PageHeader';
import QueryRenderer from '@/shared/components/QueryRenderer';
import { CartSkeleton } from '@/shared/components/Skeletons';
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

        <QueryRenderer
          isLoading={isLoading}
          error={null}
          data={displayItems.length > 0 ? displayItems : undefined}
          loadingSkeleton={{ custom: <CartSkeleton /> }}
          emptyState={{
            icon: <ShoppingCartIcon />,
            title: t('cart.emptyCart'),
            action: (
              <Link to="/products">
                <Button size="lg">{t('cart.continueShopping')}</Button>
              </Link>
            ),
          }}
        >
          {(items) => (
            <div className={styles.grid}>
              <CartItemList
                items={items}
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
        </QueryRenderer>
      </div>
    </div>
  );
}
