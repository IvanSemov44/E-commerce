import { Link } from 'react-router';
import type { Product } from '@/shared/types';
import { Button } from '@/shared/components/ui/Button';
import PageHeader from '@/shared/components/PageHeader';
import { SimpleProductGrid } from '../SimpleProductGrid';
import styles from './ProductSection.module.css';

export interface ProductSectionProps {
  ariaLabel: string;
  title: string;
  subtitle: string;
  products: Product[];
  ctaTo: string;
  ctaLabel: string;
  sectionClassName: string;
}

/**
 * ProductSection - A section component for displaying a group of products
 * with a header, product grid, and call-to-action button.
 */
export function ProductSection({
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
      <SimpleProductGrid products={products} />
      <div className={styles.sectionCta}>
        <Link to={ctaTo}>
          <Button variant="outline">{ctaLabel}</Button>
        </Link>
      </div>
    </section>
  );
}
