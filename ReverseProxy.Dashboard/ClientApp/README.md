# Signal Plane Client

Vue 3 and TypeScript control console for ReverseProxy.Store 3.0.

The interface supports Simplified Chinese and English. Simplified Chinese is the default; operators can switch languages from the sign-in page or application shell, and the selection is persisted in local storage under `signal-plane.locale`.

## Commands

```bash
npm ci
npm run dev
npm run typecheck
npm run build
npm run preview
```

The development server listens on `http://localhost:5173` and proxies `/api` and `/health` to `https://localhost:5201`. The production build is written to `dist` and served by `ReverseProxy.Dashboard`.
