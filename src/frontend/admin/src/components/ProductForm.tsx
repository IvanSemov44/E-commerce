import React, { useEffect } from 'react';
import Button from './ui/Button';
import Input from './ui/Input';
import useForm from '../hooks/useForm';
import { validators } from '../utils/validation';
import styles from './ProductForm.module.css';
import type { ProductDetail, CreateProductRequest, UpdateProductRequest } from '@shared/types';

interface ProductFormProps {
  product?: ProductDetail;
  categories: Array<{ id: string; name: string }>;
  onSubmit: (data: CreateProductRequest | (UpdateProductRequest & { id: string })) => Promise<void>;
  onCancel: () => void;
}

interface ProductFormData {
  name: string;
  slug: string;
  description: string;
  shortDescription: string;
  price: string;
  compareAtPrice: string;
  stockQuantity: string;
  categoryId: string;
  isFeatured: boolean;
}

// Validation function for product form
const validateProductForm = (values: ProductFormData): Partial<Record<keyof ProductFormData, string>> => {
  const errors: Partial<Record<keyof ProductFormData, string>> = {};

  const nameError = validators.required('Product name')(values.name);
  if (nameError) errors.name = nameError;

  const slugError = validators.required('Slug')(values.slug);
  if (slugError) errors.slug = slugError;

  const priceRequiredError = validators.required('Price')(values.price);
  if (priceRequiredError) {
    errors.price = priceRequiredError;
  } else {
    const priceNumberError = validators.positiveNumber(values.price);
    if (priceNumberError) errors.price = priceNumberError;
  }

  if (values.compareAtPrice && values.compareAtPrice.trim()) {
    const compareAtPriceError = validators.positiveNumber(values.compareAtPrice);
    if (compareAtPriceError) errors.compareAtPrice = compareAtPriceError;
  }

  const stockRequiredError = validators.required('Stock quantity')(values.stockQuantity);
  if (stockRequiredError) {
    errors.stockQuantity = stockRequiredError;
  } else {
    const stockNumeric = validators.numeric(values.stockQuantity);
    if (stockNumeric) errors.stockQuantity = 'Stock quantity must be a whole number';
  }

  const categoryError = validators.required('Category')(values.categoryId);
  if (categoryError) errors.categoryId = categoryError;

  return errors;
};

export default function ProductForm({ product, categories, onSubmit, onCancel }: ProductFormProps) {
  const [error, setError] = React.useState('');

  // Handle form submission (called by useForm after validation)
  const handleFormSubmit = async (values: ProductFormData) => {
    setError('');

    try {
      await onSubmit({
        ...values,
        price: parseFloat(values.price),
        compareAtPrice: values.compareAtPrice ? parseFloat(values.compareAtPrice) : undefined,
        stockQuantity: parseInt(values.stockQuantity, 10),
      });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to save product';
      setError(message);
    }
  };

  // Initialize useForm hook
  const form = useForm<ProductFormData>({
    initialValues: {
      name: '',
      slug: '',
      description: '',
      shortDescription: '',
      price: '',
      compareAtPrice: '',
      stockQuantity: '',
      categoryId: '',
      isFeatured: false,
    },
    validate: validateProductForm,
    onSubmit: handleFormSubmit,
  });

  // Sync product data to form when product prop changes
  useEffect(() => {
    if (product) {
      form.setValues({
        name: product.name || '',
        slug: product.slug || '',
        description: product.description || '',
        shortDescription: product.shortDescription || '',
        price: product.price?.toString() || '',
        compareAtPrice: product.compareAtPrice?.toString() || '',
        stockQuantity: product.stockQuantity?.toString() || '',
        categoryId: product.category?.id || '',
        isFeatured: product.isFeatured || false,
      });
    }
  }, [product]);

  return (
    <form onSubmit={form.handleSubmit} className={styles.form}>
      {error && <div className={styles.error}>{error}</div>}

      <div className={styles.row}>
        <Input
          label="Product Name"
          name="name"
          value={form.values.name}
          onChange={form.handleChange}
          error={form.errors.name}
          required
        />
      </div>

      <div className={styles.row}>
        <Input
          label="Slug"
          name="slug"
          value={form.values.slug}
          onChange={form.handleChange}
          error={form.errors.slug}
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
            value={form.values.description}
            onChange={form.handleChange}
            rows={4}
            className={styles.textarea}
          />
        </div>
      </div>

      <div className={styles.row}>
        <Input
          label="Short Description"
          name="shortDescription"
          value={form.values.shortDescription}
          onChange={form.handleChange}
        />
      </div>

      <div className={styles.row}>
        <Input
          label="Price"
          name="price"
          type="number"
          step="0.01"
          value={form.values.price}
          onChange={form.handleChange}
          error={form.errors.price}
          required
        />
        <Input
          label="Compare At Price"
          name="compareAtPrice"
          type="number"
          step="0.01"
          value={form.values.compareAtPrice}
          onChange={form.handleChange}
          error={form.errors.compareAtPrice}
        />
      </div>

      <div className={styles.row}>
        <Input
          label="Stock Quantity"
          name="stockQuantity"
          type="number"
          value={form.values.stockQuantity}
          onChange={form.handleChange}
          error={form.errors.stockQuantity}
          required
        />
        <div className={styles.field}>
          <label htmlFor="categoryId">Category</label>
          <select
            id="categoryId"
            name="categoryId"
            value={form.values.categoryId}
            onChange={form.handleChange}
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
          {form.errors.categoryId && <div className={styles.fieldError}>{form.errors.categoryId}</div>}
        </div>
      </div>

      <div className={styles.row}>
        <label className={styles.checkbox}>
          <input
            type="checkbox"
            name="isFeatured"
            checked={form.values.isFeatured}
            onChange={form.handleChange}
          />
          <span>Featured Product</span>
        </label>
      </div>

      <div className={styles.actions}>
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={form.isSubmitting}>
          {form.isSubmitting ? 'Saving...' : product ? 'Update Product' : 'Create Product'}
        </Button>
      </div>
    </form>
  );
}
