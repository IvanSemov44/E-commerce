import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import PaginatedView from './PaginatedView';

const paginationPropsSpy = vi.fn();

vi.mock('../ui/Pagination', () => ({
  default: (props: {
    currentPage: number;
    totalCount: number;
    pageSize: number;
    onPageChange: (page: number) => void;
    onPageSizeChange?: (pageSize: number) => void;
  }) => {
    paginationPropsSpy(props);
    return (
      <div>
        <button onClick={() => props.onPageChange(2)}>Go page 2</button>
        <button onClick={() => props.onPageSizeChange?.(24)}>Set size 24</button>
      </div>
    );
  },
}));

describe('PaginatedView', () => {
  it('renders items via renderItem callback', () => {
    render(
      <PaginatedView
        items={[{ id: '1', name: 'Phone' }, { id: '2', name: 'Laptop' }]}
        totalCount={2}
        currentPage={1}
        pageSize={12}
        onPageChange={vi.fn()}
        renderItem={(item) => <div key={item.id}>{item.name}</div>}
      />
    );

    expect(screen.getByText('Phone')).toBeInTheDocument();
    expect(screen.getByText('Laptop')).toBeInTheDocument();
  });

  it('passes pagination props and handlers through', () => {
    const onPageChange = vi.fn();
    const onPageSizeChange = vi.fn();

    render(
      <PaginatedView
        items={[{ id: '1', name: 'Phone' }]}
        totalCount={50}
        currentPage={3}
        pageSize={12}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
        renderItem={(item) => <div key={item.id}>{item.name}</div>}
        showPageSizeSelector
      />
    );

    expect(paginationPropsSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        currentPage: 3,
        totalCount: 50,
        pageSize: 12,
        showPageSizeSelector: true,
      })
    );

    fireEvent.click(screen.getByRole('button', { name: 'Go page 2' }));
    fireEvent.click(screen.getByRole('button', { name: 'Set size 24' }));

    expect(onPageChange).toHaveBeenCalledWith(2);
    expect(onPageSizeChange).toHaveBeenCalledWith(24);
  });

  it('applies custom grid class', () => {
    const { container } = render(
      <PaginatedView
        items={[{ id: '1', name: 'Phone' }]}
        totalCount={1}
        currentPage={1}
        pageSize={12}
        onPageChange={vi.fn()}
        renderItem={(item) => <div key={item.id}>{item.name}</div>}
        gridClassName="custom-grid"
      />
    );

    expect(container.querySelector('.custom-grid')).toBeInTheDocument();
  });
});
