"use client";

import { ProductForm } from "@/features/products/components/product-form";
import { useProduct } from "@/features/products/hooks/use-product";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";

export function EditProduct({ id }: { id: string }) {
  const query = useProduct(id);
  if (query.isPending) return <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت محصول…</p>;
  if (query.error || !query.data) return <Alert>{presentApiError(query.error)}</Alert>;
  return <ProductForm mode="edit" product={query.data} />;
}
