import type { components } from "@/shared/lib/api-types.generated";
import { apiFetch } from "@/shared/lib/api-client";

export type ProductListResult = components["schemas"]["PortalProductListResult"];
export type ProductSummary = components["schemas"]["PortalProductSummary"];

export interface ProductListParams {
  page: number;
  pageSize: number;
  keywords: string;
}

export const productQueryKeys = {
  all: ["products"] as const,
  list: (params: ProductListParams) => [...productQueryKeys.all, "list", params] as const,
  detail: (id: string) => [...productQueryKeys.all, "detail", id] as const,
};

export function listProducts(params: ProductListParams) {
  const query = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize),
  });
  if (params.keywords) {
    query.set("keywords", params.keywords);
  }

  return apiFetch<ProductListResult>(`/api/v1/catalog/products?${query.toString()}`);
}
