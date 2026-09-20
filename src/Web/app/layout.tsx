import type { Metadata } from "next";
import type { ReactNode } from "react";

import { QueryProvider } from "@/shared/lib/query-provider";

import "./globals.css";

export const metadata: Metadata = {
  title: "پنل مدیریت نفس‌لند",
  description: "مدیریت محصولات نفس‌لند",
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="fa" dir="rtl">
      <body>
        <QueryProvider>{children}</QueryProvider>
      </body>
    </html>
  );
}
