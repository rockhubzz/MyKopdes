'use client';

// Admin-scoped alias of the shared Items & Stock screen. Renders the exact
// same component as /owner/items so behaviour never drifts — the separate
// route exists only so admins stay inside the /admin section (and keep the
// Admin shell/nav) instead of mounting OwnerLayout.
import OwnerItemsPage from '@/app/owner/items/page';

export default function AdminItemsPage() {
  return <OwnerItemsPage />;
}
