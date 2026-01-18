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
            <div></div>
            <div>
              <div></div>
              <div></div>
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
          <div key={i} className="animate-pulse"></div>
        ))}
      </>
    );
  }

  return (
    <div className="animate-pulse" style={{ paddingBottom: '100%' }}></div>
  );
}
