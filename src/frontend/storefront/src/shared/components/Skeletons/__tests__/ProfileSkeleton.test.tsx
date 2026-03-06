import { describe, it, expect } from 'vitest'
import { render } from '@testing-library/react'
import ProfileSkeleton from '../ProfileSkeleton'

describe('ProfileSkeleton', () => {
  it('renders profile container', () => {
    const { container } = render(<ProfileSkeleton />)
    const profileContainer = container.querySelector('[class*="profileContainer"]')
    expect(profileContainer).toBeInTheDocument()
  })

  it('renders profile header section', () => {
    const { container } = render(<ProfileSkeleton />)
    const profileHeader = container.querySelector('[class*="profileHeader"]')
    expect(profileHeader).toBeInTheDocument()
  })

  it('renders header info section', () => {
    const { container } = render(<ProfileSkeleton />)
    const headerInfo = container.querySelector('[class*="headerInfo"]')
    expect(headerInfo).toBeInTheDocument()
  })

  it('renders profile form section', () => {
    const { container } = render(<ProfileSkeleton />)
    const profileForm = container.querySelector('[class*="profileForm"]')
    expect(profileForm).toBeInTheDocument()
  })

  it('renders multiple form groups', () => {
    const { container } = render(<ProfileSkeleton />)
    const formGroups = container.querySelectorAll('[class*="formGroup"]')
    // Should have form groups for: first name, last name, email, phone
    expect(formGroups.length).toBeGreaterThanOrEqual(4)
  })

  it('renders circle variant for avatar', () => {
    const { container } = render(<ProfileSkeleton />)
    const skeletons = container.querySelectorAll('span[class*="skeleton"]')
    // First skeleton should be the avatar (circle variant)
    const firstSkeleton = skeletons[0]
    expect(firstSkeleton?.className).toMatch(/circle/)
  })

  it('renders submit button skeleton with margin top', () => {
    const { container } = render(<ProfileSkeleton />)
    const marginTopElements = container.querySelectorAll('[class*="marginTop"]')
    expect(marginTopElements.length).toBeGreaterThan(0)
  })

  it('renders multiple skeleton elements', () => {
    const { container } = render(<ProfileSkeleton />)
    const skeletons = container.querySelectorAll('span[class*="skeleton"]')
    // Should have: avatar + header name/email + 4 form groups (label+input each) + button
    expect(skeletons.length).toBeGreaterThan(10)
  })

  it('all skeletons have proper aria attributes', () => {
    const { container } = render(<ProfileSkeleton />)
    const skeletons = container.querySelectorAll('span[class*="skeleton"]')
    skeletons.forEach((skeleton) => {
      expect(skeleton).toHaveAttribute('aria-busy', 'true')
      expect(skeleton).toHaveAttribute('aria-label', 'Loading')
    })
  })
})
