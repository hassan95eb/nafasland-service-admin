import type { components } from "@/shared/lib/api-types.generated";
import { apiFetch } from "@/shared/lib/api-client";

export type ProductDetail = components["schemas"]["PortalProductDetail"];
export type ProductVariant = components["schemas"]["PortalProductVariant"];

export function getProduct(id: string) {
  return apiFetch<ProductDetail>(`/api/v1/catalog/products/${encodeURIComponent(id)}`);
}
