import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ProductImageGallery from './ProductImageGallery';
import type { ProductImage } from '@/shared/types';

describe('ProductImageGallery', () => {
  const mockImages: ProductImage[] = [
    { id: '1', url: 'https://example.com/image1.jpg', altText: 'Front view', isPrimary: true },
    { id: '2', url: 'https://example.com/image2.jpg', altText: 'Side view', isPrimary: false },
    { id: '3', url: 'https://example.com/image3.jpg', altText: 'Top view', isPrimary: false },
  ];

  it('should render main image with product name as alt text', () => {
    render(<ProductImageGallery images={mockImages} productName="Wireless Headphones" />);
    const mainImage = screen.getByAltText('Wireless Headphones');
    expect(mainImage).toBeInTheDocument();
    expect(mainImage).toHaveAttribute('src', mockImages[0].url);
  });

  it('should render thumbnail grid when multiple images exist', () => {
    render(<ProductImageGallery images={mockImages} productName="Product" />);
    const thumbnails = screen.getAllByRole('button');
    expect(thumbnails).toHaveLength(3);
  });

  it('should not render thumbnail grid with single image', () => {
    const singleImage = [mockImages[0]];
    render(<ProductImageGallery images={singleImage} productName="Product" />);
    const buttons = screen.queryAllByRole('button');
    expect(buttons).toHaveLength(0);
  });

  it('should update main image when thumbnail clicked', () => {
    render(<ProductImageGallery images={mockImages} productName="Product" />);
    const thumbnailButtons = screen.getAllByRole('button');

    fireEvent.click(thumbnailButtons[1]);

    const mainImage = screen.getByAltText('Product') as HTMLImageElement;
    expect(mainImage.src).toBe(mockImages[1].url);
  });

  it('should highlight active thumbnail', () => {
    render(<ProductImageGallery images={mockImages} productName="Product" />);
    const thumbnailButtons = screen.getAllByRole('button');

    // First thumbnail should have thumbnailActive class by default (CSS module scoped)
    expect(thumbnailButtons[0].className).toContain('thumbnailActive');

    // Click second thumbnail
    fireEvent.click(thumbnailButtons[1]);

    // Second should be active now
    expect(thumbnailButtons[1].className).toContain('thumbnailActive');
    expect(thumbnailButtons[0].className).not.toContain('thumbnailActive');
  });

  it('should render thumbnail images with correct alt text', () => {
    render(<ProductImageGallery images={mockImages} productName="Product" />);

    expect(screen.getByAltText('Front view')).toBeInTheDocument();
    expect(screen.getByAltText('Side view')).toBeInTheDocument();
    expect(screen.getByAltText('Top view')).toBeInTheDocument();
  });

  it('should use fallback text for thumbnail alt when altText is missing', () => {
    const imagesWithoutAlt: ProductImage[] = [
      { id: '1', url: 'https://example.com/image1.jpg', isPrimary: true },
      { id: '2', url: 'https://example.com/image2.jpg', isPrimary: false },
    ];

    render(<ProductImageGallery images={imagesWithoutAlt} productName="Product" />);

    expect(screen.getByAltText('Product image 1')).toBeInTheDocument();
    expect(screen.getByAltText('Product image 2')).toBeInTheDocument();
  });

  it('should have correct aria labels on thumbnail buttons', () => {
    render(<ProductImageGallery images={mockImages} productName="Product" />);

    const thumbnailButtons = screen.getAllByRole('button');
    expect(thumbnailButtons[0]).toHaveAttribute('aria-label', 'View product image 1');
    expect(thumbnailButtons[1]).toHaveAttribute('aria-label', 'View product image 2');
    expect(thumbnailButtons[2]).toHaveAttribute('aria-label', 'View product image 3');
  });

  it('should cycle through all images when thumbnails clicked in sequence', () => {
    render(<ProductImageGallery images={mockImages} productName="Product" />);
    const thumbnailButtons = screen.getAllByRole('button');
    const mainImage = screen.getByAltText('Product') as HTMLImageElement;

    // Click third image
    fireEvent.click(thumbnailButtons[2]);
    expect(mainImage.src).toBe(mockImages[2].url);

    // Click first image
    fireEvent.click(thumbnailButtons[0]);
    expect(mainImage.src).toBe(mockImages[0].url);
  });
});
