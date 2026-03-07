import { useState } from 'react';
import toast from 'react-hot-toast';
import { useCrudModal } from '../hooks/useCrudModal';
import { useConfirmation } from '../hooks/useConfirmation';
import {
  useGetProductsQuery,
  useCreateProductMutation,
  useUpdateProductMutation,
  useDeleteProductMutation,
} from '../store/api/productsApi';
import Button from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import Input from '../components/ui/Input';
import Table from '../components/ui/Table';
import Modal from '../components/ui/Modal';
import Pagination from '../components/ui/Pagination';
import QueryRenderer from '../components/QueryRenderer';
import ConfirmationDialog from '../components/ui/ConfirmationDialog';
import ProductForm from '../components/ProductForm';
import styles from './Products.module.css';
import type {
  Product,
  ProductDetail,
  CreateProductRequest,
  UpdateProductRequest,
} from '@shared/types';

// eslint-disable-next-line max-lines-per-function -- Products CRUD page: inline column JSX, handlers, and modal
export default function Products() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const { modalOpen, editingItem: editingProduct, handleCreate, handleEdit, handleClose } = useCrudModal<ProductDetail>();
  const { confirmation, confirm, handleConfirm, handleCancel } = useConfirmation();

  const {
    data: productsResult,
    isLoading,
    error,
  } = useGetProductsQuery({
    page,
    pageSize: 20,
    search,
  });

  const [createProduct] = useCreateProductMutation();
  const [updateProduct] = useUpdateProductMutation();
  const [deleteProduct] = useDeleteProductMutation();

  // Mock categories for now (replace with real API later)
  const categories = [
    { id: '1', name: 'Electronics' },
    { id: '2', name: 'Clothing' },
    { id: '3', name: 'Home & Garden' },
  ];

  const handleDelete = (productId: string) => {
    confirm('Delete Product', 'Are you sure you want to delete this product? This action cannot be undone.', async () => {
      try {
        await deleteProduct(productId).unwrap();
        toast.success('Product deleted successfully');
      } catch {
        toast.error('Failed to delete product');
      }
    });
  };

  const handleFormSubmit = async (
    data: CreateProductRequest | (UpdateProductRequest & { id: string })
  ) => {
    try {
      if (editingProduct) {
        await updateProduct(data as UpdateProductRequest).unwrap();
        toast.success('Product updated successfully');
      } else {
        await createProduct(data as CreateProductRequest).unwrap();
        toast.success('Product created successfully');
      }
      handleClose();
    } catch {
      // Error handling is delegated to the form component
    }
  };

  const columns = [
    {
      header: 'Name',
      accessor: (product: Product) => product.name,
      width: '25%',
    },
    {
      header: 'Price',
      accessor: (product: Product) => `$${product.price.toFixed(2)}`,
      width: '10%',
    },
    {
      header: 'Stock',
      accessor: (product: Product) => product.stockQuantity,
      width: '10%',
    },
    {
      header: 'Category',
      accessor: (product: Product) => product.category?.name || 'N/A',
      width: '15%',
    },
    {
      header: 'Featured',
      accessor: (product: Product) => (product.isFeatured ? '⭐' : ''),
      width: '10%',
    },
    {
      header: 'Rating',
      accessor: (product: Product) =>
        `${product.averageRating.toFixed(1)} (${product.reviewCount})`,
      width: '10%',
    },
    {
      header: 'Actions',
      accessor: (product: Product) => (
        <div className={styles.actionButtons}>
          <Button size="sm" variant="outline" onClick={() => handleEdit(product as ProductDetail)}>
            Edit
          </Button>
          <Button size="sm" variant="destructive" onClick={() => handleDelete(product.id)}>
            Delete
          </Button>
        </div>
      ),
      width: '20%',
    },
  ];

  const totalPages = productsResult
    ? Math.ceil(productsResult.totalCount / productsResult.pageSize)
    : 1;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>Products</h1>
        <div className={styles.actions}>
          <Input
            placeholder="Search products..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className={styles.searchInput}
          />
          <Button onClick={handleCreate}>Add Product</Button>
        </div>
      </div>

      <QueryRenderer
        isLoading={isLoading}
        error={error}
        data={productsResult?.items}
        emptyTitle="No Products"
        emptyMessage="No products found"
        isEmpty={(items) => items.length === 0}
      >
        {(items) => (
          <Card variant="elevated">
            <Table
              columns={columns}
              data={items}
              keyExtractor={(product) => product.id}
            />
            <div className={styles.modalFooter}>
              <Pagination currentPage={page} totalPages={totalPages} onPageChange={setPage} />
            </div>
          </Card>
        )}
      </QueryRenderer>

      <Modal
        isOpen={modalOpen}
        onClose={handleClose}
        title={editingProduct ? 'Edit Product' : 'Create Product'}
        size="lg"
      >
        <ProductForm
          product={editingProduct}
          categories={categories}
          onSubmit={handleFormSubmit}
          onCancel={handleClose}
        />
      </Modal>

      <ConfirmationDialog
        isOpen={confirmation.isOpen}
        title={confirmation.title}
        message={confirmation.message}
        onConfirm={handleConfirm}
        onCancel={handleCancel}
      />
    </div>
  );
}
