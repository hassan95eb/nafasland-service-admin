import { ReturnRequestForm } from "@/features/returns/components/return-request-form";
import { requirePermission } from "@/shared/permissions/require-permission";

export default async function NewReturnPage() {
  await requirePermission("returns.request");
  return <ReturnRequestForm />;
}
