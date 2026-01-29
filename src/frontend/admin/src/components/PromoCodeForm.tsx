import React, { useState } from 'react';
import Button from './ui/Button';
import Input from './ui/Input';
import styles from './ProductForm.module.css';
import type { PromoCodeDetail } from '@shared/types';

interface PromoCodeFormProps {
  promoCode?: PromoCodeDetail;
  onSubmit: (data: any) => Promise<void>;
  onCancel: () => void;
}

export default function PromoCodeForm({ promoCode, onSubmit, onCancel }: PromoCodeFormProps) {
  const [formData, setFormData] = useState({
    code: promoCode?.code || '',
    discountType: promoCode?.discountType || 'percentage',
    discountValue: promoCode?.discountValue?.toString() || '',
    minOrderAmount: promoCode?.minOrderAmount?.toString() || '',
    maxDiscountAmount: promoCode?.maxDiscountAmount?.toString() || '',
    maxUses: promoCode?.maxUses?.toString() || '',
    startDate: promoCode?.startDate ? promoCode.startDate.slice(0, 16) : '',
    endDate: promoCode?.endDate ? promoCode.endDate.slice(0, 16) : '',
    isActive: promoCode?.isActive ?? true,
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value, type } = e.target;
    let finalValue: any = type === 'checkbox' ? (e.target as HTMLInputElement).checked : value;

    // Auto-uppercase code input
    if (name === 'code') {
      finalValue = value.toUpperCase();
    }

    setFormData((prev) => ({
      ...prev,
      [name]: finalValue,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsSubmitting(true);

    try {
      const data: any = {
        code: formData.code.trim(),
        discountType: formData.discountType,
        discountValue: parseFloat(formData.discountValue),
        isActive: formData.isActive,
      };

      if (formData.minOrderAmount) {
        data.minOrderAmount = parseFloat(formData.minOrderAmount);
      }

      if (formData.maxDiscountAmount) {
        data.maxDiscountAmount = parseFloat(formData.maxDiscountAmount);
      }

      if (formData.maxUses) {
        data.maxUses = parseInt(formData.maxUses, 10);
      }

      if (formData.startDate) {
        data.startDate = new Date(formData.startDate).toISOString();
      }

      if (formData.endDate) {
        data.endDate = new Date(formData.endDate).toISOString();
      }

      await onSubmit(data);
    } catch (err: any) {
      setError(err.message || 'Failed to save promo code');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className={styles.form}>
      {error && <div className={styles.error}>{error}</div>}

      <div className={styles.row}>
        <Input
          label="Promo Code"
          name="code"
          value={formData.code}
          onChange={handleChange}
          required
          placeholder="SAVE20"
          helperText="Promo code will be automatically uppercased"
        />
      </div>

      <div className={styles.row} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
        <div>
          <label htmlFor="discountType" className={styles.label}>
            Discount Type *
          </label>
          <select
            id="discountType"
            name="discountType"
            value={formData.discountType}
            onChange={handleChange}
            required
            className={styles.select}
          >
            <option value="percentage">Percentage (%)</option>
            <option value="fixed">Fixed Amount ($)</option>
          </select>
        </div>

        <Input
          label={`Discount Value (${formData.discountType === 'percentage' ? '%' : '$'})`}
          name="discountValue"
          type="number"
          step={formData.discountType === 'percentage' ? '1' : '0.01'}
          min="0"
          max={formData.discountType === 'percentage' ? '100' : undefined}
          value={formData.discountValue}
          onChange={handleChange}
          required
          placeholder={formData.discountType === 'percentage' ? '20' : '10.00'}
        />
      </div>

      <div className={styles.row} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
        <Input
          label="Min Order Amount (optional)"
          name="minOrderAmount"
          type="number"
          step="0.01"
          min="0"
          value={formData.minOrderAmount}
          onChange={handleChange}
          placeholder="50.00"
          helperText="Minimum order amount to use this code"
        />

        <Input
          label="Max Discount Amount (optional)"
          name="maxDiscountAmount"
          type="number"
          step="0.01"
          min="0"
          value={formData.maxDiscountAmount}
          onChange={handleChange}
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
          value={formData.maxUses}
          onChange={handleChange}
          placeholder="100"
          helperText="Maximum number of times this code can be used"
        />
      </div>

      <div className={styles.row} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
        <Input
          label="Start Date (optional)"
          name="startDate"
          type="datetime-local"
          value={formData.startDate}
          onChange={handleChange}
          helperText="When this code becomes active"
        />

        <Input
          label="End Date (optional)"
          name="endDate"
          type="datetime-local"
          value={formData.endDate}
          onChange={handleChange}
          helperText="When this code expires"
        />
      </div>

      <div className={styles.row}>
        <label className={styles.checkbox}>
          <input
            type="checkbox"
            name="isActive"
            checked={formData.isActive}
            onChange={handleChange}
          />
          <span>Active</span>
        </label>
      </div>

      <div className={styles.actions}>
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : promoCode ? 'Update' : 'Create'}
        </Button>
      </div>
    </form>
  );
}
