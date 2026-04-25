import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';
import { startDeployVersionWatch } from './deployWatch';
import { registerPwa } from './pwa/registerPwa';
import './theme.css';
import './index.css';

startDeployVersionWatch();
registerPwa();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
