import { useState } from 'react';
import toast from 'react-hot-toast';
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
import ProductForm from '../components/ProductForm';
import styles from './Products.module.css';
import type { Product, ProductDetail, CreateProductRequest, UpdateProductRequest } from '@shared/types';

export default function Products() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<ProductDetail | undefined>();

  const { data: productsResult, isLoading, error } = useGetProductsQuery({
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

  const handleCreate = () => {
    setEditingProduct(undefined);
    setModalOpen(true);
  };

  const handleEdit = (product: Product) => {
    // Cast to ProductDetail for now
    setEditingProduct(product as ProductDetail);
    setModalOpen(true);
  };

  const handleDelete = async (productId: string) => {
    if (!confirm('Are you sure you want to delete this product?')) return;

    try {
      await deleteProduct(productId).unwrap();
      toast.success('Product deleted successfully');
    } catch {
      toast.error('Failed to delete product');
    }
  };

  const handleFormSubmit = async (data: CreateProductRequest | (UpdateProductRequest & { id: string })) => {
    try {
      if (editingProduct) {
        await updateProduct(data as UpdateProductRequest).unwrap();
        toast.success('Product updated successfully');
      } else {
        await createProduct(data as CreateProductRequest).unwrap();
        toast.success('Product created successfully');
      }
      setModalOpen(false);
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
          <Button
            size="sm"
            variant="outline"
            onClick={() => handleEdit(product)}
          >
            Edit
          </Button>
          <Button
            size="sm"
            variant="destructive"
            onClick={() => handleDelete(product.id)}
          >
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

      <Card variant="elevated">
        {isLoading ? (
          <div className={styles.loadingState}>
            Loading products...
          </div>
        ) : error ? (
          <div className={styles.errorState}>
            Failed to load products
          </div>
        ) : (
          <>
            <Table
              columns={columns}
              data={productsResult?.items || []}
              keyExtractor={(product) => product.id}
            />
            <div className={styles.modalFooter}>
              <Pagination
                currentPage={page}
                totalPages={totalPages}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </Card>

      <Modal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        title={editingProduct ? 'Edit Product' : 'Create Product'}
        size="lg"
      >
        <ProductForm
          product={editingProduct}
          categories={categories}
          onSubmit={handleFormSubmit}
          onCancel={() => setModalOpen(false)}
        />
      </Modal>
    </div>
  );
}
