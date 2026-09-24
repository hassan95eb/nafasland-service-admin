import { Suspense } from "react";

import { UserActivityView } from "@/features/audit/components/user-activity-view";
import { requirePermission } from "@/shared/permissions/require-permission";

export default async function UserActivityPage({ params }: { params: Promise<{ userId: string }> }) {
  const [{ userId }] = await Promise.all([params, requirePermission("audit.read.all")]);
  return (
    <Suspense>
      <UserActivityView userId={userId} />
    </Suspense>
  );
}
