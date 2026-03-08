import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Pagination from './Pagination';

describe('Pagination', () => {
  const defaultProps = {
    currentPage: 1,
    totalCount: 120,
    pageSize: 12,
    onPageChange: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders pagination component', () => {
    const { container } = render(<Pagination {...defaultProps} />);
    expect(container.querySelector('[class*="pagination"]')).toBeInTheDocument();
  });

  it('calculates total pages correctly', () => {
    render(<Pagination {...defaultProps} />);
    // With 120 items and 12 per page = 10 pages
    // Should show page numbers
    expect(screen.getByRole('button', { name: /go to page 2/i })).toBeInTheDocument();
  });

  it('disables previous button on first page', () => {
    render(<Pagination {...defaultProps} currentPage={1} />);
    const prevButton = screen.getByLabelText(/previous/i);
    expect(prevButton).toBeDisabled();
  });

  it('enables previous button on non-first page', () => {
    render(<Pagination {...defaultProps} currentPage={2} />);
    const prevButton = screen.getByLabelText(/previous/i);
    expect(prevButton).not.toBeDisabled();
  });

  it('disables next button on last page', () => {
    render(<Pagination {...defaultProps} currentPage={10} totalCount={120} pageSize={12} />);
    const nextButton = screen.getByLabelText(/next/i);
    expect(nextButton).toBeDisabled();
  });

  it('enables next button on non-last page', () => {
    render(<Pagination {...defaultProps} currentPage={1} />);
    const nextButton = screen.getByLabelText(/next/i);
    expect(nextButton).not.toBeDisabled();
  });

  it('calls onPageChange when page button is clicked', async () => {
    const handlePageChange = vi.fn();
    const user = userEvent.setup();
    render(<Pagination {...defaultProps} onPageChange={handlePageChange} currentPage={1} />);

    const page2Button = screen.getByRole('button', { name: /go to page 2/i });
    await user.click(page2Button);

    expect(handlePageChange).toHaveBeenCalledWith(2);
  });

  it('calls onPageChange when next button is clicked', async () => {
    const handlePageChange = vi.fn();
    const user = userEvent.setup();
    render(<Pagination {...defaultProps} onPageChange={handlePageChange} currentPage={1} />);

    const nextButton = screen.getByLabelText(/next/i);
    await user.click(nextButton);

    expect(handlePageChange).toHaveBeenCalledWith(2);
  });

  it('calls onPageChange when previous button is clicked', async () => {
    const handlePageChange = vi.fn();
    const user = userEvent.setup();
    render(<Pagination {...defaultProps} onPageChange={handlePageChange} currentPage={3} />);

    const prevButton = screen.getByLabelText(/previous/i);
    await user.click(prevButton);

    expect(handlePageChange).toHaveBeenCalledWith(2);
  });

  it('shows page size selector when showPageSizeSelector is true', () => {
    render(
      <Pagination
        {...defaultProps}
        showPageSizeSelector
        onPageSizeChange={vi.fn()}
        pageSizeOptions={[12, 24]}
      />
    );
    const select = screen.getByRole('combobox', { name: /items per page/i });
    expect(select).toBeInTheDocument();
  });

  it('calls onPageSizeChange when page size changes', async () => {
    const handlePageSizeChange = vi.fn();
    const user = userEvent.setup();
    render(
      <Pagination
        {...defaultProps}
        showPageSizeSelector
        pageSizeOptions={[12, 24, 48]}
        onPageSizeChange={handlePageSizeChange}
      />
    );

    const select = screen.getByRole('combobox', { name: /items per page/i });
    await user.selectOptions(select, '24');

    expect(handlePageSizeChange).toHaveBeenCalledWith(24);
  });

  it('hides first/last buttons when showFirstLast is false', () => {
    render(<Pagination {...defaultProps} showFirstLast={false} />);
    const firstButton = screen.queryByLabelText(/first page/i);
    const lastButton = screen.queryByLabelText(/last page/i);
    expect(firstButton).not.toBeInTheDocument();
    expect(lastButton).not.toBeInTheDocument();
  });

  it('shows first/last buttons by default', () => {
    render(<Pagination {...defaultProps} />);
    expect(screen.getByLabelText(/first page/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/last page/i)).toBeInTheDocument();
  });

  it('goes to first page when first button is clicked', async () => {
    const handlePageChange = vi.fn();
    const user = userEvent.setup();
    render(<Pagination {...defaultProps} currentPage={5} onPageChange={handlePageChange} />);

    const firstButton = screen.getByLabelText(/first page/i);
    await user.click(firstButton);

    expect(handlePageChange).toHaveBeenCalledWith(1);
  });

  it('goes to last page when last button is clicked', async () => {
    const handlePageChange = vi.fn();
    const user = userEvent.setup();
    render(<Pagination {...defaultProps} currentPage={1} onPageChange={handlePageChange} />);

    const lastButton = screen.getByLabelText(/last page/i);
    await user.click(lastButton);

    expect(handlePageChange).toHaveBeenCalledWith(10);
  });

  it('applies custom className', () => {
    const { container } = render(<Pagination {...defaultProps} className="custom-pagination" />);
    const pagination = container.querySelector('.custom-pagination');
    expect(pagination).toBeInTheDocument();
  });

  it('handles custom pageLabel function', () => {
    render(<Pagination {...defaultProps} pageLabel={(page) => `Navigate to page ${page}`} />);
    expect(screen.getByRole('button', { name: /navigate to page 2/i })).toBeInTheDocument();
  });

  it('shows ellipsis for large page ranges', () => {
    render(<Pagination {...defaultProps} currentPage={1} totalCount={500} pageSize={12} />);
    const ellipsis = screen.queryAllByText('…');
    expect(ellipsis.length).toBeGreaterThan(0);
  });

  it('highlights current page', () => {
    render(<Pagination {...defaultProps} currentPage={3} />);
    const currentPageButton = screen.getByRole('button', { name: /go to page 3/i });
    expect(currentPageButton).toHaveAttribute('aria-current', 'page');
  });

  it('displays total items count', () => {
    render(<Pagination {...defaultProps} />);
    expect(screen.getByText(/1-12 of 120/i)).toBeInTheDocument();
  });

  it('supports default page size options', () => {
    render(
      <Pagination
        {...defaultProps}
        showPageSizeSelector
        onPageSizeChange={vi.fn()}
        pageSizeOptions={[12, 24]}
      />
    );
    const select = screen.getByRole('combobox', { name: /items per page/i });
    expect(select).toBeInTheDocument();
  });
});
