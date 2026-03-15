import { Card } from '../ui/Card';
import { CloseIcon } from '../icons';

interface ErrorAlertProps {
  message: string;
  onDismiss?: () => void;
}

export default function ErrorAlert({ message, onDismiss }: ErrorAlertProps) {
  return (
    <Card variant="bordered" padding="md" className="error-container">
      <div>
        <p className="error-text">{message}</p>
        {onDismiss && (
          <button onClick={onDismiss} aria-label="Dismiss error">
            <CloseIcon width={20} height={20} />
          </button>
        )}
      </div>
    </Card>
  );
}
