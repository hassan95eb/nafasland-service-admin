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
] as const;

export function filterNavItems(permissions: readonly string[]) {
  const allowed = new Set(permissions);
  return navItems.filter((item) => allowed.has(item.permission));
}
