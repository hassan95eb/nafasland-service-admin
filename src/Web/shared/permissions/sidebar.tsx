import Link from "next/link";

import { LogoutButton } from "@/features/auth/components/logout-button";
import { filterNavItems } from "@/shared/permissions/nav-items";

export function Sidebar({ username, permissions }: { username: string; permissions: string[] }) {
  const items = filterNavItems(permissions);

  return (
    <aside className="flex w-full flex-col border-b border-[var(--border)] bg-white p-4 lg:min-h-screen lg:w-64 lg:border-b-0 lg:border-l">
      <div className="mb-5 border-b border-[var(--border)] pb-4">
        <p className="font-black text-[var(--primary)]">پنل نفس‌لند</p>
        <p className="mt-1 truncate text-xs text-[var(--muted)]">{username}</p>
      </div>

      <nav aria-label="منوی اصلی" className="flex-1">
        <ul className="flex gap-2 lg:flex-col">
          {items.map((item) => (
            <li key={item.href}>
              <Link
                className="block rounded-lg bg-emerald-50 px-4 py-2.5 text-sm font-bold text-[var(--primary-strong)]"
                href={item.href}
              >
                {item.label}
              </Link>
            </li>
          ))}
        </ul>
      </nav>

      <div className="mt-4">
        <LogoutButton />
      </div>
    </aside>
  );
}
