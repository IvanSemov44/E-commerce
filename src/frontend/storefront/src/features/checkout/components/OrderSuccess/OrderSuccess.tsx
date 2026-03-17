import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button } from '@/shared/components/ui/Button';
import { CheckIcon } from '@/shared/components/icons';
import type { OrderSuccessProps } from './OrderSuccess.types';

export default function OrderSuccess({ orderNumber, email, isGuestOrder }: OrderSuccessProps) {
  const { t } = useTranslation();

  return (
    <div className="text-center py-8">
      <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
        <CheckIcon className="w-8 h-8 text-green-600" />
      </div>

      <h2 className="text-2xl font-bold mb-2">{t('checkout.orderSuccess')}</h2>
      <p className="text-gray-600 mb-4">{t('checkout.confirmationEmailSent', { email })}</p>

      <p className="text-sm text-gray-500 mb-6">
        {t('checkout.orderNumber')}: <span className="font-mono">{orderNumber}</span>
      </p>

      {/* Guest order account creation CTA */}
      {isGuestOrder && (
        <div className="bg-blue-50 rounded-lg p-6 mb-6 max-w-md mx-auto">
          <h3 className="text-lg font-semibold mb-2 text-blue-900">
            {t('checkout.createAccount')}
          </h3>
          <p className="text-sm text-blue-700 mb-4">{t('checkout.registerToTrack')}</p>
          <Link to={ROUTE_PATHS.register}>
            <Button variant="outline">{t('auth.signUp')}</Button>
          </Link>
        </div>
      )}

      <div className="flex gap-4 justify-center">
        <Link to={ROUTE_PATHS.orders}>
          <Button variant="outline">{t('orders.viewOrders')}</Button>
        </Link>
        <Link to={ROUTE_PATHS.products}>
          <Button>{t('checkout.continueShopping')}</Button>
        </Link>
      </div>
    </div>
  );
}
