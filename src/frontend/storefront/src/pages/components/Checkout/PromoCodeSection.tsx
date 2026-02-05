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
  return (
    <div className={styles.promoSection}>
      {!promoCodeValidation?.isValid ? (
        <div className={styles.promoInput}>
          <Input
            placeholder="Enter promo code"
            value={promoCode}
            onChange={(e) => onPromoCodeChange(e.target.value.toUpperCase())}
            style={{ flex: 1 }}
          />
          <Button
            onClick={onApply}
            disabled={validatingPromoCode || !promoCode.trim()}
            variant="secondary"
            size="sm"
          >
            {validatingPromoCode ? 'Validating...' : 'Apply'}
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
              Remove
            </button>
          )}
        </div>
      )}
    </div>
  );
}
