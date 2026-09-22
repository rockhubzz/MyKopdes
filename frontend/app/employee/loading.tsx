/**
 * Route-transition fallback: rendered instantly by the App Router while the
 * destination page's JS chunk loads, so sidebar navigation never shows a
 * blank gap. Pure server component — no hooks, no data fetching.
 */
export default function EmployeeLoading() {
  return (
    <div className="space-y-6" aria-label="Loading">
      <div className="skeleton h-8 w-48 rounded-lg" />
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="card">
            <div className="skeleton h-3 w-20 rounded" />
            <div className="skeleton h-7 w-24 rounded mt-2" />
          </div>
        ))}
      </div>
      <div className="card space-y-2">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="skeleton h-4 rounded" />
        ))}
      </div>
    </div>
  );
}
