/**
 * Validate email format
 * @param email - Email to validate
 * @returns True if email is valid
 */
export function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
}

/**
 * Get authentication banner message
 * @param isAuthenticated - User authentication status
 * @returns Descriptive message
 */
export function getAuthBannerMessage(isAuthenticated: boolean): string {
  if (isAuthenticated) {
    return 'You are logged in';
  }
  return 'Sign in or continue as guest';
}

/**
 * Get login button label
 * @returns Button label
 */
export function getLoginButtonLabel(): string {
  return 'Sign In';
}

/**
 * Get signup button label
 * @returns Button label
 */
export function getSignupButtonLabel(): string {
  return 'Create Account';
}

/**
 * Get guest email placeholder
 * @returns Placeholder text
 */
export function getGuestEmailPlaceholder(): string {
  return 'Enter your email to continue';
}
