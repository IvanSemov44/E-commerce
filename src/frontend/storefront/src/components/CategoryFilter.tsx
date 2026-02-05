import { useGetTopLevelCategoriesQuery } from '../store/api/categoriesApi';
import styles from './CategoryFilter.module.css';

interface CategoryFilterProps {
  selectedCategoryId?: string;
  onSelectCategory: (categoryId?: string) => void;
}

export default function CategoryFilter({ selectedCategoryId, onSelectCategory }: CategoryFilterProps) {
  const { data: categories, isLoading, error } = useGetTopLevelCategoriesQuery();

  if (error) {
    return (
      <div className={styles.error}>
        Failed to load categories
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <h3 className={styles.title}>
        Categories
      </h3>

      {isLoading ? (
        <div className={styles.loading}>Loading categories...</div>
      ) : (
        <ul className={styles.list}>
          <li>
            <button
              onClick={() => onSelectCategory(undefined)}
              className={`${styles.categoryButton} ${!selectedCategoryId ? styles.active : ''}`}
            >
              All Products
            </button>
          </li>

          {categories?.map((category) => (
            <li key={category.id}>
              <button
                onClick={() => onSelectCategory(category.id)}
                className={`${styles.categoryButton} ${selectedCategoryId === category.id ? styles.active : ''}`}
              >
                {category.name}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
