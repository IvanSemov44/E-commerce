interface LoadingSkeletonProps {
  count?: number;
  type?: 'card' | 'text' | 'image';
}

export default function LoadingSkeleton({ count = 1, type = 'card' }: LoadingSkeletonProps) {
  const skeletons = Array.from({ length: count }, (_, i) => i);

  if (type === 'card') {
    return (
      <>
        {skeletons.map((i) => (
          <div key={i} className="animate-pulse">
            <div className="bg-slate-200 rounded-lg aspect-square mb-4"></div>
            <div className="space-y-2">
              <div className="h-4 bg-slate-200 rounded w-3/4"></div>
              <div className="h-4 bg-slate-200 rounded w-1/2"></div>
            </div>
          </div>
        ))}
      </>
    );
  }

  if (type === 'text') {
    return (
      <>
        {skeletons.map((i) => (
          <div key={i} className="h-4 bg-slate-200 rounded animate-pulse mb-2"></div>
        ))}
      </>
    );
  }

  return (
    <div className="bg-slate-200 rounded-lg animate-pulse" style={{ paddingBottom: '100%' }}></div>
  );
}
