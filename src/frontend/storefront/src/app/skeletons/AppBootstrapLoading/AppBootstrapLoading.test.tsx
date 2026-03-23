import { describe, it } from 'vitest';
import { render } from '@testing-library/react';
import { AppBootstrapLoading } from './AppBootstrapLoading';

describe('AppBootstrapLoading', () => {
  it('renders without crashing', () => {
    render(<AppBootstrapLoading />);
  });
});
