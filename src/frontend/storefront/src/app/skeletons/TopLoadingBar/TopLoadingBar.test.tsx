import { describe, it } from 'vitest';
import { render } from '@testing-library/react';
import { TopLoadingBar } from './TopLoadingBar';

describe('TopLoadingBar', () => {
  it('renders without crashing', () => {
    render(<TopLoadingBar />);
  });
});
