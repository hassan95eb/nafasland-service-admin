export function filterProductsWithoutPrice<T extends { price?: number | string | null }>(items: readonly T[], enabled: boolean) {
  return enabled ? items.filter((item) => item.price === null) : [...items];
}
