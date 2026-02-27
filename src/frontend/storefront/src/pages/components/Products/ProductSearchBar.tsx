import { useTranslation } from 'react-i18next';
import styles from './ProductSearchBar.module.css';

interface ProductSearchBarProps {
  searchValue: string;
  sortBy: string;
  onSearchChange: (value: string) => void;
  onSortChange: (value: string) => void;
}

export default function ProductSearchBar({
  searchValue,
  sortBy,
  onSearchChange,
  onSortChange,
}: ProductSearchBarProps) {
  const { t } = useTranslation();
  
  return (
    <div className={styles.searchBar}>
      <input
        type="text"
        placeholder={t('products.searchProducts')}
        value={searchValue}
        onChange={(e) => onSearchChange(e.target.value)}
        className={styles.searchInput}
      />
      <select
        value={sortBy}
        onChange={(e) => onSortChange(e.target.value)}
        className={styles.sortSelect}
      >
        <option value="newest">{t('products.sortNewest')}</option>
        <option value="name">{t('products.sortNameAZ')}</option>
        <option value="price-asc">{t('products.sortPriceLowHigh')}</option>
        <option value="price-desc">{t('products.sortPriceHighLow')}</option>
        <option value="rating">{t('products.sortRating')}</option>
      </select>
    </div>
  );
}
