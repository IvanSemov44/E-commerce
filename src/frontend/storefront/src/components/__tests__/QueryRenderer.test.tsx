import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import QueryRenderer from '../QueryRenderer'

describe('QueryRenderer', () => {
  describe('error state', () => {
    it('should render error alert when there is an error', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={{ message: 'Something went wrong' }}
          data={undefined}
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('Failed to load data. Please try again.')).toBeInTheDocument()
    })

    it('should render custom error message', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={{ message: 'Error' }}
          data={undefined}
          errorMessage="Custom error message"
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('Custom error message')).toBeInTheDocument()
    })
  })

  describe('loading state', () => {
    it('should render loading skeleton when loading', () => {
      render(
        <QueryRenderer
          isLoading={true}
          error={null}
          data={undefined}
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      // Loading skeleton should be present
      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBeGreaterThan(0)
    })

    it('should render custom loading skeleton', () => {
      render(
        <QueryRenderer
          isLoading={true}
          error={null}
          data={undefined}
          loadingSkeleton={{ custom: <div>Custom Loading...</div> }}
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('Custom Loading...')).toBeInTheDocument()
    })

    it('should not render children when loading', () => {
      render(
        <QueryRenderer
          isLoading={true}
          error={null}
          data="test data"
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.queryByText('test data')).not.toBeInTheDocument()
    })
  })

  describe('empty state', () => {
    it('should render empty state when data is undefined', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={undefined}
          emptyState={{ title: 'No items found' }}
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('No items found')).toBeInTheDocument()
    })

    it('should render empty state when data is null', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={null}
          emptyState={{ title: 'No items found' }}
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('No items found')).toBeInTheDocument()
    })

    it('should render empty state when data is empty array', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={[]}
          emptyState={{ title: 'No items found' }}
        >
          {(data) => <div>{data.join(',')}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('No items found')).toBeInTheDocument()
    })

    it('should render empty state with description', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={undefined}
          emptyState={{ title: 'No items', description: 'Try adjusting your filters' }}
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('No items')).toBeInTheDocument()
      expect(screen.getByText('Try adjusting your filters')).toBeInTheDocument()
    })

    it('should render empty state with action', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={undefined}
          emptyState={{ title: 'No items', action: <button>Add Item</button> }}
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByRole('button', { name: 'Add Item' })).toBeInTheDocument()
    })

    it('should render empty state with icon', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={undefined}
          emptyState={{ title: 'No items', icon: <span>🔍</span> }}
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('🔍')).toBeInTheDocument()
    })

    it('should return null when no emptyState is provided', () => {
      const { container } = render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={undefined}
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(container.firstChild).toBeNull()
    })
  })

  describe('success state', () => {
    it('should render children with data when data exists', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data="Hello World"
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('Hello World')).toBeInTheDocument()
    })

    it('should render children with array data', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={['Item 1', 'Item 2', 'Item 3']}
        >
          {(data) => <div>{data.map((item) => <span key={item}>{item}</span>)}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('Item 1')).toBeInTheDocument()
      expect(screen.getByText('Item 2')).toBeInTheDocument()
      expect(screen.getByText('Item 3')).toBeInTheDocument()
    })

    it('should render children with object data', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={{ name: 'Test', value: 42 }}
        >
          {(data) => <div>{data.name}: {data.value}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('Test: 42')).toBeInTheDocument()
    })
  })

  describe('custom isEmpty function', () => {
    it('should use custom isEmpty function', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={{ items: [] }}
          isEmpty={(data) => data.items.length === 0}
          emptyState={{ title: 'No items in list' }}
        >
          {(data) => <div>{data.items.length} items</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('No items in list')).toBeInTheDocument()
    })

    it('should render data when custom isEmpty returns false', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={null}
          data={{ items: ['a', 'b'] }}
          isEmpty={(data) => data.items.length === 0}
        >
          {(data) => <div>{data.items.length} items</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('2 items')).toBeInTheDocument()
    })
  })

  describe('priority', () => {
    it('should show error over loading state', () => {
      render(
        <QueryRenderer
          isLoading={true}
          error={{ message: 'Error' }}
          data={undefined}
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('Failed to load data. Please try again.')).toBeInTheDocument()
    })

    it('should show error over data', () => {
      render(
        <QueryRenderer
          isLoading={false}
          error={{ message: 'Error' }}
          data="some data"
        >
          {(data) => <div>{data}</div>}
        </QueryRenderer>
      )

      expect(screen.getByText('Failed to load data. Please try again.')).toBeInTheDocument()
    })
  })
})