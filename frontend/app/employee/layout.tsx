'use client';

import StaffShell from '@/components/StaffShell';

export default function EmployeeLayout({ children }: { children: React.ReactNode }) {
  return <StaffShell sectionRole="employee">{children}</StaffShell>;
}
