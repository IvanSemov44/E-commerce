import React, { useEffect } from 'react';
import Button from './ui/Button';
import Input from './ui/Input';
import useForm from '../hooks/useForm';
import { validators } from '../utils/validation';
import { getErrorMessage } from '../utils/formatters';
import styles from './PromoCodeForm.module.css';
import type { PromoCodeDetail, CreatePromoCodeRequest, UpdatePromoCodeRequest } from '@shared/types';

interface PromoCodeFormProps {
  promoCode?: PromoCodeDetail;
  onSubmit: (data: CreatePromoCodeRequest | (UpdatePromoCodeRequest & { id: string })) => Promise<void>;
  onCancel: () => void;
}

interface PromoCodeFormData {
  code: string;
  discountType: string;
  discountValue: string;
  minOrderAmount: string;
  maxDiscountAmount: string;
  maxUses: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

// Validation function for promo code form
const validatePromoCodeForm = (values: PromoCodeFormData): Partial<Record<keyof PromoCodeFormData, string>> => {
  const errors: Partial<Record<keyof PromoCodeFormData, string>> = {};

  const codeError = validators.required('Promo code')(values.code);
  if (codeError) errors.code = codeError;

  const discountTypeError = validators.required('Discount type')(values.discountType);
  if (discountTypeError) errors.discountType = discountTypeError;

  const discountRequiredError = validators.required('Discount value')(values.discountValue);
  if (discountRequiredError) {
    errors.discountValue = discountRequiredError;
  } else {
    const discountNumberError = validators.positiveNumber(values.discountValue);
    if (discountNumberError) {
      errors.discountValue = discountNumberError;
    } else if (values.discountType === 'percentage') {
      const discountVal = parseFloat(values.discountValue);
      if (discountVal > 100) {
        errors.discountValue = 'Percentage cannot exceed 100%';
      }
    }
  }

  if (values.minOrderAmount && values.minOrderAmount.trim()) {
    const minOrderError = validators.positiveNumber(values.minOrderAmount);
    if (minOrderError) errors.minOrderAmount = minOrderError;
  }

  if (values.maxDiscountAmount && values.maxDiscountAmount.trim()) {
    const maxDiscountError = validators.positiveNumber(values.maxDiscountAmount);
    if (maxDiscountError) errors.maxDiscountAmount = maxDiscountError;
  }

  if (values.maxUses && values.maxUses.trim()) {
    const maxUsesNumeric = validators.numeric(values.maxUses);
    if (maxUsesNumeric) errors.maxUses = 'Max uses must be a whole number';
  }

  return errors;
};

export default function PromoCodeForm({ promoCode, onSubmit, onCancel }: PromoCodeFormProps) {
  const [error, setError] = React.useState('');

  // Handle form submission (called by useForm after validation)
  const handleFormSubmit = async (values: PromoCodeFormData) => {
    setError('');

    try {
      const baseData: CreatePromoCodeRequest = {
        code: values.code.trim(),
        discountType: values.discountType as 'percentage' | 'fixed',
        discountValue: parseFloat(values.discountValue),
        isActive: values.isActive,
        ...(values.minOrderAmount && { minOrderAmount: parseFloat(values.minOrderAmount) }),
        ...(values.maxDiscountAmount && { maxDiscountAmount: parseFloat(values.maxDiscountAmount) }),
        ...(values.maxUses && { maxUses: parseInt(values.maxUses, 10) }),
        ...(values.startDate && { startDate: new Date(values.startDate).toISOString() }),
        ...(values.endDate && { endDate: new Date(values.endDate).toISOString() }),
      };

      await onSubmit(promoCode ? { ...baseData, id: promoCode.id } : baseData);
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'Failed to save promo code'));
    }
  };

  // Initialize useForm hook
  const form = useForm<PromoCodeFormData>({
    initialValues: {
      code: '',
      discountType: 'percentage',
      discountValue: '',
      minOrderAmount: '',
      maxDiscountAmount: '',
      maxUses: '',
      startDate: '',
      endDate: '',
      isActive: true,
    },
    validate: validatePromoCodeForm,
    onSubmit: handleFormSubmit,
  });

  // Sync promo code data to form when promoCode prop changes
  useEffect(() => {
    if (promoCode) {
      form.setValues({
        code: promoCode.code || '',
        discountType: promoCode.discountType || 'percentage',
        discountValue: promoCode.discountValue?.toString() || '',
        minOrderAmount: promoCode.minOrderAmount?.toString() || '',
        maxDiscountAmount: promoCode.maxDiscountAmount?.toString() || '',
        maxUses: promoCode.maxUses?.toString() || '',
        startDate: promoCode.startDate ? promoCode.startDate.slice(0, 16) : '',
        endDate: promoCode.endDate ? promoCode.endDate.slice(0, 16) : '',
        isActive: promoCode.isActive ?? true,
      });
    }
  }, [promoCode]);

  // Custom handler for code field to auto-uppercase
  const handleCodeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const uppercasedEvent = {
      ...e,
      target: {
        name: e.target.name,
        type: e.target.type,
        value: e.target.value.toUpperCase(),
      } as HTMLInputElement,
    };
    form.handleChange(uppercasedEvent as React.ChangeEvent<HTMLInputElement>);
  };

  return (
    <form onSubmit={form.handleSubmit} className={styles.form}>
      {error && <div className={styles.error}>{error}</div>}

      <div className={styles.row}>
        <Input
          label="Promo Code"
          name="code"
          value={form.values.code}
          onChange={handleCodeChange}
          error={form.errors.code}
          required
          placeholder="SAVE20"
          helperText="Promo code will be automatically uppercased"
        />
      </div>

      <div className={styles.formRow}>
        <div>
          <label htmlFor="discountType" className={styles.label}>
            Discount Type *
          </label>
          <select
            id="discountType"
            name="discountType"
            value={form.values.discountType}
            onChange={form.handleChange}
            required
            className={styles.select}
          >
            <option value="percentage">Percentage (%)</option>
            <option value="fixed">Fixed Amount ($)</option>
          </select>
          {form.errors.discountType && <div className={styles.fieldError}>{form.errors.discountType}</div>}
        </div>

        <Input
          label={`Discount Value (${form.values.discountType === 'percentage' ? '%' : '$'})`}
          name="discountValue"
          type="number"
          step={form.values.discountType === 'percentage' ? '1' : '0.01'}
          min="0"
          max={form.values.discountType === 'percentage' ? '100' : undefined}
          value={form.values.discountValue}
          onChange={form.handleChange}
          error={form.errors.discountValue}
          required
          placeholder={form.values.discountType === 'percentage' ? '20' : '10.00'}
        />
      </div>

      <div className={styles.formRow}>
        <Input
          label="Min Order Amount (optional)"
          name="minOrderAmount"
          type="number"
          step="0.01"
          min="0"
          value={form.values.minOrderAmount}
          onChange={form.handleChange}
          error={form.errors.minOrderAmount}
          placeholder="50.00"
          helperText="Minimum order amount to use this code"
        />

        <Input
          label="Max Discount Amount (optional)"
          name="maxDiscountAmount"
          type="number"
          step="0.01"
          min="0"
          value={form.values.maxDiscountAmount}
          onChange={form.handleChange}
          error={form.errors.maxDiscountAmount}
          placeholder="100.00"
          helperText="Cap the maximum discount"
        />
      </div>

      <div className={styles.row}>
        <Input
          label="Max Uses (optional)"
          name="maxUses"
          type="number"
          min="0"
          value={form.values.maxUses}
          onChange={form.handleChange}
          error={form.errors.maxUses}
          placeholder="100"
          helperText="Maximum number of times this code can be used"
        />
      </div>

      <div className={styles.formRow}>
        <Input
          label="Start Date (optional)"
          name="startDate"
          type="datetime-local"
          value={form.values.startDate}
          onChange={form.handleChange}
          helperText="When this code becomes active"
        />

        <Input
          label="End Date (optional)"
          name="endDate"
          type="datetime-local"
          value={form.values.endDate}
          onChange={form.handleChange}
          helperText="When this code expires"
        />
      </div>

      <div className={styles.row}>
        <label className={styles.checkbox}>
          <input
            type="checkbox"
            name="isActive"
            checked={form.values.isActive}
            onChange={form.handleChange}
          />
          <span>Active</span>
        </label>
      </div>

      <div className={styles.actions}>
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={form.isSubmitting}>
          {form.isSubmitting ? 'Saving...' : promoCode ? 'Update' : 'Create'}
        </Button>
      </div>
    </form>
  );
}
