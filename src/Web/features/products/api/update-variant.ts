import type { components } from "@/shared/lib/api-types.generated";
import { apiFetch } from "@/shared/lib/api-client";

export type UpdateVariantRequest = components["schemas"]["UpdateVariantPriceAndInventoryRequest"];
export type UpdateVariantResult = components["schemas"]["UpdateVariantPriceAndInventoryResult"];

export interface UpdateVariantInput {
  variantId: string;
  idempotencyKey: string;
  request: UpdateVariantRequest;
}

export function createIdempotencyKey(randomUuid: () => string = () => crypto.randomUUID()) {
  return randomUuid();
}

export function updateVariant({ variantId, idempotencyKey, request }: UpdateVariantInput) {
  return apiFetch<UpdateVariantResult>(
    `/api/v1/catalog/products/variants/${encodeURIComponent(variantId)}`,
    {
      method: "PATCH",
      headers: { "Idempotency-Key": idempotencyKey },
      body: JSON.stringify(request),
    },
  );
}
