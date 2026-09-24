import { Suspense } from "react";

import { AuditLogView } from "@/features/audit/components/audit-log-view";
import { requirePermission } from "@/shared/permissions/require-permission";

export default async function AuditLogPage() {
  await requirePermission("audit.read.all");
  return (
    <Suspense>
      <AuditLogView />
    </Suspense>
  );
}
