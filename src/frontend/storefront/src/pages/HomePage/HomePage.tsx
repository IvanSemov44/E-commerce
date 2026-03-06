import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { GridIcon } from '@/shared/components/icons';
import { useGetFeaturedProductsQuery, useGetProductsQuery } from '@/features/products/api/productApi';
import { useGetTopLevelCategoriesQuery } from '@/features/products/api/categoriesApi';
import Button from '@/shared/components/ui/Button';
import { ProductCard } from '@/features/products/components';
import PageHeader from '@/shared/components/PageHeader';
import QueryRenderer from '@/shared/components/QueryRenderer';
import TrustSignals from '@/shared/components/TrustSignals';
import styles from './HomePage.module.css';

export default function HomePage() {
  const { t } = useTranslation();
  const { data: featured, isLoading, error } = useGetFeaturedProductsQuery(10);
  const { data: categories } = useGetTopLevelCategoriesQuery();
  
  // Bestsellers - products sorted by review count (popularity)
  const { data: bestsellersData } = useGetProductsQuery({
    pageSize: 4,
    sortBy: 'rating',
    sortOrder: 'desc',
  });
  
  // Promotions - products with compareAtPrice (on sale)
  const { data: promotionsData } = useGetProductsQuery({
    pageSize: 4,
    sortBy: 'newest',
    sortOrder: 'desc',
  });

  return (
    <div className={styles.container}>
      {/* Hero Section */}
      <section className={styles.hero}>
        <div className={styles.heroContent}>
          <h1 className={styles.heroTitle}>
            {t('home.title')}
          </h1>
          <p className={styles.heroSubtitle}>
            {t('home.subtitle')}
          </p>
          <Link to="/products">
            <Button size="lg">
              {t('home.exploreProducts')}
            </Button>
          </Link>
        </div>
      </section>

      {/* Trust Signals Section */}
      <section className={styles.trustSection}>
        <TrustSignals variant="full" />
      </section>

      {/* Category Showcase */}
      {categories && categories.length > 0 && (
        <section className={styles.categoriesSection}>
          <PageHeader title={t('home.browseCategories')} />
          <div className={styles.categoriesGrid}>
            {categories.slice(0, 6).map((category) => (
              <Link
                key={category.id}
                to={`/products?category=${category.id}`}
                className={styles.categoryCard}
              >
                {category.imageUrl ? (
                  <img 
                    src={category.imageUrl} 
                    alt={category.name}
                    className={styles.categoryImage}
                  />
                ) : (
                  <div className={styles.categoryPlaceholder}>
                    <GridIcon />
                  </div>
                )}
                <span className={styles.categoryName}>{category.name}</span>
              </Link>
            ))}
          </div>
        </section>
      )}

      {/* Featured Products */}
      <section className={styles.featuredSection}>
        <PageHeader title={t('home.featuredProducts')} subtitle={t('home.featuredProductsSubtitle')} />

        <QueryRenderer
          isLoading={isLoading}
          error={error}
          data={featured}
          errorMessage={t('products.failedToLoadProducts')}
          emptyState={{
            icon: <GridIcon />,
            title: t('products.noProducts')
          }}
        >
          {(featured) => (
            <div className={styles.grid}>
              {featured.map((product) => (
                <ProductCard
                  key={product.id}
                  id={product.id}
                  name={product.name}
                  slug={product.slug}
                  price={product.price}
                  compareAtPrice={product.compareAtPrice}
                  imageUrl={product.images[0]?.url}
                  rating={Math.round(product.averageRating)}
                  reviewCount={product.reviewCount}
                />
              ))}
            </div>
          )}
        </QueryRenderer>
      </section>

      {/* Bestsellers Section */}
      {bestsellersData?.items && bestsellersData.items.length > 0 && (
        <section className={styles.bestsellersSection}>
          <PageHeader title={t('home.bestSellers')} subtitle={t('home.ourMostPopularProducts')} />
          <div className={styles.grid}>
            {bestsellersData.items.map((product) => (
              <ProductCard
                key={product.id}
                id={product.id}
                name={product.name}
                slug={product.slug}
                price={product.price}
                compareAtPrice={product.compareAtPrice}
                imageUrl={product.images[0]?.url}
                rating={Math.round(product.averageRating)}
                reviewCount={product.reviewCount}
              />
            ))}
          </div>
          <div className={styles.sectionCta}>
            <Link to="/products?sortBy=rating&sortOrder=desc">
              <Button variant="outline">{t('home.viewAllBestsellers')}</Button>
            </Link>
          </div>
        </section>
      )}

      {/* Promotions Section */}
      {promotionsData?.items && promotionsData.items.some(p => p.compareAtPrice) && (
        <section className={styles.promotionsSection}>
          <PageHeader title={t('home.onSale')} subtitle={t('home.onSaleSubtitle')} />
          <div className={styles.grid}>
            {promotionsData.items
              .filter((product) => product.compareAtPrice)
              .slice(0, 4)
              .map((product) => (
                <ProductCard
                  key={product.id}
                  id={product.id}
                  name={product.name}
                  slug={product.slug}
                  price={product.price}
                  compareAtPrice={product.compareAtPrice}
                  imageUrl={product.images[0]?.url}
                  rating={Math.round(product.averageRating)}
                  reviewCount={product.reviewCount}
                />
              ))}
          </div>
          <div className={styles.sectionCta}>
            <Link to="/products?onSale=true">
              <Button variant="outline">{t('home.viewAllOffers')}</Button>
            </Link>
          </div>
        </section>
      )}
    </div>
  );
}
