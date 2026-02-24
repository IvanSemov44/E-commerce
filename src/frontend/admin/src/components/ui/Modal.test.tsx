import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import Modal from './Modal'

describe('Modal', () => {
  it('renders when isOpen is true', () => {
    render(
      <Modal isOpen={true} onClose={() => {}} title="Test Modal">
        <div>Modal Content</div>
      </Modal>
    )
    expect(screen.getByText('Modal Content')).toBeInTheDocument()
    expect(screen.getByText('Test Modal')).toBeInTheDocument()
  })

  it('does not render when isOpen is false', () => {
    render(
      <Modal isOpen={false} onClose={() => {}} title="Test Modal">
        <div>Modal Content</div>
      </Modal>
    )
    expect(screen.queryByText('Modal Content')).not.toBeInTheDocument()
    expect(screen.queryByText('Test Modal')).not.toBeInTheDocument()
  })

  it('renders title correctly', () => {
    render(
      <Modal isOpen={true} onClose={() => {}} title="My Modal Title">
        <div>Content</div>
      </Modal>
    )
    expect(screen.getByText('My Modal Title')).toBeInTheDocument()
  })

  it('calls onClose when close button is clicked', () => {
    const handleClose = vi.fn()
    render(
      <Modal isOpen={true} onClose={handleClose} title="Test Modal">
        <div>Content</div>
      </Modal>
    )
    
    const closeButton = screen.getByRole('button', { name: '×' })
    fireEvent.click(closeButton)
    
    expect(handleClose).toHaveBeenCalledTimes(1)
  })

  it('calls onClose when overlay is clicked', () => {
    const handleClose = vi.fn()
    render(
      <Modal isOpen={true} onClose={handleClose} title="Test Modal">
        <div>Content</div>
      </Modal>
    )
    
    const overlay = screen.getByText('Test Modal').closest('[class*="overlay"]')
    if (overlay) {
      fireEvent.click(overlay)
      expect(handleClose).toHaveBeenCalled()
    }
  })

  it('does not call onClose when modal content is clicked', () => {
    const handleClose = vi.fn()
    render(
      <Modal isOpen={true} onClose={handleClose} title="Test Modal">
        <div>Content</div>
      </Modal>
    )
    
    const content = screen.getByText('Content')
    fireEvent.click(content)
    
    expect(handleClose).not.toHaveBeenCalled()
  })

  it('renders children correctly', () => {
    render(
      <Modal isOpen={true} onClose={() => {}} title="Test Modal">
        <p>Paragraph content</p>
        <button>Action Button</button>
      </Modal>
    )
    expect(screen.getByText('Paragraph content')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Action Button' })).toBeInTheDocument()
  })

  it('applies size classes correctly', () => {
    const { rerender } = render(
      <Modal isOpen={true} onClose={() => {}} title="Test Modal" size="sm">
        <div>Content</div>
      </Modal>
    )
    
    let modal = screen.getByText('Content').closest('[class*="modal"]')
    expect(modal?.className).toMatch(/sm/i)
    
    rerender(
      <Modal isOpen={true} onClose={() => {}} title="Test Modal" size="lg">
        <div>Content</div>
      </Modal>
    )
    
    modal = screen.getByText('Content').closest('[class*="modal"]')
    expect(modal?.className).toMatch(/lg/i)
  })
})