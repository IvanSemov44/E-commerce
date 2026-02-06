/**
 * OptimizedImage Component
 * Lazy-loads images with modern format support (AVIF/WebP fallback)
 * Reduces initial page load time by deferring image downloads
 */

import { useState, useEffect, useRef } from 'react';
import styles from './OptimizedImage.module.css';

interface OptimizedImageProps {
  src: string;
  alt: string;
  width?: number;
  height?: number;
  className?: string;
  loading?: 'eager' | 'lazy';
  onLoad?: () => void;
  onError?: (error: Error) => void;
}

/**
 * Optimized image component with:
 * - Lazy loading (deferred until visible in viewport)
 * - Progressive enhancement (AVIF → WebP → JPG/PNG)
 * - Automatic aspect ratio preservation
 * - Loading skeleton
 * - Error handling
 */
export default function OptimizedImage({
  src,
  alt,
  width,
  height,
  className = '',
  loading = 'lazy',
  onLoad,
  onError,
}: OptimizedImageProps) {
  const [isLoaded, setIsLoaded] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const imgRef = useRef<HTMLImageElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // Set up intersection observer for lazy loading
  useEffect(() => {
    if (loading === 'eager') {
      // Eager load: don't use intersection observer
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting && imgRef.current) {
            // Image is visible - load it
            imgRef.current.src = src;
            observer.unobserve(entry.target);
          }
        });
      },
      {
        rootMargin: '50px', // Start loading 50px before visible
      }
    );

    if (containerRef.current) {
      observer.observe(containerRef.current);
    }

    return () => observer.disconnect();
  }, [src, loading]);

  const handleImageLoad = () => {
    setIsLoaded(true);
    setError(null);
    onLoad?.();
  };

  const handleImageError = (e: React.SyntheticEvent<HTMLImageElement>) => {
    const error = new Error(`Failed to load image: ${src}`);
    setError(error);
    onError?.(error);
  };

  // Determine aspect ratio for spacing reserve
  const aspectRatioStyle =
    width && height
      ? {
          aspectRatio: `${width} / ${height}`,
        }
      : {};

  return (
    <div
      ref={containerRef}
      className={`${styles.container} ${!isLoaded ? styles.loading : ''}`}
      style={aspectRatioStyle}
    >
      {/* Loading skeleton */}
      {!isLoaded && !error && <div className={styles.skeleton} />}

      {/* Error state */}
      {error && (
        <div className={styles.error}>
          <p>Failed to load image</p>
        </div>
      )}

      {/* Picture element with multiple format support */}
      <picture className={`${styles.picture} ${className}`}>
        {/* AVIF: Modern format with best compression */}
        <source
          srcSet={src.replace(/\.\w+$/, '.avif')}
          type="image/avif"
        />

        {/* WebP: Good compression, wide browser support */}
        <source
          srcSet={src.replace(/\.\w+$/, '.webp')}
          type="image/webp"
        />

        {/* Fallback to original image format */}
        <img
          ref={imgRef}
          src={loading === 'eager' ? src : undefined}
          alt={alt}
          width={width}
          height={height}
          onLoad={handleImageLoad}
          onError={handleImageError}
          className={`${styles.image} ${isLoaded ? styles.loaded : ''}`}
          loading={loading === 'lazy' ? 'lazy' : 'eager'}
        />
      </picture>
    </div>
  );
}
