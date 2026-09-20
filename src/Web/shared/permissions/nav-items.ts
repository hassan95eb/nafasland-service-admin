export const navItems = [
  {
    href: "/products",
    label: "محصولات",
    permission: "catalog.products.read",
  },
] as const;

export function filterNavItems(permissions: readonly string[]) {
  const allowed = new Set(permissions);
  return navItems.filter((item) => allowed.has(item.permission));
}
