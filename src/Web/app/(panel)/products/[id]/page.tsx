import { ProductDetails } from "@/features/products/components/product-details";
import { requirePermission } from "@/shared/permissions/require-permission";

export default async function ProductDetailsPage({ params }: { params: Promise<{ id: string }> }) {
  const [{ id }] = await Promise.all([params, requirePermission("catalog.products.read")]);
  return <ProductDetails id={id} />;
}
