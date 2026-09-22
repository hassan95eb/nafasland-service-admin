import { ProductForm } from "@/features/products/components/product-form";
import { requirePermission } from "@/shared/permissions/require-permission";

export default async function NewProductPage() {
  await requirePermission("catalog.products.write");
  return <ProductForm mode="create" />;
}
