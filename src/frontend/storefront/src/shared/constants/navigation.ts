export const ROUTE_PATHS = {
  home: '/',
  products: '/products',
  productDetail: '/products/:slug',
  cart: '/cart',
  checkout: '/checkout',
  wishlist: '/wishlist',
  orders: '/orders',
  orderDetail: '/orders/:orderId',
  profile: '/profile',
  login: '/login',
  register: '/register',
  forgotPassword: '/forgot-password',
  resetPassword: '/reset-password',
  privacy: '/privacy',
  terms: '/terms',
  returns: '/returns',
  cookies: '/cookies',
  security: '/security',
  about: '/about',
  careers: '/careers',
  press: '/press',
  blog: '/blog',
  help: '/help',
  contact: '/contact',
  trackOrder: '/track-order',
} as const;

export type RoutePath = (typeof ROUTE_PATHS)[keyof typeof ROUTE_PATHS];

export interface NavigationItem {
  labelKey: string;
  path: RoutePath;
  requiresAuth?: boolean;
}

export const HEADER_NAV_ITEMS: NavigationItem[] = [
  { labelKey: 'nav.products', path: ROUTE_PATHS.products },
  { labelKey: 'nav.orders', path: ROUTE_PATHS.orders, requiresAuth: true },
];

export type MobileNavIcon = 'products' | 'orders' | 'wishlist' | 'cart';
export type MobileNavBadge = 'wishlist' | 'cart';

export interface MobileNavigationItem extends NavigationItem {
  icon: MobileNavIcon;
  badge?: MobileNavBadge;
}

export const MOBILE_NAV_ITEMS: MobileNavigationItem[] = [
  { labelKey: 'nav.products', path: ROUTE_PATHS.products, icon: 'products' },
  { labelKey: 'nav.orders', path: ROUTE_PATHS.orders, icon: 'orders', requiresAuth: true },
  {
    labelKey: 'nav.wishlist',
    path: ROUTE_PATHS.wishlist,
    icon: 'wishlist',
    badge: 'wishlist',
    requiresAuth: true,
  },
  { labelKey: 'nav.cart', path: ROUTE_PATHS.cart, icon: 'cart', badge: 'cart' },
];

export interface FooterSection {
  title: string;
  links: NavigationItem[];
}

export const FOOTER_SECTIONS: FooterSection[] = [
  {
    title: 'footer.company',
    links: [
      { labelKey: 'footer.aboutUs', path: ROUTE_PATHS.about },
      { labelKey: 'footer.careers', path: ROUTE_PATHS.careers },
      { labelKey: 'footer.press', path: ROUTE_PATHS.press },
      { labelKey: 'footer.blog', path: ROUTE_PATHS.blog },
    ],
  },
  {
    title: 'footer.support',
    links: [
      { labelKey: 'footer.helpCenter', path: ROUTE_PATHS.help },
      { labelKey: 'footer.contactUs', path: ROUTE_PATHS.contact },
      { labelKey: 'footer.trackOrder', path: ROUTE_PATHS.trackOrder },
      { labelKey: 'footer.returns', path: ROUTE_PATHS.returns },
    ],
  },
  {
    title: 'Legal',
    links: [
      { labelKey: 'footer.privacyPolicy', path: ROUTE_PATHS.privacy },
      { labelKey: 'footer.termsOfService', path: ROUTE_PATHS.terms },
      { labelKey: 'footer.cookiePolicy', path: ROUTE_PATHS.cookies },
      { labelKey: 'footer.security', path: ROUTE_PATHS.security },
    ],
  },
];
