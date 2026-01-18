import Card from './ui/Card';

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
          <button
            onClick={onDismiss}
            aria-label="Dismiss error"
          >
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>
    </Card>
  );
}
