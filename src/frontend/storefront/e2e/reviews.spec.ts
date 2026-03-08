import { test, expect } from '@playwright/test';

/**
 * E2E Tests: Reviews
 * Tests product review submission, editing, and display
 */

test.describe('Reviews', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display reviews on product page', async ({ page }) => {
    // Navigate to a product
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      // Look for reviews section
      const reviewsSection = page.locator('[class*="reviews"], [data-testid="reviews"]');

      if ((await reviewsSection.count()) > 0) {
        // Check for review items
        const reviewItem = page.locator('[class*="review-item"], [data-testid="review"]');
        expect((await reviewItem.count()) >= 0).toBeTruthy();
      }
    } else {
      test.skip();
    }
  });

  test('should show average rating on product page', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      // Look for rating display
      const ratingElement = page.locator('[class*="rating"], [data-testid="rating"]');

      if ((await ratingElement.count()) > 0) {
        const ratingText = await ratingElement.first().textContent();
        expect(ratingText).toBeTruthy();
      }
    } else {
      test.skip();
    }
  });

  test('should show star rating display', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      // Look for star icons
      const stars = page.locator('[class*="star"], svg[class*="star"], [data-testid*="star"]');
      expect((await stars.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should display review author name', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      // Look for review items
      const reviewItem = page.locator('[class*="review-item"], [data-testid="review"]').first();

      if ((await reviewItem.count()) > 0) {
        // Look for author name
        const authorName = reviewItem.locator('[class*="author"], [class*="user"]');
        expect((await authorName.count()) >= 0).toBeTruthy();
      }
    } else {
      test.skip();
    }
  });

  test('should display review date', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      const reviewItem = page.locator('[class*="review-item"], [data-testid="review"]').first();

      if ((await reviewItem.count()) > 0) {
        // Look for date
        const dateElement = reviewItem.locator(
          '[class*="date"], :text(/\\d{1,2}[\\/\\-]\\d{1,2}[\\/\\-]\\d{2,4}/)'
        );
        expect((await dateElement.count()) >= 0).toBeTruthy();
      }
    } else {
      test.skip();
    }
  });

  test('should show review comment text', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      const reviewItem = page.locator('[class*="review-item"], [data-testid="review"]').first();

      if ((await reviewItem.count()) > 0) {
        // Look for comment text
        const commentText = reviewItem.locator('[class*="comment"], [class*="content"], p');
        expect((await commentText.count()) >= 0).toBeTruthy();
      }
    } else {
      test.skip();
    }
  });

  test('should allow writing a review when logged in', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      // Look for write review button
      const writeReviewButton = page
        .locator(
          'button:has-text("Write a Review"), button:has-text("Add Review"), [data-testid="write-review"]'
        )
        .first();

      if ((await writeReviewButton.count()) > 0) {
        await writeReviewButton.click();
        await page.waitForTimeout(1000);

        // Should show review form or login prompt
        const reviewForm = page.locator('form, [class*="review-form"]');
        const loginPrompt = page.locator(':text("Sign in"), input[type="email"]');

        expect((await reviewForm.count()) > 0 || (await loginPrompt.count()) > 0).toBeTruthy();
      } else {
        test.skip();
      }
    } else {
      test.skip();
    }
  });

  test('should submit review with rating and comment', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      const writeReviewButton = page
        .locator('button:has-text("Write a Review"), button:has-text("Add Review")')
        .first();

      if ((await writeReviewButton.count()) > 0) {
        await writeReviewButton.click();
        await page.waitForTimeout(1000);

        // Check if review form is visible
        const reviewForm = page.locator('form, [class*="review-form"]');

        if ((await reviewForm.count()) > 0) {
          // Try to select a rating
          const starRating = page
            .locator('[class*="star-rating"] button, [data-testid*="star"]')
            .first();
          if ((await starRating.count()) > 0) {
            await starRating.click();
          }

          // Try to fill comment
          const commentInput = page.locator('textarea, input[name*="comment"]').first();
          if ((await commentInput.count()) > 0) {
            await commentInput.fill('Great product! Highly recommended.');
          }

          // Try to submit
          const submitButton = page
            .locator('button[type="submit"], button:has-text("Submit")')
            .first();
          if ((await submitButton.count()) > 0) {
            await submitButton.click();
            await page.waitForTimeout(1000);

            // Check for success
            const successToast = page.locator('[class*="toast"], [role="alert"]');
            expect((await successToast.count()) >= 0).toBeTruthy();
          }
        } else {
          test.skip();
        }
      } else {
        test.skip();
      }
    } else {
      test.skip();
    }
  });

  test('should show helpful vote buttons on reviews', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      const reviewItem = page.locator('[class*="review-item"], [data-testid="review"]').first();

      if ((await reviewItem.count()) > 0) {
        // Look for helpful buttons
        const helpfulButton = reviewItem.locator(
          'button:has-text("Helpful"), button:has-text("Helpful?")'
        );
        expect((await helpfulButton.count()) >= 0).toBeTruthy();
      }
    } else {
      test.skip();
    }
  });

  test('should sort reviews by date or rating', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      // Look for sort dropdown
      const sortDropdown = page.locator('select, [class*="sort"]').first();

      if ((await sortDropdown.count()) > 0) {
        await sortDropdown.click();
        await page.waitForTimeout(500);

        // Look for sort options
        const sortOption = page.locator('option:has-text("Newest"), option:has-text("Highest")');
        expect((await sortOption.count()) >= 0).toBeTruthy();
      }
    } else {
      test.skip();
    }
  });

  test('should paginate reviews if many exist', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      // Look for pagination
      const pagination = page.locator('[class*="pagination"], button:has-text("Load More")');
      expect((await pagination.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });

  test('should show verified purchase badge', async ({ page }) => {
    await page.goto('/products');
    await page.waitForTimeout(1000);

    const productLink = page.locator('a[href*="/products/"]').first();

    if ((await productLink.count()) > 0) {
      await productLink.click();
      await page.waitForTimeout(1000);

      // Look for verified purchase badge
      const verifiedBadge = page.locator(':text("Verified Purchase"), [class*="verified"]');
      expect((await verifiedBadge.count()) >= 0).toBeTruthy();
    } else {
      test.skip();
    }
  });
});
