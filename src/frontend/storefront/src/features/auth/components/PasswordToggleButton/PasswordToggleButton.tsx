import { EyeIcon, EyeOffIcon } from '@/shared/components/icons';
import styles from './PasswordToggleButton.module.css';

interface Props {
  show: boolean;
  ariaLabel: string;
  onClick: () => void;
}

export function PasswordToggleButton({ show, ariaLabel, onClick }: Props) {
  return (
    <button type="button" className={styles.button} aria-label={ariaLabel} onClick={onClick}>
      {show ? <EyeOffIcon /> : <EyeIcon />}
    </button>
  );
}
