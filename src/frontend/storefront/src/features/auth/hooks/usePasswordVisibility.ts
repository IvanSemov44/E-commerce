import { useState } from 'react';
import { useTranslation } from 'react-i18next';

export function usePasswordVisibility() {
  const { t } = useTranslation();
  const [show, setShow] = useState(false);

  return {
    show,
    toggle: () => setShow((v) => !v),
    inputType: show ? ('text' as const) : ('password' as const),
    ariaLabel: show ? t('auth.hidePassword') : t('auth.showPassword'),
  };
}
