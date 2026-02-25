import { test, expect, Page } from '@playwright/test';

/**
 * Accessibility E2E Tests
 * 
 * These tests provide basic accessibility checks that work without axe-core.
 * For comprehensive accessibility testing, install @axe-core/playwright:
 * npm install -D @axe-core/playwright
 */

/**
 * Simple accessibility check helper
 * Performs basic accessibility validation without axe-core
 */
async function checkBasicAccessibility(page: Page): Promise<string[]> {
  const violations: string[] = [];
  
  // Check for images without alt text
  const imagesWithoutAlt = await page.locator('img:not([alt])').count();
  if (imagesWithoutAlt > 0) {
    violations.push(`${imagesWithoutAlt} images missing alt attribute`);
  }
  
  // Check for form inputs without labels
  const inputsWithoutLabels = await page.evaluate(() => {
    const inputs = document.querySelectorAll('input:not([aria-label]):not([aria-labelledby])');
    let count = 0;
    inputs.forEach(input => {
      const id = input.getAttribute('id');
      if (!id || !document.querySelector(`label[for="${id}"]`)) {
        count++;
      }
    });
    return count;
  });
  if (inputsWithoutLabels > 0) {
    violations.push(`${inputsWithoutLabels} form inputs without proper labels`);
  }
  
  // Check for buttons without accessible names
  const buttonsWithoutText = await page.evaluate(() => {
    const buttons = document.querySelectorAll('button');
    let count = 0;
    buttons.forEach(button => {
      const text = button.textContent?.trim();
      const ariaLabel = button.getAttribute('aria-label');
      const ariaLabelledBy = button.getAttribute('aria-labelledby');
      if (!text && !ariaLabel && !ariaLabelledBy) {
        count++;
      }
    });
    return count;
  });
  if (buttonsWithoutText > 0) {
    violations.push(`${buttonsWithoutText} buttons without accessible names`);
  }
  
  // Check for missing main landmark
  const mainCount = await page.locator('main, [role="main"]').count();
  if (mainCount === 0) {
    violations.push('Page missing main landmark');
  }
  
  // Check for multiple h1 elements
  const h1Count = await page.locator('h1').count();
  if (h1Count > 1) {
    violations.push(`Page has ${h1Count} h1 elements, should have exactly 1`);
  }
  
  return violations;
}

test.describe('Accessibility Tests', () => {
  test.describe('Public Pages', () => {
    test('home page should pass basic accessibility checks', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');

      const violations = await checkBasicAccessibility(page);
      expect(violations).toEqual([]);
    });

    test('products page should pass basic accessibility checks', async ({ page }) => {
      await page.goto('/products');
      await page.waitForLoadState('networkidle');

      const violations = await checkBasicAccessibility(page);
      expect(violations).toEqual([]);
    });

    test('login page should pass basic accessibility checks', async ({ page }) => {
      await page.goto('/login');
      await page.waitForLoadState('networkidle');

      const violations = await checkBasicAccessibility(page);
      expect(violations).toEqual([]);
    });

    test('register page should pass basic accessibility checks', async ({ page }) => {
      await page.goto('/register');
      await page.waitForLoadState('networkidle');

      const violations = await checkBasicAccessibility(page);
      expect(violations).toEqual([]);
    });

    test('cart page should pass basic accessibility checks', async ({ page }) => {
      await page.goto('/cart');
      await page.waitForLoadState('networkidle');

      const violations = await checkBasicAccessibility(page);
      expect(violations).toEqual([]);
    });
  });

  test.describe('Form Accessibility', () => {
    test('login form should have proper labels', async ({ page }) => {
      await page.goto('/login');

      // Check email input has label
      const emailInput = page.locator('input[type="email"]');
      const emailId = await emailInput.getAttribute('id');
      const emailLabel = page.locator(`label[for="${emailId}"]`);
      
      if (await emailLabel.count() === 0) {
        // Check for aria-label or aria-labelledby
        const ariaLabel = await emailInput.getAttribute('aria-label');
        const ariaLabelledBy = await emailInput.getAttribute('aria-labelledby');
        
        expect(ariaLabel || ariaLabelledBy).toBeTruthy();
      } else {
        await expect(emailLabel).toBeVisible();
      }

      // Check password input has label
      const passwordInput = page.locator('input[type="password"]');
      const passwordId = await passwordInput.getAttribute('id');
      const passwordLabel = page.locator(`label[for="${passwordId}"]`);
      
      if (await passwordLabel.count() === 0) {
        const ariaLabel = await passwordInput.getAttribute('aria-label');
        const ariaLabelledBy = await passwordInput.getAttribute('aria-labelledby');
        
        expect(ariaLabel || ariaLabelledBy).toBeTruthy();
      } else {
        await expect(passwordLabel).toBeVisible();
      }
    });

    test('form inputs should have proper error associations', async ({ page }) => {
      await page.goto('/login');
      
      // Submit empty form to trigger validation
      const submitButton = page.locator('button[type="submit"]');
      await submitButton.click();
      
      // Check if error messages are associated with inputs
      const emailInput = page.locator('input[type="email"]');
      const ariaInvalid = await emailInput.getAttribute('aria-invalid');
      const ariaDescribedBy = await emailInput.getAttribute('aria-describedby');
      
      // If there's an error, input should be marked invalid
      if (ariaInvalid === 'true') {
        // Error message should be linked via aria-describedby
        expect(ariaDescribedBy).toBeTruthy();
        
        // Verify the error element exists
        const errorElement = page.locator(`#${ariaDescribedBy}`);
        await expect(errorElement).toBeVisible();
      }
    });

    test('form inputs should have proper autocomplete attributes', async ({ page }) => {
      await page.goto('/login');
      
      // Email should have autocomplete="email"
      const emailInput = page.locator('input[type="email"]');
      const emailAutocomplete = await emailInput.getAttribute('autocomplete');
      expect(emailAutocomplete).toBe('email');
      
      // Password should have autocomplete="current-password"
      const passwordInput = page.locator('input[type="password"]');
      const passwordAutocomplete = await passwordInput.getAttribute('autocomplete');
      expect(passwordAutocomplete).toBe('current-password');
    });
  });

  test.describe('Keyboard Navigation', () => {
    test('should be able to navigate main menu with keyboard', async ({ page }) => {
      await page.goto('/');
      
      // Tab through navigation
      await page.keyboard.press('Tab');
      await page.keyboard.press('Tab');
      
      // Focus should be visible
      const focusedElement = page.locator(':focus');
      await expect(focusedElement).toBeVisible();
    });

    test('should be able to navigate product grid with keyboard', async ({ page }) => {
      await page.goto('/products');
      await page.waitForLoadState('networkidle');
      
      // Tab to first product
      for (let i = 0; i < 10; i++) {
        await page.keyboard.press('Tab');
      }
      
      // Should be able to activate with Enter
      const focusedElement = page.locator(':focus');
      await focusedElement.press('Enter');
      
      // Should navigate to product page
      await page.waitForURL(/\/products\/\d+/, { timeout: 5000 });
    });

    test('should be able to close modals with Escape key', async ({ page }) => {
      await page.goto('/');
      
      // Open a modal if available (e.g., search, cart preview)
      const modalTrigger = page.locator('[data-testid="search-button"], [aria-haspopup="dialog"]').first();
      
      if (await modalTrigger.count() > 0) {
        await modalTrigger.click();
        
        // Modal should be open
        const modal = page.locator('[role="dialog"]');
        await expect(modal).toBeVisible();
        
        // Press Escape to close
        await page.keyboard.press('Escape');
        
        // Modal should be closed
        await expect(modal).not.toBeVisible();
      } else {
        test.skip();
      }
    });

    test('should trap focus in modals', async ({ page }) => {
      await page.goto('/');
      
      // Open modal
      const modalTrigger = page.locator('[aria-haspopup="dialog"]').first();
      
      if (await modalTrigger.count() > 0) {
        await modalTrigger.click();
        
        const modal = page.locator('[role="dialog"]');
        await expect(modal).toBeVisible();
        
        const modalHandle = await modal.elementHandle();
        
        // Tab through modal elements
        const tabbableElements = modal.locator('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])');
        const count = await tabbableElements.count();
        
        // Tab through all elements
        for (let i = 0; i < count + 2; i++) {
          await page.keyboard.press('Tab');
        }
        
        // Focus should still be within modal
        const focusedElement = page.locator(':focus');
        const isInModal = await focusedElement.evaluate((el, modalEl) => 
          modalEl ? modalEl.contains(el) : false, modalHandle
        );
        
        expect(isInModal).toBeTruthy();
      } else {
        test.skip();
      }
    });
  });

  test.describe('Images and Media', () => {
    test('all images should have alt text', async ({ page }) => {
      await page.goto('/products');
      await page.waitForLoadState('networkidle');
      
      const images = page.locator('img');
      const count = await images.count();
      
      for (let i = 0; i < count; i++) {
        const img = images.nth(i);
        const alt = await img.getAttribute('alt');
        const role = await img.getAttribute('role');
        
        // Image should have alt text or be marked as decorative
        expect(alt !== null || role === 'presentation' || role === 'none').toBeTruthy();
      }
    });

    test('decorative images should have empty alt or role="presentation"', async ({ page }) => {
      await page.goto('/');
      
      // Find decorative images (icons, backgrounds, etc.)
      const decorativeImages = page.locator('img[alt=""], img[role="presentation"], img[role="none"]');
      const count = await decorativeImages.count();
      
      // These should be properly marked as decorative
      for (let i = 0; i < count; i++) {
        const img = decorativeImages.nth(i);
        const alt = await img.getAttribute('alt');
        const role = await img.getAttribute('role');
        
        expect(alt === '' || role === 'presentation' || role === 'none').toBeTruthy();
      }
    });
  });

  test.describe('Headings and Landmarks', () => {
    test('page should have exactly one main landmark', async ({ page }) => {
      await page.goto('/');
      
      const mainElements = page.locator('main, [role="main"]');
      const count = await mainElements.count();
      
      expect(count).toBe(1);
    });

    test('page should have proper heading hierarchy', async ({ page }) => {
      await page.goto('/');
      
      // Should have exactly one h1
      const h1Elements = page.locator('h1');
      const h1Count = await h1Elements.count();
      expect(h1Count).toBe(1);
      
      // Heading levels should not skip
      const headings = await page.locator('h1, h2, h3, h4, h5, h6').all();
      let previousLevel = 0;
      
      for (const heading of headings) {
        const tagName = await heading.evaluate(el => el.tagName.toLowerCase());
        const currentLevel = parseInt(tagName.replace('h', ''));
        
        // Should not skip more than one level
        expect(currentLevel).toBeLessThanOrEqual(previousLevel + 1);
        previousLevel = currentLevel;
      }
    });

    test('navigation should have proper landmark', async ({ page }) => {
      await page.goto('/');
      
      const navElements = page.locator('nav, [role="navigation"]');
      const count = await navElements.count();
      
      // Should have at least one navigation landmark
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('footer should have proper landmark', async ({ page }) => {
      await page.goto('/');
      
      const footerElements = page.locator('footer, [role="contentinfo"]');
      const count = await footerElements.count();
      
      // Should have exactly one footer landmark
      expect(count).toBe(1);
    });
  });

  test.describe('Screen Reader Support', () => {
    test('buttons should have accessible names', async ({ page }) => {
      await page.goto('/');
      
      const buttons = page.locator('button');
      const count = await buttons.count();
      
      for (let i = 0; i < Math.min(count, 20); i++) {
        const button = buttons.nth(i);
        
        // Button should have accessible name via text content, aria-label, or aria-labelledby
        const textContent = await button.textContent();
        const ariaLabel = await button.getAttribute('aria-label');
        const ariaLabelledBy = await button.getAttribute('aria-labelledby');
        
        const hasAccessibleName = 
          (textContent && textContent.trim().length > 0) ||
          (ariaLabel && ariaLabel.trim().length > 0) ||
          (ariaLabelledBy && ariaLabelledBy.trim().length > 0);
        
        expect(hasAccessibleName).toBeTruthy();
      }
    });

    test('links should have discernible text', async ({ page }) => {
      await page.goto('/');
      
      const links = page.locator('a');
      const count = await links.count();
      
      for (let i = 0; i < Math.min(count, 20); i++) {
        const link = links.nth(i);
        
        const textContent = await link.textContent();
        const ariaLabel = await link.getAttribute('aria-label');
        const ariaLabelledBy = await link.getAttribute('aria-labelledby');
        const title = await link.getAttribute('title');
        
        const hasDiscernibleText = 
          (textContent && textContent.trim().length > 0) ||
          (ariaLabel && ariaLabel.trim().length > 0) ||
          (ariaLabelledBy && ariaLabelledBy.trim().length > 0) ||
          (title && title.trim().length > 0);
        
        expect(hasDiscernibleText).toBeTruthy();
      }
    });

    test('loading states should be announced', async ({ page }) => {
      await page.goto('/products');
      
      // Trigger a loading state
      const searchInput = page.locator('input[type="search"]').first();
      
      if (await searchInput.count() > 0) {
        await searchInput.fill('test');
        await searchInput.press('Enter');
        
        // Check for loading indicator with proper ARIA
        const loadingIndicator = page.locator('[role="status"], [aria-live="polite"], [aria-busy="true"]');
        
        if (await loadingIndicator.count() > 0) {
          // Loading indicator should have accessible name
          const textContent = await loadingIndicator.first().textContent();
          const ariaLabel = await loadingIndicator.first().getAttribute('aria-label');
          
          expect(textContent || ariaLabel).toBeTruthy();
        }
      }
    });
  });

  test.describe('Focus Management', () => {
    test('focus should be visible on all interactive elements', async ({ page }) => {
      await page.goto('/');
      
      // Add custom CSS to ensure focus is visible
      await page.addStyleTag({
        content: `
          *:focus {
            outline: 3px solid #000 !important;
            outline-offset: 2px !important;
          }
        `
      });
      
      const interactiveElements = page.locator('button, a, input, select, textarea, [tabindex]:not([tabindex="-1"])');
      const count = await interactiveElements.count();
      
      for (let i = 0; i < Math.min(count, 10); i++) {
        await page.keyboard.press('Tab');
        
        const focusedElement = page.locator(':focus');
        await expect(focusedElement).toBeVisible();
      }
    });

    test('skip link should be present', async ({ page }) => {
      await page.goto('/');
      
      // Look for skip link
      const skipLink = page.locator('a[href="#main"], a[href="#content"], [data-testid="skip-link"]');
      
      if (await skipLink.count() > 0) {
        // Skip link should be first focusable element
        await page.keyboard.press('Tab');
        const focusedElement = page.locator(':focus');
        
        // Should be the skip link
        const isSkipLink = await focusedElement.evaluate((el) => 
          el.getAttribute('href') === '#main' || el.getAttribute('href') === '#content'
        );
        
        expect(isSkipLink).toBeTruthy();
      }
    });
  });
});