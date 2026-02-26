import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import PaginatedView from '../PaginatedView';

interface TestItem {
  id: string;
  name: string;
}

describe('PaginatedView', () => {
  const mockItems: TestItem[] = [
    { id: '1', name: 'Item 1' },
    { id: '2', name: 'Item 2' },
    { id: '3', name: 'Item 3' },
  ];

  const defaultProps = {
    items: mockItems,
    totalCount: 30,
    currentPage: 1,
    pageSize: 10,
    onPageChange: vi.fn(),
    renderItem: (item: TestItem) => <div key={item.id}>{item.name}</div>,
  };

  describe('Rendering', () => {
    it('renders all items using renderItem', () => {
      render(<PaginatedView {...defaultProps} />);

      expect(screen.getByText('Item 1')).toBeInTheDocument();
      expect(screen.getByText('Item 2')).toBeInTheDocument();
      expect(screen.getByText('Item 3')).toBeInTheDocument();
    });

    it('displays current page information', () => {
      render(<PaginatedView {...defaultProps} />);

      expect(screen.getByText('Page 1 of 3')).toBeInTheDocument();
    });

    it('calculates total pages correctly', () => {
      render(<PaginatedView {...defaultProps} totalCount={25} pageSize={10} />);

      expect(screen.getByText('Page 1 of 3')).toBeInTheDocument();
    });

    it('handles single page correctly', () => {
      render(
        <PaginatedView {...defaultProps} totalCount={5} pageSize={10} />
      );

      expect(screen.getByText('Page 1 of 1')).toBeInTheDocument();
    });

    it('applies gridClassName to grid container', () => {
      const { container } = render(
        <PaginatedView {...defaultProps} gridClassName="custom-grid" />
      );

      expect(container.querySelector('.custom-grid')).toBeInTheDocument();
    });
  });

  describe('Navigation Buttons', () => {
    it('renders Previous and Next buttons', () => {
      render(<PaginatedView {...defaultProps} />);

      expect(screen.getByRole('button', { name: 'Previous' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Next' })).toBeInTheDocument();
    });

    it('disables Previous button on first page', () => {
      render(<PaginatedView {...defaultProps} currentPage={1} />);

      const prevButton = screen.getByRole('button', { name: 'Previous' });
      expect(prevButton).toBeDisabled();
    });

    it('enables Previous button when not on first page', () => {
      render(<PaginatedView {...defaultProps} currentPage={2} />);

      const prevButton = screen.getByRole('button', { name: 'Previous' });
      expect(prevButton).not.toBeDisabled();
    });

    it('disables Next button on last page', () => {
      render(<PaginatedView {...defaultProps} currentPage={3} />);

      const nextButton = screen.getByRole('button', { name: 'Next' });
      expect(nextButton).toBeDisabled();
    });

    it('enables Next button when not on last page', () => {
      render(<PaginatedView {...defaultProps} currentPage={1} />);

      const nextButton = screen.getByRole('button', { name: 'Next' });
      expect(nextButton).not.toBeDisabled();
    });
  });

  describe('Page Navigation', () => {
    it('calls onPageChange with previous page when Previous is clicked', () => {
      const onPageChange = vi.fn();
      render(<PaginatedView {...defaultProps} currentPage={2} onPageChange={onPageChange} />);

      const prevButton = screen.getByRole('button', { name: 'Previous' });
      fireEvent.click(prevButton);

      expect(onPageChange).toHaveBeenCalledWith(1);
    });

    it('calls onPageChange with next page when Next is clicked', () => {
      const onPageChange = vi.fn();
      render(<PaginatedView {...defaultProps} currentPage={1} onPageChange={onPageChange} />);

      const nextButton = screen.getByRole('button', { name: 'Next' });
      fireEvent.click(nextButton);

      expect(onPageChange).toHaveBeenCalledWith(2);
    });

    it('does not call onPageChange when Previous is disabled and clicked', () => {
      const onPageChange = vi.fn();
      render(<PaginatedView {...defaultProps} currentPage={1} onPageChange={onPageChange} />);

      const prevButton = screen.getByRole('button', { name: 'Previous' });
      fireEvent.click(prevButton);

      expect(onPageChange).not.toHaveBeenCalled();
    });

    it('does not call onPageChange when Next is disabled and clicked', () => {
      const onPageChange = vi.fn();
      render(<PaginatedView {...defaultProps} currentPage={3} onPageChange={onPageChange} />);

      const nextButton = screen.getByRole('button', { name: 'Next' });
      fireEvent.click(nextButton);

      expect(onPageChange).not.toHaveBeenCalled();
    });
  });

  describe('Edge Cases', () => {
    it('handles empty items array', () => {
      render(<PaginatedView {...defaultProps} items={[]} totalCount={0} />);

      expect(screen.getByText('Page 1 of 0')).toBeInTheDocument();
    });

    it('handles exact page boundary', () => {
      render(<PaginatedView {...defaultProps} totalCount={20} pageSize={10} currentPage={2} />);

      expect(screen.getByText('Page 2 of 2')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
    });

    it('handles middle page correctly', () => {
      render(<PaginatedView {...defaultProps} totalCount={30} pageSize={10} currentPage={2} />);

      expect(screen.getByText('Page 2 of 3')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Previous' })).not.toBeDisabled();
      expect(screen.getByRole('button', { name: 'Next' })).not.toBeDisabled();
    });
  });
});
