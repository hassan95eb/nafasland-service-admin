import { AdminDetailsView } from "@/features/admins/components/admin-details";
import { requirePermission } from "@/shared/permissions/require-permission";

export default async function AdminDetailsPage({ params }: { params: Promise<{ id: string }> }) {
  const [{ id }, user] = await Promise.all([params, requirePermission("identity.users.manage")]);
  return <AdminDetailsView id={id} canManageAccess={user.permissions.includes("identity.access.manage")} />;
}
