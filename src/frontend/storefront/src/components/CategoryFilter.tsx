import { useGetTopLevelCategoriesQuery } from '../store/api/categoriesApi';

interface CategoryFilterProps {
  selectedCategoryId?: string;
  onSelectCategory: (categoryId?: string) => void;
}

export default function CategoryFilter({ selectedCategoryId, onSelectCategory }: CategoryFilterProps) {
  const { data: categories, isLoading, error } = useGetTopLevelCategoriesQuery();

  if (error) {
    return (
      <div style={{ padding: '1rem', color: '#d32f2f' }}>
        Failed to load categories
      </div>
    );
  }

  return (
    <div style={{
      borderRadius: '0.5rem',
      border: '1px solid #e0e0e0',
      padding: '1.5rem',
      backgroundColor: '#fafafa'
    }}>
      <h3 style={{ marginTop: 0, marginBottom: '1rem', fontSize: '1.125rem', fontWeight: 600 }}>
        Categories
      </h3>

      {isLoading ? (
        <div style={{ color: '#666', fontSize: '0.875rem' }}>Loading categories...</div>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          <li>
            <button
              onClick={() => onSelectCategory(undefined)}
              style={{
                background: 'none',
                border: 'none',
                padding: '0.5rem 0',
                cursor: 'pointer',
                color: !selectedCategoryId ? '#1976d2' : '#666',
                fontWeight: !selectedCategoryId ? 600 : 400,
                fontSize: '0.95rem',
                transition: 'color 0.2s'
              }}
              onMouseEnter={(e) => {
                if (!selectedCategoryId) return;
                e.currentTarget.style.color = '#1976d2';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.color = !selectedCategoryId ? '#1976d2' : '#666';
              }}
            >
              All Products
            </button>
          </li>

          {categories?.map((category) => (
            <li key={category.id}>
              <button
                onClick={() => onSelectCategory(category.id)}
                style={{
                  background: 'none',
                  border: 'none',
                  padding: '0.5rem 0',
                  cursor: 'pointer',
                  color: selectedCategoryId === category.id ? '#1976d2' : '#666',
                  fontWeight: selectedCategoryId === category.id ? 600 : 400,
                  fontSize: '0.95rem',
                  transition: 'color 0.2s'
                }}
                onMouseEnter={(e) => {
                  if (selectedCategoryId === category.id) return;
                  e.currentTarget.style.color = '#1976d2';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.color = selectedCategoryId === category.id ? '#1976d2' : '#666';
                }}
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
