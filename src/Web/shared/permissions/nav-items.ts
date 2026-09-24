export const navItems = [
  {
    href: "/products",
    label: "محصولات",
    permission: "catalog.products.read",
  },
  {
    href: "/admins",
    label: "مدیریت ادمین‌ها",
    permission: "identity.users.manage",
  },
  {
    href: "/approvals",
    label: "درخواست‌های من",
    // "approval.read.own" (ADR-010) is available to every role, which under the
    // default-closed permission model means it cannot be a grantable permission
    // (see ApprovalsPermissions' own comment) — null here means "every
    // authenticated user", not "nobody".
    permission: null,
  },
  {
    href: "/approvals/review",
    label: "کارتابل تأیید",
    permission: "approvals.read.all",
  },
  {
    href: "/admin/audit",
    label: "گزارش فعالیت",
    permission: "audit.read.all",
  },
] as const;

export function filterNavItems(permissions: readonly string[]) {
  const allowed = new Set(permissions);
  return navItems.filter((item) => item.permission === null || allowed.has(item.permission));
}
