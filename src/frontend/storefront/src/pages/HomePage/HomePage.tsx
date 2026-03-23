import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePerformanceMonitor } from '@/shared/hooks';
import { GridIcon } from '@/shared/components/icons';
import {
  useGetFeaturedProductsQuery,
  useGetProductsQuery,
  useGetTopLevelCategoriesQuery,
} from '@/features/products/api';
import type { Product } from '@/shared/types';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { withQuery } from '@/shared/lib/routing';
import { Button } from '@/shared/components/ui/Button';
import { ProductCard } from '@/features/products/components';
import PageHeader from '@/shared/components/PageHeader';
import { TrustSignals } from '@/shared/components/TrustSignals';
import styles from './HomePage.module.css';

interface ProductGridProps {
  products: Product[];
}

function ProductGrid({ products }: ProductGridProps) {
  return (
    <div className={styles.grid}>
      {products.map((product) => (
        <ProductCard
          key={product.id}
          id={product.id}
          name={product.name}
          slug={product.slug}
          price={product.price}
          compareAtPrice={product.compareAtPrice}
          imageUrl={product.images[0]?.url}
          rating={product.averageRating}
          reviewCount={product.reviewCount}
          stockQuantity={product.stockQuantity}
        />
      ))}
    </div>
  );
}

interface ProductSectionProps {
  ariaLabel: string;
  title: string;
  subtitle: string;
  products: Product[];
  ctaTo: string;
  ctaLabel: string;
  sectionClassName: string;
}

function ProductSection({
  ariaLabel,
  title,
  subtitle,
  products,
  ctaTo,
  ctaLabel,
  sectionClassName,
}: ProductSectionProps) {
  return (
    <section className={sectionClassName} aria-label={ariaLabel}>
      <PageHeader title={title} subtitle={subtitle} />
      <ProductGrid products={products} />
      <div className={styles.sectionCta}>
        <Link to={ctaTo}>
          <Button variant="outline">{ctaLabel}</Button>
        </Link>
      </div>
    </section>
  );
}

export default function HomePage() {
  usePerformanceMonitor();
  const { t } = useTranslation();
  const { data: featured } = useGetFeaturedProductsQuery({ page: 1, pageSize: 10 });
  const { data: categories } = useGetTopLevelCategoriesQuery();

  // Bestsellers - products sorted by review count (popularity)
  const { data: bestsellersData } = useGetProductsQuery({
    pageSize: 4,
    sortBy: 'rating',
  });

  // Promotions - products with compareAtPrice (on sale)
  const { data: promotionsData } = useGetProductsQuery({
    pageSize: 4,
    sortBy: 'newest',
  });

  return (
    <div className={styles.container}>
      {/* Hero Section */}
      <section className={styles.hero} aria-label={t('home.title')}>
        <div className={styles.heroContent}>
          <h1 className={styles.heroTitle}>{t('home.title')}</h1>
          <p className={styles.heroSubtitle}>{t('home.subtitle')}</p>
          <Link to={ROUTE_PATHS.products}>
            <Button size="lg">{t('home.exploreProducts')}</Button>
          </Link>
        </div>
      </section>

      {/* Trust Signals Section */}
      <section className={styles.trustSection} aria-label={t('trustSignals.title')}>
        <TrustSignals variant="full" />
      </section>

      {/* Category Showcase */}
      {categories && categories.length > 0 && (
        <section className={styles.categoriesSection} aria-label={t('home.browseCategories')}>
          <PageHeader title={t('home.browseCategories')} />
          <div className={styles.categoriesGrid}>
            {categories.slice(0, 6).map((category) => (
              <Link
                key={category.id}
                to={withQuery(ROUTE_PATHS.products, { category: category.id })}
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
      {featured?.items && featured.items.length > 0 && (
        <ProductSection
          ariaLabel={t('home.featuredProducts')}
          title={t('home.featuredProducts')}
          subtitle={t('home.featuredProductsSubtitle')}
          products={featured.items}
          ctaTo={ROUTE_PATHS.products}
          ctaLabel={t('home.viewAllFeatured')}
          sectionClassName={styles.featuredSection}
        />
      )}

      {/* Bestsellers Section */}
      {bestsellersData?.items && bestsellersData.items.length > 0 && (
        <ProductSection
          ariaLabel={t('home.bestSellers')}
          title={t('home.bestSellers')}
          subtitle={t('home.ourMostPopularProducts')}
          products={bestsellersData.items}
          ctaTo={withQuery(ROUTE_PATHS.products, { sortBy: 'rating' })}
          ctaLabel={t('home.viewAllBestsellers')}
          sectionClassName={styles.bestsellersSection}
        />
      )}

      {/* Promotions Section */}
      {promotionsData?.items && promotionsData.items.some((p) => p.compareAtPrice) && (
        <ProductSection
          ariaLabel={t('home.onSale')}
          title={t('home.onSale')}
          subtitle={t('home.onSaleSubtitle')}
          products={promotionsData.items.filter((product) => product.compareAtPrice).slice(0, 4)}
          ctaTo={withQuery(ROUTE_PATHS.products, { onSale: true })}
          ctaLabel={t('home.viewAllOffers')}
          sectionClassName={styles.promotionsSection}
        />
      )}
    </div>
  );
}
