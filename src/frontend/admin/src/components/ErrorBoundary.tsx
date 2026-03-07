/**
 * Error Boundary Component - Admin Panel
 * Catches and displays component rendering errors
 */

import React from 'react';
import ErrorPage from '../pages/ErrorPage';
import { logger } from '../utils/logger';

interface Props {
  children: React.ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export default class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    logger.error('ErrorBoundary', 'Unhandled render error', error, errorInfo);
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError) {
      return (
        <ErrorPage
          error={this.state.error}
          isDevelopment={
            import.meta.env.DEV || import.meta.env.MODE === 'development'
          }
          onReset={this.handleReset}
        />
      );
    }

    return this.props.children;
  }
}
