import { AdminManagement } from "@/features/admins/components/admin-management";
import { requirePermission } from "@/shared/permissions/require-permission";

export default async function AdminsPage() {
  const user = await requirePermission("identity.users.manage");
  return <AdminManagement canManageAccess={user.permissions.includes("identity.access.manage")} />;
}
