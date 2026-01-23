import React, { useState } from 'react';
import Button from './ui/Button';
import Input from './ui/Input';
import styles from './ProductForm.module.css';
import type { ProductDetail } from '@shared/types';

interface ProductFormProps {
  product?: ProductDetail;
  categories: Array<{ id: string; name: string }>;
  onSubmit: (data: any) => Promise<void>;
  onCancel: () => void;
}

export default function ProductForm({ product, categories, onSubmit, onCancel }: ProductFormProps) {
  const [formData, setFormData] = useState({
    name: product?.name || '',
    slug: product?.slug || '',
    description: product?.description || '',
    shortDescription: product?.shortDescription || '',
    price: product?.price?.toString() || '',
    compareAtPrice: product?.compareAtPrice?.toString() || '',
    stockQuantity: product?.stockQuantity?.toString() || '',
    categoryId: product?.category?.id || '',
    isFeatured: product?.isFeatured || false,
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
  ) => {
    const { name, value, type } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? (e.target as HTMLInputElement).checked : value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsSubmitting(true);

    try {
      await onSubmit({
        ...formData,
        price: parseFloat(formData.price),
        compareAtPrice: formData.compareAtPrice ? parseFloat(formData.compareAtPrice) : undefined,
        stockQuantity: parseInt(formData.stockQuantity, 10),
      });
    } catch (err: any) {
      setError(err.message || 'Failed to save product');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className={styles.form}>
      {error && <div className={styles.error}>{error}</div>}

      <div className={styles.row}>
        <Input
          label="Product Name"
          name="name"
          value={formData.name}
          onChange={handleChange}
          required
        />
      </div>

      <div className={styles.row}>
        <Input
          label="Slug"
          name="slug"
          value={formData.slug}
          onChange={handleChange}
          required
          helperText="URL-friendly version (e.g., laptop-stand)"
        />
      </div>

      <div className={styles.row}>
        <div className={styles.field}>
          <label htmlFor="description">Description</label>
          <textarea
            id="description"
            name="description"
            value={formData.description}
            onChange={handleChange}
            rows={4}
            className={styles.textarea}
          />
        </div>
      </div>

      <div className={styles.row}>
        <Input
          label="Short Description"
          name="shortDescription"
          value={formData.shortDescription}
          onChange={handleChange}
        />
      </div>

      <div className={styles.row}>
        <Input
          label="Price"
          name="price"
          type="number"
          step="0.01"
          value={formData.price}
          onChange={handleChange}
          required
        />
        <Input
          label="Compare At Price"
          name="compareAtPrice"
          type="number"
          step="0.01"
          value={formData.compareAtPrice}
          onChange={handleChange}
        />
      </div>

      <div className={styles.row}>
        <Input
          label="Stock Quantity"
          name="stockQuantity"
          type="number"
          value={formData.stockQuantity}
          onChange={handleChange}
          required
        />
        <div className={styles.field}>
          <label htmlFor="categoryId">Category</label>
          <select
            id="categoryId"
            name="categoryId"
            value={formData.categoryId}
            onChange={handleChange}
            required
            className={styles.select}
          >
            <option value="">Select a category</option>
            {categories.map((cat) => (
              <option key={cat.id} value={cat.id}>
                {cat.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className={styles.row}>
        <label className={styles.checkbox}>
          <input
            type="checkbox"
            name="isFeatured"
            checked={formData.isFeatured}
            onChange={handleChange}
          />
          <span>Featured Product</span>
        </label>
      </div>

      <div className={styles.actions}>
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : product ? 'Update Product' : 'Create Product'}
        </Button>
      </div>
    </form>
  );
}
