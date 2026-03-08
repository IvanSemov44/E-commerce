import type { RoutePath } from '@/shared/constants/navigation';

type RouteParams = Record<string, string | number>;
type RouteQuery = Record<string, string | number | boolean | null | undefined>;

export function buildRoutePath(path: RoutePath, params?: RouteParams): string {
  if (!params) {
    return path;
  }

  return Object.entries(params).reduce<string>(
    (resolvedPath, [key, value]) =>
      resolvedPath.replace(`:${key}`, encodeURIComponent(String(value))),
    path
  );
}

export function withQuery(path: string, query?: RouteQuery): string {
  if (!query) {
    return path;
  }

  const searchParams = new URLSearchParams();

  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined && value !== null) {
      searchParams.set(key, String(value));
    }
  });

  const queryString = searchParams.toString();
  return queryString ? `${path}?${queryString}` : path;
}
