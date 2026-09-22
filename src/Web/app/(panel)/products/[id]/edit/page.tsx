import { EditProduct } from "@/features/products/components/edit-product";
import { requirePermission } from "@/shared/permissions/require-permission";

export default async function EditProductPage({ params }: { params: Promise<{ id: string }> }) {
  const [{ id }] = await Promise.all([params, requirePermission("catalog.products.write")]);
  return <EditProduct id={id} />;
}
