import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useGetFeaturedProductsQuery, useGetProductsQuery } from '../store/api/productApi';
import { useGetTopLevelCategoriesQuery } from '../store/api/categoriesApi';
import Button from '../components/ui/Button';
import ProductCard from '../components/ProductCard';
import PageHeader from '../components/PageHeader';
import QueryRenderer from '../components/QueryRenderer';
import TrustSignals from '../components/TrustSignals';
import styles from './Home.module.css';

export default function Home() {
  const { t } = useTranslation();
  const { data: featured, isLoading, error } = useGetFeaturedProductsQuery(10);
  const { data: categories } = useGetTopLevelCategoriesQuery();
  
  // Bestsellers - products sorted by review count (popularity)
  const { data: bestsellersData } = useGetProductsQuery({
    pageSize: 4,
    sortBy: 'reviewCount',
    sortOrder: 'desc',
  });
  
  // Promotions - products with compareAtPrice (on sale)
  const { data: promotionsData } = useGetProductsQuery({
    pageSize: 4,
    sortBy: 'createdAt',
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
                    <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
                    </svg>
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
            icon: (
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
              </svg>
            ),
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
            <Link to="/products?sortBy=reviewCount&sortOrder=desc">
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
