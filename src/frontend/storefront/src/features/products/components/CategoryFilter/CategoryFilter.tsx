import { useSearchParams } from 'react-router';
import { useGetTopLevelCategoriesQuery } from '@/features/products/api/categoriesApi';
import { useTranslation } from 'react-i18next';
import styles from './CategoryFilter.module.css';

export function CategoryFilter() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const selectedCategoryId = searchParams.get('categoryId') ?? undefined;

  const { data: categories, isLoading, error } = useGetTopLevelCategoriesQuery();

  if (isLoading) {
    return <div className={styles.loading}>{t('products.loadingCategories')}</div>;
  }

  if (error) {
    return <div className={styles.error}>{t('products.failedToLoadCategories')}</div>;
  }

  const handleSelect = (id: string | undefined) => {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        if (id) next.set('categoryId', id);
        else next.delete('categoryId');
        next.delete('page');
        return next;
      },
      { replace: true }
    );
  };

  return (
    <div className={styles.container}>
      <h3 className={styles.title}>{t('products.categories')}</h3>
      <ul className={styles.list}>
        <li>
          <button
            onClick={() => handleSelect(undefined)}
            className={`${styles.categoryButton} ${!selectedCategoryId ? styles.active : ''}`}
          >
            {t('products.allProducts')}
          </button>
        </li>
        {categories?.map((category) => (
          <li key={category.id}>
            <button
              onClick={() => handleSelect(category.id)}
              className={`${styles.categoryButton} ${selectedCategoryId === category.id ? styles.active : ''}`}
            >
              {category.name}
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}
