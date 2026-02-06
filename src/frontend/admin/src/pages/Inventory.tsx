import { useState } from 'react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import {
  useGetInventoryQuery,
  useGetLowStockProductsQuery,
  useAdjustStockMutation,
  useRestockProductMutation,
  type InventoryItem,
} from '../store/api/inventoryApi';
import styles from './Inventory.module.css';

export default function Inventory() {
  const [search, setSearch] = useState('');
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<InventoryItem | null>(null);
  const [showAdjustModal, setShowAdjustModal] = useState(false);
  const [newQuantity, setNewQuantity] = useState('');
  const [reason, setReason] = useState('adjustment');
  const [notes, setNotes] = useState('');
  const [adjustType, setAdjustType] = useState<'set' | 'add'>('set');

  const { data, isLoading, error, refetch } = useGetInventoryQuery({
    search,
    lowStockOnly,
  });

  const { data: lowStockData } = useGetLowStockProductsQuery();
  const [adjustStock, { isLoading: isAdjusting }] = useAdjustStockMutation();
  const [restockProduct, { isLoading: isRestocking }] = useRestockProductMutation();

  const inventory = data?.data || [];
  const lowStockCount = lowStockData?.count || 0;

  const handleOpenAdjustModal = (product: InventoryItem, type: 'set' | 'add') => {
    setSelectedProduct(product);
    setAdjustType(type);
    setNewQuantity('');
    setReason(type === 'add' ? 'restock' : 'adjustment');
    setNotes('');
    setShowAdjustModal(true);
  };

  const handleCloseAdjustModal = () => {
    setShowAdjustModal(false);
    setSelectedProduct(null);
    setNewQuantity('');
    setReason('adjustment');
    setNotes('');
  };

  const handleSubmitAdjustment = async () => {
    if (!selectedProduct || !newQuantity) return;

    const quantity = parseInt(newQuantity, 10);
    if (isNaN(quantity) || quantity < 0) {
      toast.error('Please enter a valid quantity');
      return;
    }

    try {
      if (adjustType === 'add') {
        await restockProduct({
          productId: selectedProduct.productId,
          request: { quantity, reason, notes },
        }).unwrap();
        toast.success(`Successfully added ${quantity} units to ${selectedProduct.productName}`);
      } else {
        await adjustStock({
          productId: selectedProduct.productId,
          request: { quantity, reason, notes },
        }).unwrap();
        toast.success(`Stock adjusted to ${quantity} units for ${selectedProduct.productName}`);
      }
      handleCloseAdjustModal();
      refetch();
    } catch (err: any) {
      toast.error(err?.data?.message || 'Failed to adjust stock');
    }
  };

  const getStockStatusClass = (item: InventoryItem) => {
    if (item.isOutOfStock) return styles.stockOutOfStock;
    if (item.isLowStock) return styles.stockLowStock;
    return styles.stockInStock;
  };

  const getStatusBadgeClass = (item: InventoryItem) => {
    if (item.isOutOfStock) return `${styles.statusBadge} ${styles.statusOutOfStock}`;
    if (item.isLowStock) return `${styles.statusBadge} ${styles.statusLowStock}`;
    return `${styles.statusBadge} ${styles.statusInStock}`;
  };

  const getStockStatusLabel = (item: InventoryItem) => {
    if (item.isOutOfStock) return 'Out of Stock';
    if (item.isLowStock) return 'Low Stock';
    return 'In Stock';
  };

  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.loading}>Loading inventory...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>Error loading inventory. Please try again.</div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1>Inventory Management</h1>
        {lowStockCount > 0 && (
          <div className={styles.alert}>
            <span className={styles.alertIcon}>⚠️</span>
            <span>{lowStockCount} product{lowStockCount !== 1 ? 's' : ''} running low on stock</span>
          </div>
        )}
      </div>

      <div className={styles.filters}>
        <input
          type="text"
          placeholder="Search by product name or SKU..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className={styles.searchInput}
        />
        <label className={styles.checkbox}>
          <input
            type="checkbox"
            checked={lowStockOnly}
            onChange={(e) => setLowStockOnly(e.target.checked)}
          />
          Show low stock only
        </label>
      </div>

      <div className={styles.tableContainer}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Product</th>
              <th>SKU</th>
              <th>Price</th>
              <th>Current Stock</th>
              <th>Threshold</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {inventory.length === 0 ? (
              <tr>
                <td colSpan={7} className={styles.noData}>
                  No products found
                </td>
              </tr>
            ) : (
              inventory.map((item) => (
                <tr key={item.productId}>
                  <td>
                    <div className={styles.productCell}>
                      {item.imageUrl && (
                        <img
                          src={item.imageUrl}
                          alt={item.productName}
                          className={styles.productImage}
                        />
                      )}
                      <span>{item.productName}</span>
                    </div>
                  </td>
                  <td>{item.sku || '-'}</td>
                  <td>${item.price.toFixed(2)}</td>
                  <td>
                    <span className={`${styles.stockQuantity} ${getStockStatusClass(item)}`}>
                      {item.stockQuantity}
                    </span>
                  </td>
                  <td>{item.lowStockThreshold}</td>
                  <td>
                    <span className={getStatusBadgeClass(item)}>
                      {getStockStatusLabel(item)}
                    </span>
                  </td>
                  <td>
                    <div className={styles.actions}>
                      <button
                        onClick={() => handleOpenAdjustModal(item, 'add')}
                        className={styles.btnRestock}
                        title="Add stock"
                      >
                        +
                      </button>
                      <button
                        onClick={() => handleOpenAdjustModal(item, 'set')}
                        className={styles.btnAdjust}
                        title="Adjust stock"
                      >
                        ⚙️
                      </button>
                      <Link
                        to={`/products/edit/${item.productId}`}
                        className={styles.btnView}
                        title="Edit product"
                      >
                        ✏️
                      </Link>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {showAdjustModal && selectedProduct && (
        <div className={styles.modalOverlay} onClick={handleCloseAdjustModal}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <h2>
                {adjustType === 'add' ? 'Add Stock' : 'Adjust Stock'} - {selectedProduct.productName}
              </h2>
              <button onClick={handleCloseAdjustModal} className={styles.closeBtn}>
                ×
              </button>
            </div>

            <div className={styles.modalBody}>
              <div className={styles.currentStock}>
                <strong>Current Stock:</strong> {selectedProduct.stockQuantity} units
              </div>

              <div className={styles.formGroup}>
                <label>
                  {adjustType === 'add' ? 'Quantity to Add' : 'New Stock Quantity'}
                </label>
                <input
                  type="number"
                  value={newQuantity}
                  onChange={(e) => setNewQuantity(e.target.value)}
                  min="0"
                  className={styles.input}
                  placeholder={adjustType === 'add' ? 'Enter quantity to add' : 'Enter new stock quantity'}
                />
              </div>

              <div className={styles.formGroup}>
                <label>Reason</label>
                <select
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  className={styles.select}
                >
                  <option value="restock">Restock</option>
                  <option value="adjustment">Adjustment</option>
                  <option value="damage">Damage</option>
                  <option value="correction">Correction</option>
                  <option value="return">Return</option>
                </select>
              </div>

              <div className={styles.formGroup}>
                <label>Notes (Optional)</label>
                <textarea
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  className={styles.textarea}
                  rows={3}
                  placeholder="Add any additional notes..."
                />
              </div>

              {adjustType === 'add' && newQuantity && (
                <div className={styles.preview}>
                  New stock will be: {selectedProduct.stockQuantity + parseInt(newQuantity || '0', 10)} units
                </div>
              )}
            </div>

            <div className={styles.modalFooter}>
              <button onClick={handleCloseAdjustModal} className={styles.btnCancel}>
                Cancel
              </button>
              <button
                onClick={handleSubmitAdjustment}
                className={styles.btnSubmit}
                disabled={isAdjusting || isRestocking || !newQuantity}
              >
                {isAdjusting || isRestocking ? 'Processing...' : 'Confirm'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
