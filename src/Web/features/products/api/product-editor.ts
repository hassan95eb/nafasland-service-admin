import type { components } from "@/shared/lib/api-types.generated";
import { apiFetch } from "@/shared/lib/api-client";

export type CreateProductRequest = components["schemas"]["CreateProductRequest"];
export type CreateProductResult = components["schemas"]["CreateProductResult"];
export type UpdateProductRequest = components["schemas"]["UpdateProductRequest"];
export type UpdateProductResult = components["schemas"]["UpdateProductResult"];
export type CategoryResult = components["schemas"]["PortalTaxonomyResultOfPortalCategoryNode"];
export type FilterResult = components["schemas"]["PortalTaxonomyResultOfPortalFilterGroup"];

export function createProduct(request: CreateProductRequest, idempotencyKey: string) {
  return apiFetch<CreateProductResult>("/api/v1/catalog/products", {
    method: "POST",
    headers: { "Idempotency-Key": idempotencyKey },
    body: JSON.stringify(request),
  });
}

export function updateProduct(id: string, request: UpdateProductRequest) {
  return apiFetch<UpdateProductResult>(`/api/v1/catalog/products/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: JSON.stringify(request),
  });
}

export function getCategories() {
  return apiFetch<CategoryResult>("/api/v1/catalog/categories");
}

export function getFilters() {
  return apiFetch<FilterResult>("/api/v1/catalog/filters");
}
