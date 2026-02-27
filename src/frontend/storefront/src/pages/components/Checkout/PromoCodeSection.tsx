import { useTranslation } from 'react-i18next';
import Input from '../../../components/ui/Input';
import Button from '../../../components/ui/Button';
import styles from './PromoCodeSection.module.css';

interface PromoCodeValidation {
  isValid: boolean;
  discountAmount: number;
  message?: string;
}

interface PromoCodeSectionProps {
  promoCode: string;
  onPromoCodeChange: (code: string) => void;
  promoCodeValidation: PromoCodeValidation | null;
  validatingPromoCode: boolean;
  onApply: () => void;
  onRemove: () => void;
}

export default function PromoCodeSection({
  promoCode,
  onPromoCodeChange,
  promoCodeValidation,
  validatingPromoCode,
  onApply,
  onRemove,
}: PromoCodeSectionProps) {
  const { t } = useTranslation();
  
  return (
    <div className={styles.promoSection}>
      {!promoCodeValidation?.isValid ? (
        <div className={styles.promoInput}>
          <Input
            placeholder={t('cart.enterPromoCode')}
            value={promoCode}
            onChange={(e) => onPromoCodeChange(e.target.value.toUpperCase())}
            className={styles.promoInputField}
          />
          <Button
            onClick={onApply}
            disabled={validatingPromoCode || !promoCode.trim()}
            variant="secondary"
            size="sm"
          >
            {validatingPromoCode ? t('cart.validating') : t('cart.applyCode')}
          </Button>
        </div>
      ) : null}

      {promoCodeValidation && (
        <div
          className={`${styles.promoMessage} ${
            promoCodeValidation.isValid ? styles.promoSuccess : styles.promoError
          }`}
        >
          {promoCodeValidation.message}
          {promoCodeValidation.isValid && (
            <button onClick={onRemove} className={styles.promoRemove} type="button">
              {t('common.remove')}
            </button>
          )}
        </div>
      )}
    </div>
  );
}
