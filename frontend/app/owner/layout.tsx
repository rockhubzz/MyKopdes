'use client';

import StaffShell from '@/components/StaffShell';

export default function OwnerLayout({ children }: { children: React.ReactNode }) {
  return <StaffShell sectionRole="shop_owner">{children}</StaffShell>;
}
