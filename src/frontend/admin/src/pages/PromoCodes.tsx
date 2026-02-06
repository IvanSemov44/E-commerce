import { useState } from 'react';
import toast from 'react-hot-toast';
import {
  useGetPromoCodesQuery,
  useCreatePromoCodeMutation,
  useUpdatePromoCodeMutation,
  useDeactivatePromoCodeMutation,
} from '../store/api/promoCodesApi';
import Button from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import Input from '../components/ui/Input';
import Table from '../components/ui/Table';
import Modal from '../components/ui/Modal';
import Pagination from '../components/ui/Pagination';
import Badge from '../components/ui/Badge';
import PromoCodeForm from '../components/PromoCodeForm';
import styles from './PromoCodes.module.css';
import type { PromoCode, PromoCodeDetail } from '@shared/types';

export default function PromoCodes() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [activeFilter, setActiveFilter] = useState<boolean | undefined>(undefined);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingPromoCode, setEditingPromoCode] = useState<PromoCodeDetail | undefined>();

  const { data: promoCodesResult, isLoading, error } = useGetPromoCodesQuery({
    page,
    pageSize: 20,
    search,
    isActive: activeFilter,
  });

  const [createPromoCode] = useCreatePromoCodeMutation();
  const [updatePromoCode] = useUpdatePromoCodeMutation();
  const [deactivatePromoCode] = useDeactivatePromoCodeMutation();

  const handleCreate = () => {
    setEditingPromoCode(undefined);
    setModalOpen(true);
  };

  const handleEdit = (promoCode: PromoCode) => {
    setEditingPromoCode(promoCode as PromoCodeDetail);
    setModalOpen(true);
  };

  const handleDeactivate = async (promoCodeId: string) => {
    if (!confirm('Are you sure you want to deactivate this promo code?')) return;

    try {
      await deactivatePromoCode(promoCodeId).unwrap();
      toast.success('Promo code deactivated successfully');
    } catch (err) {
      toast.error('Failed to deactivate promo code');
    }
  };

  const handleFormSubmit = async (data: CreatePromoCodeRequest | (UpdatePromoCodeRequest & { id: string })) => {
    try {
      if (editingPromoCode) {
        await updatePromoCode({ ...data, id: editingPromoCode.id }).unwrap();
        toast.success('Promo code updated successfully');
      } else {
        await createPromoCode(data).unwrap();
        toast.success('Promo code created successfully');
      }
      setModalOpen(false);
    } catch (err) {
      throw err;
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const columns = [
    {
      header: 'Code',
      accessor: (promo: PromoCode) => promo.code,
      width: '12%',
    },
    {
      header: 'Type',
      accessor: (promo: PromoCode) =>
        promo.discountType === 'percentage' ? 'Percentage' : 'Fixed',
      width: '10%',
    },
    {
      header: 'Value',
      accessor: (promo: PromoCode) =>
        promo.discountType === 'percentage'
          ? `${promo.discountValue}%`
          : `$${promo.discountValue.toFixed(2)}`,
      width: '10%',
    },
    {
      header: 'Min Order',
      accessor: (promo: PromoCode) =>
        promo.minOrderAmount ? `$${promo.minOrderAmount.toFixed(2)}` : 'None',
      width: '10%',
    },
    {
      header: 'Max Uses',
      accessor: (promo: PromoCode) => promo.maxUses || 'Unlimited',
      width: '10%',
    },
    {
      header: 'Used',
      accessor: (promo: PromoCode) => promo.usedCount,
      width: '8%',
    },
    {
      header: 'Status',
      accessor: (promo: PromoCode) => (
        <Badge variant={promo.isActive ? 'success' : 'default'}>
          {promo.isActive ? 'Active' : 'Inactive'}
        </Badge>
      ),
      width: '10%',
    },
    {
      header: 'Dates',
      accessor: (promo: PromoCode) => (
        <div className={styles.codeDetails}>
          <div>Start: {formatDate(promo.startDate)}</div>
          <div>End: {formatDate(promo.endDate)}</div>
        </div>
      ),
      width: '15%',
    },
    {
      header: 'Actions',
      accessor: (promo: PromoCode) => (
        <div className={styles.actionButtons}>
          <Button
            size="sm"
            variant="outline"
            onClick={() => handleEdit(promo)}
          >
            Edit
          </Button>
          {promo.isActive && (
            <Button
              size="sm"
              variant="destructive"
              onClick={() => handleDeactivate(promo.id)}
            >
              Deactivate
            </Button>
          )}
        </div>
      ),
      width: '15%',
    },
  ];

  const totalPages = promoCodesResult
    ? Math.ceil(promoCodesResult.totalCount / promoCodesResult.pageSize)
    : 1;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>Promo Codes</h1>
        <Button onClick={handleCreate}>Create Promo Code</Button>
      </div>

      <div className={styles.filters}>
        <select
          value={activeFilter === undefined ? 'all' : activeFilter ? 'active' : 'inactive'}
          onChange={(e) => {
            const value = e.target.value;
            setActiveFilter(value === 'all' ? undefined : value === 'active');
            setPage(1);
          }}
          className={styles.filterSelect}
        >
          <option value="all">All Status</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>

        <Input
          placeholder="Search promo codes..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          className={styles.searchInput}
        />
      </div>

      <Card variant="elevated">
        {isLoading ? (
          <div className={styles.loadingState}>
            Loading promo codes...
          </div>
        ) : error ? (
          <div className={styles.errorState}>
            Failed to load promo codes
          </div>
        ) : (
          <>
            <Table
              columns={columns}
              data={promoCodesResult?.items || []}
              keyExtractor={(promo) => promo.id}
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
        title={editingPromoCode ? 'Edit Promo Code' : 'Create Promo Code'}
        size="lg"
      >
        <PromoCodeForm
          promoCode={editingPromoCode}
          onSubmit={handleFormSubmit}
          onCancel={() => setModalOpen(false)}
        />
      </Modal>
    </div>
  );
}
