import { useGetTopLevelCategoriesQuery } from '../store/api/categoriesApi';
import { useTranslation } from 'react-i18next';
import styles from './CategoryFilter.module.css';

interface CategoryFilterProps {
  selectedCategoryId?: string;
  onSelectCategory: (categoryId?: string) => void;
}

export default function CategoryFilter({ selectedCategoryId, onSelectCategory }: CategoryFilterProps) {
  const { data: categories, isLoading, error } = useGetTopLevelCategoriesQuery();
  const { t } = useTranslation();

  if (error) {
    return (
      <div className={styles.error}>
        {t('products.failedToLoadCategories')}
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <h3 className={styles.title}>
        {t('products.categories')}
      </h3>

      {isLoading ? (
        <div className={styles.loading}>{t('products.loadingCategories')}</div>
      ) : (
        <ul className={styles.list}>
          <li>
            <button
              onClick={() => onSelectCategory(undefined)}
              className={`${styles.categoryButton} ${!selectedCategoryId ? styles.active : ''}`}
            >
              {t('products.allProducts')}
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
