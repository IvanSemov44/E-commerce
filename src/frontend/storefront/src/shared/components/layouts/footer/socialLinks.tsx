import type { ReactNode } from 'react';
import {
  FacebookIcon,
  TwitterIcon,
  InstagramIcon,
  LinkedInIcon,
  YouTubeIcon,
} from '@/shared/components/icons';

export interface FooterSocialLink {
  icon: ReactNode;
  href: string;
  label: string;
}

export const FOOTER_SOCIAL_LINKS: FooterSocialLink[] = [
  { icon: <FacebookIcon />, href: '#', label: 'Facebook' },
  { icon: <TwitterIcon />, href: '#', label: 'Twitter' },
  { icon: <InstagramIcon />, href: '#', label: 'Instagram' },
  { icon: <LinkedInIcon />, href: '#', label: 'LinkedIn' },
  { icon: <YouTubeIcon />, href: '#', label: 'YouTube' },
];
