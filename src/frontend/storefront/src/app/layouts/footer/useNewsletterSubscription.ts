import type { FormEvent } from 'react';
import { useState } from 'react';
import { useToast } from '@/app/Toast';

const NEWSLETTER_SUBSCRIBERS_KEY = 'newsletter_subscribers';

interface UseNewsletterSubscriptionParams {
  invalidEmailMessage: string;
  subscribeSuccessMessage: string;
  alreadySubscribedMessage: string;
  subscribeFailedMessage: string;
}

export function useNewsletterSubscription({
  invalidEmailMessage,
  subscribeSuccessMessage,
  alreadySubscribedMessage,
  subscribeFailedMessage,
}: UseNewsletterSubscriptionParams) {
  const [email, setEmail] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { success, error } = useToast();

  const handleNewsletterSubmit = async (event: FormEvent) => {
    event.preventDefault();

    if (!email || !email.includes('@')) {
      error(invalidEmailMessage);
      return;
    }

    setIsSubmitting(true);

    try {
      const subscribers = JSON.parse(localStorage.getItem(NEWSLETTER_SUBSCRIBERS_KEY) || '[]');
      if (!subscribers.includes(email)) {
        subscribers.push(email);
        localStorage.setItem(NEWSLETTER_SUBSCRIBERS_KEY, JSON.stringify(subscribers));
        success(subscribeSuccessMessage);
      } else {
        error(alreadySubscribedMessage);
      }

      setEmail('');
    } catch {
      error(subscribeFailedMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    email,
    isSubmitting,
    setEmail,
    handleNewsletterSubmit,
  };
}
