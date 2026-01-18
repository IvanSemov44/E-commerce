import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import './index.css'
import App from './App.tsx'
import { store } from './store/store'
import ErrorBoundary from './components/ErrorBoundary'

const rootElement = document.getElementById('root')
if (!rootElement) {
  throw new Error('Root element not found in HTML')
}

createRoot(rootElement).render(
  <StrictMode>
    <Provider store={store}>
      <ErrorBoundary>
        <App />
      </ErrorBoundary>
    </Provider>
  </StrictMode>,
)
