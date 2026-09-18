import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'Koperasi Store',
  description: 'Village cooperative store management system',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="id">
      <body>{children}</body>
    </html>
  );
}
