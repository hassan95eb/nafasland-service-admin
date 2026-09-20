import { ProductList } from "@/features/products/components/product-list";
import { requirePermission } from "@/shared/permissions/require-permission";

export default async function ProductsPage() {
  await requirePermission("catalog.products.read");
  return <ProductList />;
}
