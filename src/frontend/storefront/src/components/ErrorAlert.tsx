import Card from './ui/Card';

interface ErrorAlertProps {
  message: string;
  onDismiss?: () => void;
}

export default function ErrorAlert({ message, onDismiss }: ErrorAlertProps) {
  return (
    <Card variant="bordered" padding="md" className="error-container">
      <div className="flex items-center justify-between gap-4">
        <p className="error-text flex-1">{message}</p>
        {onDismiss && (
          <button
            onClick={onDismiss}
            className="text-rose-600 hover:text-rose-700 transition-colors flex-shrink-0"
            aria-label="Dismiss error"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>
    </Card>
  );
}
