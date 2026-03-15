## AppInitializer Hook

- Separate LoadingFallback usage: keep shared/components/LoadingFallback/LoadingFallback.tsx for Suspense route lazy loading, and create a dedicated AppInitializingFallback or skeleton used by AppShell when isInitializing is true; avoid reusing the same message/spinner for both.

  [Current LoadingFallback File](shared/components/LoadingFallback/LoadingFallback.tsx)

