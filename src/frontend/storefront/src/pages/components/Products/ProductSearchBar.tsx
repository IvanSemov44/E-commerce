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
  return (
    <div className={styles.searchBar}>
      <input
        type="text"
        placeholder="Search products..."
        value={searchValue}
        onChange={(e) => onSearchChange(e.target.value)}
        className={styles.searchInput}
      />
      <select
        value={sortBy}
        onChange={(e) => onSortChange(e.target.value)}
        className={styles.sortSelect}
      >
        <option value="newest">Newest First</option>
        <option value="name">Name (A-Z)</option>
        <option value="price-asc">Price: Low to High</option>
        <option value="price-desc">Price: High to Low</option>
        <option value="rating">Highest Rated</option>
      </select>
    </div>
  );
}
